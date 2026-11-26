# What HLinq can meet, the engine meets — `where[x.Decimal==x.Double]`, `where[x.Enum==x.Int]`

Issue #31 collected the pairs of members that meet in a comparison and are refused although the
meeting has one obvious answer. !134 made a comparison compare exactly what C# compares — and
that ceiling is too strict for HLinq: C# refuses `decimal == double` and `enum == int` because
a programmer could write the cast by hand; the author of a query string cannot. Where the two
types meet in exactly one reasonable, statable type, the engine lifts; where the meeting has no
single answer, the refusal stays — an array and a text, an entity and a number, two unrelated
references.

## What was measured

The queries, measured over the shipped library (alpha after !134) against an in-memory source:

| query | today | C# by hand |
| --- | --- | --- |
| `x.Int == x.Short`, `x.Int == x.Long`, `x.Int == x.Double` | OK (lifted by C#'s own table) | OK |
| `x.Enum == x.Int` | refused | compile error |
| `x.Decimal == x.Double` | refused | compile error |
| `x.Enum == x.Name` | refused | compile error |
| `x.Ids == x.Name` (an array and a text) | refused | compile error |
| `where[x.Decimal+x.Double>5]` | OK — math's `CommonNumericType` answers `decimal` | compile error |

The last row is the inconsistency the issue names: two features answer the same meeting of two
types differently — the math fold promotes `decimal`-and-`double` to a sum today, the comparison
refuses it.

The three provider packs, asked (through EF's own translators, as `Providers/*TranslationUnitTests`
does) whether they can answer each lift in one statement:

| the lift, written as the hand-written Linq that spells it | npgsql | sqlite | mysql |
| --- | --- | --- | --- |
| `(int)e.Enum == e.Int` | `"Enum" = "Int"` | same | same |
| `e.Enum.ToString() == e.Name` | `CASE "Enum" WHEN 1 THEN 'Active' … END = "Name"` | same shape | same shape |
| `(double)e.Decimal == e.Double` | `"Decimal"::double precision = "Double"` | `CAST(… AS REAL)` | `CAST … + 0e0` |
| `e.Decimal == (decimal)e.Double` | `"Double"::numeric` | `CAST("Double" AS TEXT)` — sqlite answers the comparison over its text-backed decimal | `CAST … AS decimal(65,30)` |
| `(double)e.Int == e.Double` | OK | OK | OK |

Every candidate lift translates to one server-side statement in all three packs. The enum's
string form translates as EF's own `CASE` over the mapped values — no pack refuses it. And
sqlite's handling of the two `decimal`-to-`double` directions settles the direction question
below on its own: `decimal → double` is a real `CAST … AS REAL`, while `double → decimal` asks
sqlite to compare a number to a `TEXT`.

## The rule, and each written behaviour

**One meeting per pair, stated; no meeting is the refusal.** After both sides are built and
before the comparison is chosen, when the two types differ:

1. everything !134 already lifts stays: C#'s own table (numeric widths to the most specific
   operator both convert in, a nullable and its underlying type to the lifted one), and two
   reference types comparing as `object`;
2. an **enum and its underlying integer type** meet in that integer type — the enum is converted,
   the provider sends the number it already stores for it. `Status.Deleted == 3` is true;
   `Status.Deleted == x.Int` is the comparison of the two columns' numbers;
3. an **enum and a text** meet in a text of the enum's *name* — `"Deleted" == "Deleted"`, not
   `"3" == "Deleted"`. The name is what the engines already speak: the packs map an enum member
   to its number and EF spells its name through the `CASE` of the table above, and it is what
   the converters read a query's text as when a value stands against an enum member (!134's
   written-value table) — number-as-text would make the two directions of the same meeting
   answer differently;
4. a **`decimal` and a `double`** (or `float`) meet in `double`. Stated with its loss: past ~15
   to 17 significant digits the `decimal`'s value is the nearest representable `double` — a
   `decimal` of 30 digits loses its tail; what it never does is overflow, which the other
   direction does past ~7.9e28, and sqlite answers `double → decimal` as text against a
   number. `decimal → double` is the one direction that answers a value on every engine.
5. everything else stays refused, naming the two types: two arrays, an entity and a number, an
   entity and an entity of another type, a `bool` and a text, a `Guid` and a number.

Nullability composes as in !134: whichever side is nullable, the meeting is nullable. An enum
meets an `int?` in the lifted underlying type; a `decimal?` meets a `double` in `double?`.

**Math is not this plan.** The fold's `CommonNumericType` answering `decimal` for a
decimal-and-double sum is shipped behavior asserted by the select plan and by the packs; whether
math reads this same table — and which way its sum then promotes — is a separate measurement of
a shipped decision. The divergence is visible after this plan (`x.Decimal==x.Double` compares,
`x.Decimal+x.Double` still meets in `decimal`); a follow-up issue should make one of the two
the other's answer.

**The `int == double` sentence the issue asks for** — the widening is exact (`int` reaches
`double` losslessly below 2^53), so `22` against `23.67` is false, never rounded to `22 == 24`
— is already the behavior; it belongs in README's comparison section next to the lift, since
the query author is the one who needs to know it.

## Steps

- **S1** — the table above as tests in `QueryWhere`: each new pair asserted against the same
  predicate written by hand over the same rows (the enum pair with a member whose underlying
  value equals the int column and one that does not; the `decimal`/`double` pair with a value
  past 17 digits asserting the stated loss; the enum-text pair with `"Deleted"` matching a
  member called `Deleted` and a row of `3` matching nothing), each kept refusal keeping its
  exception and message; and the README section: what meets in what, with the two written
  behaviours (exact widening, name-not-number, the `decimal` tail).
- **S2** — the lift in `ExpressionBuilder.Comparison.cs`: the two extra meetings after C#'s
  table answers nothing, before the nullable composition — enum-to-underlying, enum-to-name
  (through the same `Expression.Call(ToString)` EF translates to its `CASE`), `decimal` to
  `double` — unit green, `jb inspectcode` at baseline.
- **S3** — the providers: `Providers/LiftedComparisonTranslationUnitTests` grows the new pairs —
  one statement per engine naming both columns, and the enum-text pair asserting the `CASE`
  (or its pack's spelling) is inside the predicate; Postgres integration tests answering the
  rows the hand-written LINQ answers, the enum and `decimal`/`double` pairs included.
- **S4** — mark the plan done here, close #31, and name in the row the one refusal a caller may
  expect to pass and that answers no: `bool == string`.

## Out of scope

- Math's promotion table (`CommonNumericType`) — named above as the follow-up.
- A written value meeting a member — that is `ComparisonType` and the converters, decided and
  measured already; nothing here changes what text reaches which type.
- Arrays: "array == string is not reasonable" is honored by the refusal staying; no array
  comparison is introduced.
- `CostOfCast`, overload ranking, ordering roots — as in !134: one value meets no second type
  in an ordering root, and which candidate wins a method contest is the written-constants plan's.

## Definition of done (per AGENTS.md)

`dotnet build HLinq.slnx`; unit, netstandard-unit and integration suites green; `dotnet format
HLinq.slnx --verify-no-changes`; `jb inspectcode` at the baseline count; branch pushed, MR opened,
pipeline watched to green, then stop and wait for review.

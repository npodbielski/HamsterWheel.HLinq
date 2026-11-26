# A comparison compares what C# compares — `where[x.Int==x.NullableInt]`, `where[x.Short==x.Byte]`

Issue #29 collected the places where a value meets a type and the query is refused although the same
comparison written by hand in C# compiles. Measured, the collection splits in two: every *written
value* already reaches the type it stands against, and what remains is the meeting of two *members*
whose types differ by a conversion C# performs implicitly — a nullable and its underlying type, or
two numeric widths. The comparison should lift those two sides to the type C# would compare them in,
and keep refusing every pair C# refuses (`x.Name==x.Int` stays a `400` for the same reason it is a
compile error).

## What was measured

A probe ran each query through tokenizer → parser → applier over an in-memory `DummyEntity` and
recorded what the applier answers (the same harness as `HLinqQueryApplierUnitTests`):

| query | today | C# written by hand |
| --- | --- | --- |
| `x.Int == 5`, `x.NullableInt == 5`, `x.NullableInt == "5"` | OK | OK |
| `x.Decimal == "123.45"`, `x.Decimal == "1e2"`, `x.Decimal == "1e-07"` | OK | OK |
| `x.DateTime == "2020-01-01"`, `x.Name == "abc"` | OK | OK |
| `x.Int - x.Long > 5` (math run folds to one type) | OK | OK |
| `x.Int == x.NullableInt`, `x.NullableInt == x.Int` | `ComparisonOperandTypesCannotBeComparedException` | OK (lifted to `int?`) |
| `x.Decimal == x.NullableDecimal`, `x.DateTime > x.NullableDateTime`, `x.Flag == x.NullableFlag`, `x.Enum == x.NullableEnum` | refused | OK (lifted to `T?`) |
| `x.Short == x.Byte`, `x.Short > x.Byte`, `x.Int == x.Long`, `x.Float == x.Double` | refused | OK (binary numeric promotion) |
| `x.Name == x.Int`, `x.Name == x.DateTime` | refused | compile error |
| `x.Decimal == "1e2.34"` | refused | not a number at all |

Two conclusions the plan rests on:

- **A written value already converts everywhere it meets a type.** `ComparisonConst` reads the
  comparison's `ComparisonType` (what the other side answered) and hands the text to the converters;
  a folded run of written constants goes through `FoldedConstant` the same way; method parameters,
  select assignments and `take`/`skip` each convert to their one fixed destination (the
  written-constants plan closed the last of them). The issue's third gap — a decimal written quoted —
  already works (`"123.45"`, `"1e2"`, `"1e-07"` all reach `decimal`), and `"1e2.34"` is not a
  decimal, or a double, in any number style: there is no value there to convert, and its refusal
  stays.
- **What remains is the member-meets-member meeting**, and C#'s answer to it is not HLinq's own
  invention: a nullable and its underlying type compare through the lifted operator, and two numeric
  widths compare after binary numeric promotion. `Expression.Equal/GreaterThan/...` refuses these
  only because an expression tree gets no compiler to insert the conversions C# would insert.

## Design

- **The lift is the applier's question, not the parse's** (AGENTS.md, *The three stages*): which type
  two members' CLR types meet in is answered against the host's type, exactly like member resolution
  and like the written value's conversion. The tree keeps saying which members the query wrote;
  `Condition`'s converter, which already builds the comparison and already catches
  `InvalidOperationException` to name the refusal, asks for the common type there.
- **One rule: compare exactly what C# would compare.** After both sides are built and before the
  `Expression.Equal/...` call, when the two expressions' types differ:
  1. a type equal to the other, or a reference type — no lift (reference types already compare as
     `object`, which is why `x.Name == x.NullableString` works today);
  2. both sides the same type `T` modulo `Nullable<>` (numeric or not: `int`/`int?`, `DateTime`/
     `DateTime?`, an enum and its nullable) — lift to `T` when neither side is nullable, to `T?`
     when either is;
  3. both sides numeric after lifting — the target of C#'s **binary numeric promotion**, lifted to
     nullable when either side is (`short` and `byte` → `int`, `int` and `long` → `long`, `float`
     and `double?` → `double?`);
  4. anything else — the existing `ComparisonOperandTypesCannotBeComparedException`, unchanged, with
     the two types it names.
  Each side is then wrapped in `Expression.Convert` to the common type, and the comparison is built
  between expressions of one type — a tree every provider's translator reads as its own cast and
  lifted comparison.
- **Promotion follows C#'s implicit conversions only, so a lossy pair stays refused.** `decimal`
  promotes with nothing but itself in C# (`decimal == double` does not compile), and the comparison
  table keeps it that way. The math fold's `CommonNumericType` answers `decimal` for a
  decimal-and-double *sum* — an existing decision of the select plan that this plan does not touch
  (out of scope); the comparison gets its own table and says so when it refuses.
- **The null answer is C#'s null answer.** `x.Int == x.NullableInt` lifted to `int?` is false on a
  row whose `NullableInt` is null (a lifted `==` between a value and null), the same as the same
  LINQ written by hand — nothing extra to define, only to assert.
- **The written side never lifts.** A written constant already became the type of the side it stands
  against before the two meet (that is `ComparisonType`'s whole job); the lift meets members and
  folded runs, and `ComparisonType` itself stays what the side answered — the converters read the
  query's text as that type, and a promotion after conversion would read `"3.5"` as an `int` the
  query never wrote.

## Steps

- **S1** — the table above as tests: each now-valid pair asserts the filter against the same
  predicate written by hand in LINQ over the in-memory source (including the null-row rows: a
  `NullableInt` of null matches nothing), each refused pair keeps its exception and its message. The
  table refuses as much as it accepts — the refusals are the half that proves this is C#'s rule and
  not a widening.
- **S2** — the lift in `Condition`'s converter: a small named helper that answers the common type of
  two types (the four rules), `Expression.Convert` wrappers, and the refusal unchanged when the
  helper answers nothing. Unit green, and `jb inspectcode` stays at the baseline count.
- **S3** — the providers: per-pack translation tests that the lifted comparison is one statement
  with the casts in the predicate (the shape `Providers/*TranslationUnitTests` already assert), and
  an integration test per numeric-width and nullable case against Postgres, asserting the rows the
  hand-written LINQ answers.
- **S4** — mark the plan done here, close #29, and name `decimal == double` staying refused in the
  row — it is the one pair a caller may expect to pass and that answers no with C#'s reason.

## Out of scope

- `x.Decimal=="1e2.34"` and every other text that is no value of any type — there is nothing to
  convert, and the converters already said so in the query's culture.
- Math's promotion table (select plan's `CommonNumericType`, decimal-and-double included) — the
  comparison reads C#'s implicit-conversion rule; whether math should read it too is a separate
  measurement of a shipped decision.
- Method overload ranking and `CostOfCast` — the written-constants plan decided those, and nothing
  here changes which candidate wins.
- Ordering roots — `orderBy` reads one value and two types never meet in it.

## Definition of done (per AGENTS.md)

`dotnet build HLinq.slnx`; unit, netstandard-unit and integration suites green; `dotnet format
HLinq.slnx --verify-no-changes`; `jb inspectcode` at the baseline count; branch pushed, MR opened,
pipeline watched to green, then stop and wait for review.

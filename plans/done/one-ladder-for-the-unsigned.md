# One ladder, one answer per pair — the unsigned pairs of #32, meeting where C# already meets

#32 is the residue !138 left: after the `decimal`-and-binary-float row was settled, two tables of
one engine still answer the same pair of numbers differently, and all fourteen of the remaining
pairs are unsigned on one side. The rule the maintainer set for it is one sentence: **meet both
values in the smallest number that covers what either can hold** — `int` and `uint` are both held
by `long`, so that is where they meet — and where no 64-bit number covers both (a signed number
and a `ulong`), there is no meeting and both clauses say so.

## What was measured

The compiler's own answer for every pair of the eleven widths (Roslyn: the result type of `a + b`,
the parameter type of the operator `a == b` binds to), against the two shipped tables
(`CommonNumericType` in `ExpressionBuilder.Math.cs`, `ComparisonCommonType` in
`ExpressionBuilder.Comparison.cs`). The ladder — smallest covering type, narrow integrals meeting
in `int` as C# itself does, `decimal ⊗ ulong` staying `decimal` (a `decimal` holds every `ulong`) —
reproduces the compiler's answer for **every** pair of the 121, cross-type and same-type. The
pairs it moves are seven:

| pair | today: math / comparison | the ladder | C# |
| --- | --- | --- | --- |
| `uint ⊗ byte`, `uint ⊗ ushort` | `long` / `uint` | **`uint`** | `uint` |
| `byte ⊗ ushort` | `int` / refused | **`int`** | `int` |
| `int ⊗ ulong` (also `long`, `short`, `sbyte`) | `ulong` / refused | **refused in both** | refused (ambiguous) |
| `decimal ⊗ ulong` | `decimal` / `decimal` | `decimal` (stays) | `decimal` |

The three other divergent rows of #32's table (`decimal ⊗ float/double`) are settled already;
after this plan the two tables answer every different-type numeric pair **identically**, so they
become one ladder instead of two that drift.

The refused pair was measured too, because today's math *answers* it and the answer was no good:
`x.Long+x.Unsigned` folds to a `ulong` today, and asked of the packs as the hand-written sum,
sqlite translates the projection and the predicate but **refuses it as an ordering key** — *SQLite
does not support expressions of type 'ulong' in ORDER BY clauses* — the same wall the `decimal`
meeting of !138 turned away from. A pair with no covering 64-bit number has no meeting the engine
can state in every clause, and C# refuses it for exactly that reason; it becomes a refusal naming
both types, in the fold and in the comparison alike.

The `uint` pairs translate in all three packs, asked as the hand-written sum in all three clauses
a fold stands in:

| the lift, as hand-written Linq | npgsql | sqlite | mysql |
| --- | --- | --- | --- |
| `e.Uint + (uint)e.Byte`, selected | `"Uint" + "Byte"::bigint` | `"Uint" + CAST("Byte" AS INTEGER)` | `"Uint" + CAST("Byte" AS unsigned)` |
| same, in a `where` | OK | OK | OK |
| same, as an `orderBy` key | OK | OK | OK |

A behavior change that is safe to make before a release: the `uint ⊗ byte` sum changes answer type
(`long` → `uint`) in a math feature whose demo and dummy rows held no `uint` at all, the new answer
is what the same expression written by hand in C# answers, and what reaches the servers does not
change — npgsql already spells both spellings `"Uint" + "Byte"::bigint` (its `uint` column is a
`bigint`, so the sum is one SQL statement either way). The `ulong ⊗ signed` refusal removes a sum
that no query can rely on: it answered a `ulong` today, which sqlite cannot order.

## The rule

**One numeric ladder, one answer per pair, read by both callers.** After nullability is lifted
(the composition rule of !136 stands), a pair of different numeric types meets in:

1. `double` — the `decimal ⊗ double/float` row of !138, unchanged, and any pair reaching a `double`
   or `float` (no integral fits those);
2. `decimal` — `decimal` with any integral width, `ulong` included: a `decimal` holds every `ulong`;
3. `long` — a `long` or `uint` beside a signed integral number (`uint ⊗ int` meets in `long`, the
   covering number C# itself picks);
4. `uint` — a `uint` beside `byte`, `ushort` or `char`: `uint` already covers all of them;
5. `ulong` — `ulong` beside `byte`, `ushort` or `char`;
6. **nothing** — a signed number (`sbyte`, `short`, `int`, `long`) and a `ulong`: no 64-bit number
   holds both ranges, so neither the fold nor the comparison operates the pair, and the refusal
   names the two types (the fold's existing "there is no `+` between them" refusal, the
   comparison's existing comparison-refusal — no new exception);
7. everything else stays as it is: same-type pairs meet in themselves, narrow integrals meet in
   `int` as C# does (`byte ⊗ ushort` is the comparison this finally answers), the enum and text
   meetings of !136 are the comparison's alone, an enumeration is still no math operand at all.

The comparison's current machinery — candidates of `CommonComparisonOperator` and the implicit-
conversion table — is what the single ladder replaces for integrals: its answer already is the
ladder's everywhere except `byte ⊗ ushort`, where the candidates' ambiguity is C#'s non-answer and
the ladder has the real one. What stays where it is: nullable composition per caller, the
comparison's enum/text and reference paths, the fold's text join and enumeration refusal.

## Steps

- **S1** — tests first: each moved pair in `QueryWhere`/`QuerySelectMath` against the same
  expression written by hand (the `uint` sums and comparisons, the `byte==ushort` comparison that
  stops being refused, the signed-`ulong` refusal in both clauses asserting its exception), each
  untouched pair asserted as it is (`decimal ⊗ ulong` still a `decimal`, `int ⊗ uint` still
  `long`); `DummyEntity` grows the `uint`/`ushort` members the rows need. README's auto-casting
  sentence gains the covering rule and the one documented refusal.
- **S2** — one ladder: `CommonNumericType` becomes the numeric answer both `FoldMath` and
  `ComparisonCommonType` read; the comparison's integral candidates go with it; unit suites green,
  `jb inspectcode` at the baseline.
- **S3** — the packs: `LiftedComparisonTranslationUnitTests` and the math translation suites grow
  the `uint` pairs — one statement per engine in projection, predicate and `ORDER BY` — and the
  `ulong ⊗ signed` refusal asserted as client-side (no statement asked). The demo `Measurement`
  grows the columns the Postgres tests need (`Unsigned`, `Uint`, `Ushort`; a local demo database
  has to be dropped and re-seeded, `EnsureCreated` does not add columns).
- **S4** — mark the plan done here, close #32, and name in the row the refusal this plan
  documents: a signed number and a `ulong` meet in nothing, in math and in comparison alike.

## Out of scope

- Same-type pairs: untouched, as C# leaves them (`byte == byte` compares bytes).
- `char`: what each caller does with it today (the comparison reads its conversions, the fold
  does not read it as a number) is what it does after the ladder - the plan's ladder is stated
  over the eleven numeric widths exactly as `IsNumeric` states them today.
- Enum meetings: settled by !136, this plan touches only the integral arms.
- The union of "which numeric widths a host may have": the ladder is stated over the eleven C#
  numeric types and answers nothing else.

## Definition of done (per AGENTS.md)

`dotnet build HLinq.slnx`; unit and netstandard-unit suites green; integration suite green in CI;
`dotnet format HLinq.slnx --verify-no-changes`; `jb inspectcode` at the baseline count with no
finding on the touched files; branch pushed, MR opened, pipeline watched to green, then stop and
wait for review.

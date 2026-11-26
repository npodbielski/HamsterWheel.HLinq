# One meeting per pair — `select[N=x.Decimal+x.Double]` answers what `where[x.Decimal==x.Double]` compares

Issue #31 closed with one sentence left open: two features answer the same meeting of two types
differently. A comparison meets a `decimal` and a `double` in `double` (!136, `ConvertedCommonType`),
the math fold meets them in `decimal` (`CommonNumericType`, from the select plan). So
`where[x.Decimal==x.Double]` compares and `select[x.Decimal+x.Double]` promotes — and the two answers
do not agree on which value the row gets. One of the two must become the other's answer.

## What was measured

The compiler's own decision, asked of Roslyn (the pairs the compiler refuses are the pairs no
hand-written Linq writes either):

| pair | `a + b` answers | `a == b` runs in |
| --- | --- | --- |
| `decimal`, `double` / `float` | does not compile (CS0019) | does not compile (CS0019) |
| `decimal`, `int` / `long` / `ulong` | `decimal` | `decimal` |
| `int`, `ulong` | ambiguous (CS0034) | no operator (CS0034) |
| `byte`, `ushort` | `int` | `int` |
| `uint`, `byte` | `uint` | `uint` |

The two tables HLinq ships today, asked for every numeric pair (the builder's own private
`CommonNumericType` and `ComparisonCommonType`, one probe over the eleven widths):

| pair | math's table | comparison's table | C# |
| --- | --- | --- | --- |
| `decimal` + `double` | `decimal` | **`double`** | neither: no sum, no operator |
| `decimal` + `float` | `decimal` | **`double`** | neither |
| `decimal` + `int`, `long`, `ulong` | `decimal` | `decimal` | `decimal` |
| `int` + `ulong` (and `long`, `short`, `sbyte`) | `ulong` | refused | ambiguous, refused |
| `byte` + `ushort` | `int` | refused | `int` |
| `uint` + `byte` / `ushort` | `long` | `uint` | `uint` |

Eighteen cross-type pairs answer differently, and fourteen of them are the unsigned family — a
separate question, named below as the follow-up. Four are `decimal` with a binary floating point,
and that is the pair C# has no answer for at all: the query's author cannot write the cast, so the
engine either states a meeting or refuses one of the two clauses for no reason the query can see.

**Which direction.** The three packs, asked through EF's own translators whether each direction
answers one server-side statement in each of the three clauses a fold stands in:

| the lift, as the hand-written Linq that spells it | npgsql | sqlite | mysql |
| --- | --- | --- | --- |
| `(double)e.Decimal + e.Double`, selected | `"Decimal"::double precision + "Double"` | `CAST("Decimal" AS REAL) + "Double"` | `(CAST("Decimal" AS decimal(65,30)) + 0e0) + "Double"` |
| same, in a `where` | OK | OK | OK |
| same, as an `orderBy` key | OK | OK | OK |
| `e.Decimal + (decimal)e.Double`, selected | `"Decimal" + "Double"::numeric` | `ef_add("Decimal", CAST("Double" AS TEXT))` | `"Decimal" + CAST("Double" AS decimal(65,30))` |
| same, in a `where` | OK | the `ef_compare` of two texts | OK |
| same, as an `orderBy` key | OK | **refused**: *SQLite does not support expressions of type 'decimal' in ORDER BY clauses* | OK |

The `decimal` direction is not merely uglier in sqlite — its column is text-backed there, and the
pack refuses an ordering over a `decimal` at all: `OrderBy(e.Decimal)` alone is refused by sqlite,
with or without a sum. `double → decimal` is therefore not an answer a query can rely on in a
clause the select plan shipped, and `decimal → double` is a real `CAST` in all three packs in all
three clauses. The direction the comparisons already take is the only one every engine answers.

**What the fold answers after the flip.** The meeting is stated with its loss, the same sentence
the README carries for the comparison: past the ~17 significant digits a `double` keeps, the
`decimal`'s tail is gone; what the direction never does is overflow, which `double → decimal` does
past ~7.9e28. One consequence of reading the pair as a `double` belongs to division: a quotient of
a `decimal` by a zero `double` answers the infinity a `double` division by zero answers, where
the same quotient over decimals throws — the difference C# itself has between `1m/0m` and
`1.0/0.0`, and the reason the written value `0.0` is not read back as a `decimal`. A `select` member therefore changes its answer type for that pair (`decimal` →
`double`), which is exactly what the shipped comparison decided in !136; a pair C# itself promotes
(`decimal` and `int`, `long`, `ulong`) is untouched.

## The rule

**A pair of numbers has one meeting, and one question per caller reads it.** Where the two shipped
tables answer the same type today they go on answering it; where they answer differently, this plan
gives the `decimal`-and-binary-float pair the answer the comparison already states and the packs
can always cast:

1. a `decimal` and a `double` (or `float`) meet in `double`, in a sum, a difference, a product, a
   quotient and a comparison alike — the pair is named by one predicate both tables read, so
   neither can drift back on its own;
2. everything else stays as it is: `decimal` with an integral number still answers `decimal`, C#'s
   own promotion of the other widths is untouched, an enumeration is still refused because an
   engine answers two columns and no exception, nullability lifts the meeting exactly as before
   (`decimal?` and `double` sum to `double?`);
3. the fourteen unsigned-family pairs that still answer differently (`int + ulong` folds to
   `ulong` where a comparison refuses, `byte == ushort` refuses where C# compares in `int`,
   `uint + byte` meets in `long` for a sum and `uint` for a comparison) are not this pair and are
   not decided here — they are #32, filed with this plan.

## Steps

- **S1** — the tests and the sentence first: `QuerySelectMath` asserts each fold of the pair against
  the same predicate written by hand over the same rows — `x.Decimal+x.Double`, `x.Decimal+x.Float`,
  `x.NullableDecimal+x.Double` (a nullable meets it), `x.Decimal+1.5` (a written number is read as
  the literal of its own type, so a `double`), `x.Decimal/x.Double` — and the rows that must *not*
  move: `x.Decimal+x.Int` still answers a `decimal`, `x.Int+x.Long+x.NullableDecimal` still answers
  a `decimal`. A `where[x.Decimal+x.Double>5]` and an `orderBy[x.Decimal+x.Double]` read the same
  fold. README's expression section says the meeting and its loss next to the promotion it already
  states.
- **S2** — the flip: `CommonNumericType` answers `double` for the pair, through the one predicate
  `ConvertedCommonType` reads, so the meeting is named once for both callers. Unit suites green,
  `jb inspectcode` at the baseline count.
- **S3** — the packs: `SelectMathTranslationUnitTests` and `WhereOrderingMathTranslationUnitTests`
  grow the pair on a row that holds a `decimal`, a `double` and a `float` — one statement per
  engine, the cast inside the projection, inside the predicate and inside the `ORDER BY`, and the
  sqlite row that the decimal answer could not answer at all. Postgres integration tests answer the
  rows the same Linq written by hand answers: the sum selected, the sum compared, the sum as a key,
  over the demo's `Measurement` rows whose seeded `decimal` of a tail no `double` keeps is the loss
  said out loud.
- **S4** — mark the plan done here, and name in the row the pair that answers differently after
  this plan is merged (`int + ulong`, `byte == ushort`: the unsigned family) and the issue that
  holds it, #32.

## Out of scope

- The unsigned family (fourteen pairs) — its own measurement of the same question, filed as this
  plan's follow-up issue.
- What an enum meets: math refuses an enumeration as an operand on purpose (the engines answer two
  columns and no exception), and a comparison meets it in its number or its name — that asymmetry
  is a refusal by design, not a divergence between two answers.
- Any pair the two tables already answer alike: this plan moves one row, not the tables.
- Client-side evaluation: as ever, what no pack translates is refused, not pulled back to the
  client.

## Definition of done (per AGENTS.md)

`dotnet build HLinq.slnx`; unit and netstandard-unit suites green; integration suite green in CI;
`dotnet format HLinq.slnx --verify-no-changes`; `jb inspectcode` at the baseline count with no
finding on the touched files; branch pushed, MR opened, pipeline watched to green, then stop and
wait for review.

## What the review moved

The expression section says what the engine decides for a pair C# cannot add without a cast - a
query writes no cast, so the engine finds the common type of the two and casts one or both values
to it, and names the two types when it finds no sensible way to operate - and points at the
comparison section for the meetings themselves, where the `decimal`-to-`double` loss is stated
once. S1's row above describes the sentence the review replaced.

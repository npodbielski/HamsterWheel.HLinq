# A query writes a `TimeSpan` and a `DateOnly`

Closes #21. `FromStringConverter` parses the types a query may write a value of; its table holds
`DateTime`, `DateTimeOffset`, `TimeOnly`, the numbers, `Guid`, `char`, `string` - but no `TimeSpan`
and no `DateOnly`. Whatever is not in the table falls to `ViaSerializationConverter`, which goes
through `System.Text.Json`, and STJ parses neither of the two - so a written span or date fails
with a `JsonException` instead of the converter's own refusal, and three recorded names
(`Distance`, `ToDate` on Postgres, `TimeFromParts` on SQL Server) sit out of reach for a row that
writes its comparisons.

## What was measured

The tokenizer already lets both spellings through as one `NameOrValue` token - no grammar change
is needed:

| written in a query | token | `TimeSpan.TryParse` | `DateOnly.TryParse` |
|---|---|---|---|
| `00:02:00` | one value | yes - `00:02:00` | no |
| `730.00:00:00` | one value | yes - `730.00:00:00` | no |
| `2020-01-01` | one value | no | yes - `2020-01-01` |
| `2020-01-01T00:00:00` | one value | no | yes |
| `"730.00:00:00"` (quoted) | one value, unquoted by the converter | yes after unquoting | no |
| `abc` | one value | no | no |

Two properties of the parsers decide what a query may then write, and both are accepted here:

- **a bare number is a `TimeSpan` of that many days**: `2` parses to `2.00:00:00`. So
  `where[Distance(x.Born, x.Born) > 2]` becomes a filter over two days - which reads as intended
  ("more than two days"), and is the same text that today dies in the JSON fallback. A query that
  means two days may also write the unambiguous `2.00:00:00` or `730.00:00:00`.
- **the two parsers do not overlap**: every text that parses as one fails the other, so a parameter
  of one of the two types is never contested by the other.

## Design

Two entries in the parse table of `FromStringConverter`, each the same shape as the `TimeOnly`
entry beside it - `TryParse` with the query's culture, `InvalidConstantStringToTypeConversionException`
when the text is not that type:

```csharp
{
    typeof(TimeSpan),
    (s, culture) => TimeSpan.TryParse(s, culture, out var span)
        ? span
        : throw new InvalidConstantStringToTypeConversionException(s, typeof(TimeSpan))
},
#if !NETSTANDARD
{
    typeof(DateOnly),
    (s, culture) => DateOnly.TryParse(s, culture, out var date)
        ? date
        : throw new InvalidConstantStringToTypeConversionException(s, typeof(DateOnly))
}
#endif
```

`TimeSpan` exists in netstandard2.0, so it is unconditional; `DateOnly` joins `TimeOnly` under
`#if !NETSTANDARD`. Nullable parameters come for free - `TryConvert` unwraps the nullable and
handles the null keyword before the table is consulted, as it does for the other types.

That is the whole of the src change. The comparison path (`ComparisonConst` -> `DefaultConverter`)
then builds:

- `where[Distance(x.Born, x.Born) > 00:02:00]`
- `where[Distance(x.Born, x.Born) > 730.00:00:00]`
- `where[ToDate(x.BornText, yyyy-MM-dd) > 2020-01-01]`

### Out of scope: written values as *arguments*

`where[Distance(x.Day, 2020-01-01) > x.Stay]` still does not build after this plan, and that is
kept out deliberately. A parameter of a function accepts a written text, char or enum member only
(`ParametersConverter.MapArgument`, mirrored by `MemberRanking.CostOfCast`); every other type is
reached by a column of the row - the documented rule of the docs README ("nothing else is a
value"). Widening that is a decision about overload ranking - a written date would make a
`DateOnly` overload compete with a `DateTime` one - that decision is #22, "allow more types as
constants as method parameters". This plan changes only what a comparison may be written with,
which no ranking is involved in: the compared type is already fixed by the left side.

## The record moves with it

The surface record of !93 measured these names against what a query could write, and said so:

- Postgres: `Distance: parameter DateOnly a binds no column and no written value; returns TimeSpan,
  which nothing of a query compares with`, `ToDate: returns DateOnly, ...`
- SQL Server: `TimeFromParts: returns TimeSpan, ...`

When the two parse entries exist, the probe's written-value families (`WrittenComparisonOf`) gain
`TimeSpan` (`> 00:02:00`) and `DateOnly` (`== 2020-01-01`), and the names move to the value set -
which the record literals and the docs pages must say in the same commit, or the tests say so:

- `PgSqlFunctionSurfaceUnitTests`: `Distance`, `ToDate` move out of `OutOfReachNames` into
  `ValueNames`; same for `TimeFromParts` in `SqlServerFunctionSurfaceUnitTests`.
- `docs/db-functions/postgres.md` and `sql-server.md`: the rows move from "Out of reach" to
  "Answering with a value" - the docs-vs-record test holds them against the record.

## Delivery steps

- **S1** - the two parse entries in `FromStringConverter`; converter tests beside the `TimeOnly`
  ones (`ConvertToT1.cs` is linked into the NetStandard project - the `DateOnly` cases carry the
  same `#if !NETSTANDARD` guard `TimeOnly` uses there), including the refusals: `abc` as a
  `TimeSpan` and as a `DateOnly` is `InvalidConstantStringToTypeConversionException`, never the
  `JsonException` of the fallback.
- **S2** - the probe's written families, the two record literals, the two docs pages; filter and
  value probes of the moved names get a test each (`where[Distance(x.DateTime, x.DateTime) >
  00:02:00]`, `where[ToDate(x.Name, yyyy-MM-dd) > 2020-01-01]`, `where[TimeFromParts(x.Int, x.Int,
  x.Int, x.Int, x.Int) > 00:02:00]`).
- **S3** - the docs README's "a value" bullet grows past text, char and enum member: a comparison
  is written with a value of the type it compares with, and the bare-number-is-days reading of a
  `TimeSpan` is named there, since `> 2` now means two days where it used to be refused.

## Definition of done

`dotnet build HLinq.slnx`; unit, netstandard-unit and integration suites green;
`dotnet format HLinq.slnx --verify-no-changes` clean; `Distance`, `ToDate` and `TimeFromParts` in
the value set of their records with the docs pages agreeing; a written `abc` for either type
refused by name of the type, not by JSON; branch pushed, MR against `alpha`, pipeline green, then
wait for review.

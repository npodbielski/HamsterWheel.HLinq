# A written value reaches a parameter of a function

Closes #22. A parameter of a function is filled from the query text with written text, a single
character or an enum member only (`ParametersConverter.MapArgument`, mirrored by
`MemberRanking.CostOfCast`); every other type is reached by a column of the row. The same text
already reaches a *comparison* (`FromStringConverter`, since !96) - so `where[Distance(x.Born,
x.Born) > 2020-01-01]` builds while `where[Distance(x.Born, 2020-01-01) > x.Stay]` is refused.
Widening the argument path is a decision about overload ranking: a written `2020-01-01` makes a
`DateOnly` overload compete with a `DateTime` one for the same text, and the ranking must say
which wins and the warning must say why.

## What was measured

The parse table of `FromStringConverter` is the list of what a query may write; the table below is
what its parsers accept, taken from the parsers themselves (invariant culture; every other text a
query can write - `abc`, a quoted value - parses as none of these):

| written | byte/short/int/long | decimal/double/float | Guid | DateOnly | TimeOnly | TimeSpan | DateTime | DateTimeOffset |
|---|---|---|---|---|---|---|---|---|
| `7` | yes | yes | no | no | no | yes - 7 days | no | no |
| `7.5` | no | yes | no | no | no | no | yes - a date in July | no |
| `2020-01-01` | no | no | no | yes | no | no | yes | yes |
| `2020-01-01T14:30` | no | no | no | yes - the time dropped | no | no | yes | yes |
| `14:30` | no | no | no | no | yes | yes | yes - today's date | yes |
| `00:02:00` | no | no | no | no | yes | yes | yes - today's date | yes |
| `730.00:00:00` | no | no | no | no | no | yes | no | no |
| a GUID | no | no | yes | no | no | no | no | no |

The parsers overlap: a moment text is a `DateOnly` (its time dropped) and a bare time is a
`DateTime` (today's date). So "parses into the parameter's type" is not enough to say which
overload a text means - and the contests are real, not hypothetical. Every name of the four
shipped packs with more than one overload was walked over all argument lists of columns and written
texts (the harness replayed `CostOfCast` and the real provider classes):

- **PgSql** - `Distance(DateOnly, DateOnly)` vs `Distance(DateTime, DateTime)`: a written pair of
  dates, or of moments, binds both.
- **SqlServer** - the `DateDiff*` family (`DateDiffDay` has six overloads: `DateTime`,
  `DateTimeOffset`, `DateOnly` and the three lifteds): a written pair binds up to six.
  `AtTimeZone(DateTime, string)` gains every written moment as its first argument.
- **MySql** - the same `DateDiff*` family, plus `ConvertTimeZone` (six first-parameter kinds) and
  `Degrees`/`Radians` (`double` vs `float`, contested by a written number).
- **Sqlite** - nothing: its one class holds two names.

A naive widening - every type of the table joins the ranking at the one existing written tier
(`ParsedFromQueryText`) - was measured too, and it loses: `DateDiffDay(2020-01-01T14:30,
2020-01-01T14:30)` then ranks `DateOnly` first by the ordinal tie-break, silently dropping the
time part of what the query wrote, and `Degrees(7)` is an ordinal coin flip between `double` and
`float`. The tie-break machinery of !49 would report the tie honestly - but the tie is avoidable.

## Design

**A written argument costs two tiers, not one.** A parameter accepts the text when the text parses
into its type - that is the binding, and it is `FromStringConverter`'s table deciding, not a new
table. The cost says how well the text fits:

- **own text** (tier `ParsedFromQueryText`, as today): the text is written in this type's own
  shape - an integer for an integral parameter, a number for a floating one, a GUID for a `Guid`,
  a date (`2020-01-01`) for `DateOnly`, a moment (`2020-01-01T14:30`) for `DateTime`/
  `DateTimeOffset`, a time (`14:30`, `00:02:00`, `730.00:00:00`) for `TimeSpan`/`TimeOnly`.
- **a slack fit** (a new tier, one above own text): the text parses into the type but is written
  in another type's shape - `2020-01-01` into a `DateTime` (the time is midnight, not written),
  `14:30` into a `DateTime` (today's date, not written), `7.5` into a `DateTime`.

The shape of the text is read from **what it parses as in the culture of the query** - not from
its punctuation, which means different things in different cultures: measured against the parse
table with its own number styles, `1.2` is a `decimal` in the invariant culture and only a
`DateOnly` in Polish, while `1,2` is a `decimal` in Polish and a `decimal` of twelve in the
invariant one (thousands are allowed). So the shape is derived by asking the parse table itself -
number, fraction, GUID, date (`DateOnly`), time (`TimeOnly`/`TimeSpan`), a moment (both a date and
a time) - and the shape table is small because the types are few. What it decides, decided by
`CheapestConversion` and no longer by the ordinal tie-break:

| written | contest | winner | why |
|---|---|---|---|
| `2020-01-01` | `DateOnly` vs `DateTime` | `DateOnly` | a date is the date's own text, slack for a moment |
| `2020-01-01T14:30` | `DateOnly` vs `DateTime` | `DateTime` | a moment is the moment's own text, slack for a date (the time is not dropped) |
| `14:30` | `TimeOnly`/`TimeSpan` vs `DateTime` | the time types | same, mirrored |
| `7` | `int` vs `long` | ordinal, warned | nothing of the text tells them apart - the tie-break says so out loud |
| `7` | `int` vs `TimeSpan` | ordinal, warned | a bare number is the days reading !96 documents, so it is a span's own text too - nothing of the text says which of the two the query meant |
| `7` | `double` vs `float` | ordinal, warned | same |

`string` stays `Exact` (a text is a string's own text), `char` and enum member stay at own-text
tier. `bool` stays out: it is not in the parse table (`DefaultConverter` decides booleans by its
own prose rules), and this plan widens arguments to exactly what the table parses.

**The ranking is answered per query, by the registered parser.** What a text parses into is a
fact of the query - its culture decides it, and the null keyword of the consumer's registration
decides what `null` means - so the question is not a static one: `IWrittenValueParser.Parses(text,
type, culture)` is answered by the registered `FromStringConverter`, injected into the builder
and handed to the ranking with the culture of the request, the same way the list of candidate
overloads is per call while the list of methods per type is not. A static, invariant-culture
question would rank `1.2` as a fraction for a query written in Polish, where it is a date, and
would read a null keyword the consumer never registered. When the ranking binds and the
conversion then refuses the text, the candidate is refused like any other that does not fill,
and the call is refused naming its arguments - the existing behaviour of `TryConvert`.

## The change

- **`FromStringConverter`** exposes its table as a question, as `IWrittenValueParser`:
  `Parses(text, destination, culture)` - quotes stripped, the registered null keyword standing
  for a nullable, the culture of the query deciding the reading - so "what a query may write" is
  stated once, by the instance the container hands over. Its own `TryConvert` keeps using the
  table the same way.
- **`MemberRanking.CostOfCast`** grows past text/char/enum: a type the table parses costs own-text
  or slack by the shape table above; a text that parses into nothing of the candidate stays
  unbindable, exactly as today.
- **`ParametersConverter.MapArgument`** mirrors it: after the string/char/enum cases, a parameter
  whose type the table parses is filled by `IDefaultConverter.ConvertTo(parameterType, text,
  culture)` - the same converter a comparison value goes through, so the value arrives typed
  (`Expression.Constant(value, parameterType)`, nullable parameters included). Both the parser
  and the converter are injected (`IParametersConverter` is registered in `HLinqCore`); the
  culture arrives through a new optional `CultureInfo?` parameter of `TryConvert`, which the two
  method converters pass from their builder context. A conversion that throws
  `InvalidConstantStringToTypeConversionException` refuses the candidate - the ranking and the
  conversion read the same table in the same culture, so this happens only when a converter the
  consumer registered beside `FromStringConverter` still refuses the text.
- **`FunctionSurfaceProbe`** fills a parameter it has no column for with a written value of every
  shape the table parses (a number, a fraction, a GUID, a date, a moment, a time), the way it
  already fills text, char and enum. The records are re-measured, not guessed: of the four packs
  no name is expected to move (the out-of-reach names lack tuples, sequences and return types,
  not written values) - if one moves, its record literal and docs page move in the same commit.

## Out of scope

- `bool` parameters (not in the parse table) and types outside it (`IPAddress`, `NpgsqlRange<T>`,
  byte arrays) stay column-only.
- Integral width (`int` vs `long`) and `double` vs `float` stay an ordinal tie, warned - the text
  carries no width, and inventing a width order is not this plan's claim.
- Comparison values are !96 and unchanged.

## Delivery steps

- **S1** - `IWrittenValueParser` over the table of `FromStringConverter`, registered as the
  instance it is; the two tiers in `CostOfCast`, answered by that parser in the culture of the
  query; `MapArgument` widened with the injected converter and the culture threaded through
  `IParametersConverter`.
- **S2** - ranking tests in `MemberResolutionUnitTests` for every row of the table above (each
  naming the `ResolutionReason` that decides it), and refusal tests: `abc` binds no number, date
  or GUID parameter; `2020-01-01T14:30` still reaches a `DateTime` parameter (slack, not refused).
- **S3** - filter-building tests: a written date into a `DateTime` parameter through the real
  packs (`where[Distance(x.DateTime, 2020-01-01) > 00:02:00]`), a written number into `Degrees`;
  the existing `Sub`/`Sub2` expectations updated where a widened overload now competes (a written
  `5` makes `Sub(int)` compete with `Sub(string)`, which still wins as `Exact`).
- **S4** - the probe's written fillings, the four records re-measured, and the docs: the "a value"
  bullet of `docs/db-functions/README.md` grows past text/char/enum to the shapes of the table,
  and the sentence of `sql-server.md` ("a written value reaches a parameter of a function only as
  text, a character or an enum member") says what is now true.

## Definition of done

`dotnet build HLinq.slnx`; unit, netstandard-unit and integration suites green;
`dotnet format HLinq.slnx --verify-no-changes` clean; the four surface records re-measured with
the probe's widened fillings (docs pages agreeing); `where[Distance(x.DateTime, 2020-01-01) >
00:02:00]` builds and ranks the `DateTime` overload by `CheapestConversion`; branch pushed, MR
against `alpha`, pipeline green, then wait for review.

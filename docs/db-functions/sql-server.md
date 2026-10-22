# SQL Server

`AddSqlServerToHLinq()` hands a query `SqlServerDbFunctionsExtensions`. How a call is written, and
what its arguments may be, is in [the page beside this one](README.md).

## Answering with a truth

| call                                  |                       |
|---------------------------------------|-----------------------|
| `where[IsDate(x.Born)]`               |                       |
| `where[IsNumeric(x.Age)]`             |                       |
| `where[Contains(x.Name, Bill)]`       | full-text search of one column; the second argument is the condition, the third the language to search in |
| `where[FreeText(x.Name, Bill)]`       | the same, over the columns of the row the term appears in |

## Answering with a value

| call                                        |                        |
|---------------------------------------------|------------------------|
| `where[DateDiffDay(x.From, x.To) > 7]`      | a number of days - and `DateDiffHour`, `Minute`, `Second`, `Millisecond`, `Microsecond`, `Nanosecond`, `Week`, `Month`, `Year` of the same two arguments |
| `where[DataLength(x.Name) > 100]`           | how many bytes the value takes, when it takes any |
| `where[PatIndex(%Bill%, x.Name) > 0]`       | where a pattern starts in a text |
| `DateFromParts(x.Year, x.Month, x.Day)`     | a date out of parts - likewise `DateTimeFromParts`, `DateTime2FromParts`, `DateTimeOffsetFromParts`, `SmallDateTimeFromParts`, `TimeFromParts`, each taking the parts as numbers |
| `AtTimeZone(x.Created, UTC)`                | a moment in a time zone |

`DateDiff` is a family of overloads, one per unit and per kind of moment: the `DateTime`,
`DateTimeOffset` and `TimeOnly` overloads are reached by a column of that kind, the `DateOnly` and
`TimeSpan` ones by a row holding such a column - a query writes no value of those two types.

## Out of reach

- `StandardDeviationPopulation`, `StandardDeviationSample`, `VariancePopulation` and
  `VarianceSample` want a sequence of numbers: a query names the columns of a row one by one, and
  writes no sequence of them.
- `DataLength` of a `byte[]` or of a `Guid` wants a column of that very type - which a row of such
  a column does answer.

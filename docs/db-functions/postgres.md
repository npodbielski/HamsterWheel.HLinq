# Postgres

`AddPgSqlToHLinq()` hands a query two classes of the Npgsql provider: `NpgsqlDbFunctionsExtensions`
and `NpgsqlTrigramsDbFunctionsExtensions`. How a call is written, and what its arguments may be, is
in [the page beside this one](README.md).

## Answering with a truth

| call                                                 |                                          |
|------------------------------------------------------|------------------------------------------|
| `where[ILike(x.Name, %Bill%)]`                       | `LIKE` ignoring case; the third argument is the escape character |
| `where[TrigramsAreSimilar(x.FirstName, x.LastName)]` | likewise `TrigramsAreWordSimilar`, `TrigramsAreStrictWordSimilar`, and the `Not` form of each |

## Answering with a value

| call                                                     |                                       |
|----------------------------------------------------------|---------------------------------------|
| `where[Reverse(x.Name) == ylliB]`                        | the text backwards                    |
| `where[ToDate(x.BornText, yyyy-MM-dd) > x.BornDate]`     | a date read out of a text, the second argument being the format |
| `ToTimestamp(x.BornText, yyyy-MM-dd HH:mm)`              | a date and a time read out of a text - both of these take texts, not dates |
| `where[Distance(x.Arrival, x.Departure) > x.Stay]`       | the span between two dates, compared with a span of the row |
| `where[TrigramsSimilarity(x.Name, x.Alias) < 0.25]`      | a number - the same for `TrigramsWordSimilarity`, `TrigramsStrictWordSimilarity`, and for the five `Distance` measures |

A function of several overloads is reached by the overloads whose parameters the row and the
written values fill: of `Distance(DateOnly, DateOnly)` and `Distance(DateTime, DateTime)` it is the
second that a row of two dates answers, `DateOnly` being a type no query writes a value of. What a
call answers with is compared as any other expression, so a row without a column of that type - a
`DateOnly`, a `TimeSpan` - holds nothing to compare it with.

## Out of reach

- `GreaterThan`, `GreaterThanOrEqual`, `LessThan`, `LessThanOrEqual` want a tuple of the values
  compared: a query writes no tuple, and no column of a row is one.
- `StringToArray` and `TrigramsShow` answer with a list of text: the call is built, but nothing a
  query holds is a list to compare a list with, so no filter is made of them.
- The provider's json, network, range, multirange, cube, aggregate, full-text-search and
  fuzzy-string-match classes are not handed over, so none of their names is a word of the language.
  An API that wants them hands over the class itself, as any other table of words is handed over -
  and there, an `NpgsqlInet`, an `IPAddress`, a `NpgsqlRange<T>` or a `PhysicalAddress` argument is
  a column of the row only, since a query writes no value of any of those.

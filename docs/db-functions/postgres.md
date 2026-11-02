# Postgres

`AddPgSqlToHLinq()` hands a query two classes of the Npgsql provider: `NpgsqlDbFunctionsExtensions`
and `NpgsqlTrigramsDbFunctionsExtensions`. How a call is written, and what its arguments may be, is
in [the page beside this one](README.md).

## Answering with a truth

| call                                                 |                                          |
|------------------------------------------------------|------------------------------------------|
| `where[ILike(x.Name, %Bill%)]`                       | `LIKE` ignoring case; the third argument is the escape character |
| `where[TrigramsAreSimilar(x.FirstName, x.LastName)]` | how alike two texts are to each other |
| `where[TrigramsAreWordSimilar(x.FirstName, x.LastName)]` | the same, reading the second text as one word |
| `where[TrigramsAreStrictWordSimilar(x.FirstName, x.LastName)]` | the same, requiring the word to stand whole |
| `where[TrigramsAreNotWordSimilar(x.FirstName, x.LastName)]` | the negation of each of the three: `TrigramsAreNotWordSimilar`, `TrigramsAreNotStrictWordSimilar` |

## Answering with a value

| call                                                     |                                       |
|----------------------------------------------------------|---------------------------------------|
| `where[Reverse(x.Name) == ylliB]`                        | the text backwards                    |
| `where[ToTimestamp(x.BornText, yyyy-MM-dd HH:mm) > x.BornDate]` | a date and a time read out of a text, the second argument being the format |
| `where[TrigramsSimilarity(x.Name, x.Alias) < 0.25]`      | how alike two texts are, as a number between 0 and 1 |
| `where[TrigramsWordSimilarity(x.Name, x.Alias) < 0.25]`  | the same for the two word measures: `TrigramsWordSimilarity`, `TrigramsStrictWordSimilarity` |
| `where[TrigramsSimilarityDistance(x.Name, x.Alias) < 0.75]` | the same five measures read as distances: `TrigramsSimilarityDistance`, `TrigramsWordSimilarityDistance`, `TrigramsStrictWordSimilarityDistance`, and the two inverted ones, `TrigramsStrictWordSimilarityDistanceInverted`, `TrigramsWordSimilarityDistanceInverted` |
| `where[Distance(x.DateTime, x.DateTime) > 00:02:00]` | the difference of two moments as a `TimeSpan`, compared with a span the query writes; of the two overloads it is the `DateTime` pair that answers with the span, the `DateOnly` one with a number of days |
| `where[ToDate(x.BornText, yyyy-MM-dd) > 2020-01-01]` | a date read out of a text, the second argument being the format, compared with a date the query writes |

A function of several overloads is reached by the overloads whose parameters the row and the
written values fill. What a call answers with is compared as any other expression: with a column
of that type, or with a value of it the query writes - a `DateOnly` or a `TimeSpan` included.

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

# MySQL

`AddMySqlToHLinq()` hands a query three classes: the generic EF `DbFunctionsExtensions`, and the
two of the Pomelo provider, `MySqlDbFunctionsExtensions` and `MySqlJsonDbFunctionsExtensions`. How
a call is written, and what its arguments may be, is in [the page beside this one](README.md).

## Answering with a truth

| call                                             |                                          |
|--------------------------------------------------|------------------------------------------|
| `where[Like(x.Name, %Bill%)]`                    | the third argument is the escape character |
| `where[IsMatch(x.Name, Bill, NaturalLanguage)]`  | full-text search; the mode is `NaturalLanguage`, `NaturalLanguageWithQueryExpansion` or `Boolean` |
| `where[JsonContains(x.Json, x.Candidate)]`       | the third argument is the path inside the document |
| `where[JsonContainsPath(x.Json, $.a.b)]`         |                                          |
| `where[JsonOverlaps(x.Json, x.Other)]`           | both documents come from the row: a query writes no document of its own |
| `where[JsonSearchAny(x.Json, Bill)]`             | the third and fourth arguments are the path and its escape character |

## Answering with a value

| call                                                |                          |
|-----------------------------------------------------|--------------------------|
| `where[DateDiffDay(x.From, x.To) > 7]`              | a number - and `DateDiffHour`, `Minute`, `Second`, `Millisecond`, `Microsecond`, `Nanosecond`, `Tick`, `Week`, `Month`, `Quarter`, `Year` |
| `where[Match(x.Name, Bill, NaturalLanguage) > 1]`   | how well the row matches |
| `ConvertTimeZone(x.Created, UTC)`                   | a moment in a time zone, when there is one |
| `Degrees(x.Angle)` / `Radians(x.Angle)`             | an angle in the other unit |
| `Unhex(x.Hex)`                                      | the text a hexadecimal spelling stands for |
| `JsonType(x.Json)`                                  | what kind of value the document holds |
| `JsonQuote(x.Text)` / `JsonUnquote(x.Json)`         | text as a document spells it, and back |

## Out of reach

- `Random` takes no argument, and a call written with none is not a call: there is nothing between
  the parentheses for the query to mean.
- `JsonContainsPathAll` and `JsonContainsPathAny` want the paths as an array, and a query writes no
  array - two paths named one after the other are not one.
- `IsMatch` and `Match` over *several* columns want them as an array of texts; the same call of one
  column is the reachable one.
- `AsJson` answers with a document of the provider: the call is built, but nothing a query holds is
  such a document to compare it with, so no filter is made of it.
- What the provider offers as SQL and not as an extension method is not in these classes: a pack
  hands over classes, not a dialect.

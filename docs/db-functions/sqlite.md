# SQLite

`AddSqliteToHLinq()` hands a query one class: the generic EF `DbFunctionsExtensions`. How a call is
written, and what its arguments may be, is in [the page beside this one](README.md).

## Answering with a truth

| call                          |                                            |
|-------------------------------|--------------------------------------------|
| `where[Like(x.Name, %Bill%)]` | the third argument is the escape character |

## Answering with a value

Nothing: of the two names the class holds, one answers with a truth and the other is below.

## Out of reach

- `Random` takes no argument, and a call written with none is not a call: there is nothing between
  the parentheses for the query to mean.
- The provider's own class, `SqliteDbFunctionsExtensions`, is not handed over: `Glob`, `Hex`,
  `Unhex` and `Substr` are not words of the language until an API hands over that class, as any
  other table of words is handed over. Of those four, `Glob` is the one a query could use as a
  filter of its own - the others answer with bytes, which nothing of a query is compared with.

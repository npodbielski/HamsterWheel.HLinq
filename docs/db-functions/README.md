# Db functions in a query

A pack is installed by `ConfigureHLinq`, as [the README describes](../../README.md#adding-ef-db-functions).
The pages next to this one say what each engine hands a query, and what stays out of its reach:
[Postgres](postgres.md), [SQL Server](sql-server.md), [MySQL](mysql.md), [MariaDB](mariadb.md),
[SQLite](sqlite.md).

## A call and its arguments

A function is written the way it is in Linq, with its arguments in place: `where[IsDate(x.Born)]`,
`where[ILike(x.Name, %Bill%)]`. The arguments are matched in the order of the parameters.

An argument is either

- **a column** - `x.BirthDate` - which goes to the server as a column and not as a value. It is
  taken when the parameter's type is the column's type, or a reference type a column of that type
  stands in for: an `Object` parameter takes the text column of a row, while a parameter of a
  value type wants exactly that value type - a `DateTime?` parameter wants a column of `DateTime?`,
  not one of `DateTime` - or
- **a value** - what the query itself writes: text, a single character, or the member of an enum
  (`NaturalLanguage`, or `DayOfWeek.Monday` to name its enum).

Nothing else is a value: a parameter of another type is reached by a column of the row only, so
what a query can ask of a function depends on the row it is asked of. A call is refused - with
the arguments it was given and the ones the function offers - when there are too few arguments,
too many, or an argument nothing of the call fits.

## A filter, or an operand of a comparison

A function answering with a truth is a filter of its own: `where[FreeText(x.Summary, Bill)]`.

A function answering with a value is asked about that value:
`where[DateDiffDay(x.From, x.To) > 7]`, `where[Reverse(x.Name) == ylliB]`. The result is compared
as any other expression, so a function returning something nothing of a query is - a list, a
document of the provider - builds a call no filter is made of.

A pack is the list of what one database can do, not a promise about what a query may ask: a
function its provider cannot translate fails at the database, as the same call written by hand in
Linq does.

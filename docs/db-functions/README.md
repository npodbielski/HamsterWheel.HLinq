# Db functions in a query

A pack is installed by `ConfigureHLinq`, as [the README describes](../../README.md#adding-ef-db-functions).
The pages next to this one say what each engine hands a query, and what stays out of its reach:
[Postgres](postgres.md), [SQL Server](sql-server.md), [MySQL and MariaDB](mysql.md), [SQLite](sqlite.md).

## A call and its arguments

A function is written the way it is in Linq, with its arguments in place: `where[IsDate(x.Born)]`,
`where[ILike(x.Name, %Bill%)]`. The arguments are matched in the order of the parameters.

An argument is either

- **a column** - `x.BirthDate` - which goes to the server as a column and not as a value. It is
  taken when the parameter's type is the column's type, or a reference type a column of that type
  stands in for: an `Object` parameter takes the text column of a row, while a parameter of a
  value type wants exactly that value type - a `DateTime?` parameter wants a column of `DateTime?`,
  not one of `DateTime` - or
- **a value** - what the query itself writes, in the text of its own type: text, a single
  character, the member of an enum (`NaturalLanguage`, or `DayOfWeek.Monday` to name its enum), a
  number (`7`, `7.5`), a GUID, a date (`2020-01-01`), a moment (`2020-01-01T14:30`), a time
  (`14:30`) or a span (`00:02:00`, or `730.00:00:00` of days, hours and minutes). A text that
  parses into the parameter's type fills it, and when several overloads could take it the one the
  text is written in the shape of wins - a written `2020-01-01` goes to a `DateOnly` parameter
  over a `DateTime` one, a written `2020-01-01T14:30` the other way round.

Nothing else is an argument: a parameter of a type no text parses into - a tuple, a sequence, a
type of the provider - is reached by a column of the row only, so what a query can ask of a
function depends on the row it is asked of. A call is refused - with the arguments it was given
and the ones the function offers - when there are too few arguments, too many, or an argument
nothing of the call fits.

## A filter, or an operand of a comparison

A function answering with a truth is a filter of its own: `where[FreeText(x.Summary, Bill)]`.

A function answering with a value is asked about that value:
`where[DateDiffDay(x.From, x.To) > 7]`, `where[Reverse(x.Name) == ylliB]`. The result is compared
as any other expression, so a function returning something nothing of a query is - a list, a
document of the provider - builds a call no filter is made of.

A comparison is written with a value of the type it compares with, and a moment of a query is
written in the type's own text: a date (`2020-01-01`), a time (`14:30`), a span (`00:02:00`, or
`730.00:00:00` of days, hours and minutes). A span written as a bare number counts that many days,
so `> 2` compares with two days.

A pack is the list of what one database can do, not a promise about what a query may ask: a
function its provider cannot translate fails at the database, as the same call written by hand in
Linq does.

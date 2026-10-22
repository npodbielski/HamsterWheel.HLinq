# MariaDB

`AddMariaDbToHLinq()` hands a query the same three classes the [MySQL page](mysql.md) lists - the
generic EF `DbFunctionsExtensions` and the two Pomelo classes, `MySqlDbFunctionsExtensions` and
`MySqlJsonDbFunctionsExtensions`. MariaDB is served by Pomelo's MySQL provider, so the two packages
name the same functions and the reachable surface is the one written there, down to the reasons.
How a call is written, and what its arguments may be, is in [the page beside this one](README.md).

## Answering with a truth

`Like`, `IsMatch`, `JsonContains`, `JsonContainsPath`, `JsonOverlaps`, `JsonSearchAny`.

## Answering with a value

The twelve `DateDiff` units, `Match`, `ConvertTimeZone`, `Degrees`, `Radians`, `Unhex`, `JsonType`,
`JsonQuote`, `JsonUnquote`.

## Out of reach

`Random` - a call written with no argument at all; `JsonContainsPathAll` and `JsonContainsPathAny` -
they want the paths as an array; `IsMatch` and `Match` of several columns - they want the columns as
an array; `AsJson` - it answers with a document of the provider, which nothing of a query compares.

The two packages are separate, and stay so: an API on MariaDB installs `AddMariaDbToHLinq`, and the
words it then understands are the words above. What the server behind them answers to a given call
is Pomelo's and MariaDB's business - a call the provider does not translate fails at the database,
which is what the same call written by hand in Linq does too.

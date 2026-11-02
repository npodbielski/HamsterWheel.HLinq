# The surface of a pack - a record of every name, and a tree per engine

Closes #19. `docs/db-functions/` says what each engine hands a query and what stays out of its
reach, and `ProviderFunctionSurfaceUnitTests` is named as the record of it (`plans/done/
issue-18-provider-packs.md`). The two are not the same set: the record asks half the names, in one
shape, of three of the five engines, and the docs' two further tables - value-returning names and
out-of-reach names - are prose no test holds.

## What is guarded today

| engine | a tree is built and read back | the surface is recorded |
| --- | --- | --- |
| Postgres | yes (`PgSqlFunctionsUnitTests`) | no |
| SQL Server | yes (`SqlServerFunctionsUnitTests`) | no |
| MySQL, MariaDB, SQLite | only `Like`, in one shared test | yes, in one shape |

The record (`ProviderFunctionSurfaceUnitTests`) keeps names and boolean-names counts per class, and
calls a boolean name bindable when `where[{name}(x.Name, abc)]` builds. So it never asks:

- a name answering with a value (`DateDiffDay`, `Reverse`, `JsonType`) - not a `bool` return, so
  never probed at all;
- a name one column and one text cannot fill (`JsonContains(x.Json, x.Other)`,
  `IsMatch(x.Name, Bill, NaturalLanguage)`) - the probe writes exactly those two arguments;
- a name of Postgres or SQL Server - the record covers the three packs of !65 only.

This is how the surface went stale quietly once: the record disagreed with the binder and nothing
noticed, because the record was a shape from the past rather than a question about the signatures.

## What was measured

The declared surface, read off the assemblies the packs hand over (the same reflection the record
already does): EF `DbFunctionsExtensions` holds 3 methods, SQL Server's class 136, Npgsql's two
classes 13 + 14, Pomelo's two 94 + 14, SQLite hands over only the generic EF class. Of those,
`DummyEntity` answers as the row in every case the docs call reachable: an `Object` parameter takes
the text column of the row (a reference type a column of that type stands in for), so
`JsonContains(x.Name, x.Text)` binds even though the entity holds no json column - which is why no
new entity is needed.

`Expression.Call` - the last step the binder itself takes - enforces the pairing on its own
(measured against the provider assemblies): it refuses an `int` column for a `String` parameter,
a `String` column for an `Object` parameter, an `int` constant for a `String` parameter. The
framework is the argument rule; a probe does not have to restate it, it has to ask it.

The hand-written EF call and the tree the binder builds are the same tree up to one normalization:
`x => EF.Functions.Like(x.Name, "%B%")` compiles to a call of the very same `MethodInfo`
(verified), with the `DbFunctions` this-parameter as the instance, while the binder builds
`null.Like(<null DbFunctions>, Param_0.Name, "%B%")` - the this-parameter as a null constant
(`StaticMethodSourceWrapper.MapEfDbFunctionParams`). Same method, same arguments in the same
order, one offset for the this-parameter: the two trees are comparable member by member.

## Design

### The record

One record per pack - all five, not the three of !65 - in a file of its engine beside its tree
test (`PgSql/`, `SqlServer/`, and the three packs), so "where do I look for MySQL's surface" is
the same answer for the record and for the trees; the probe that asks the question is one shared
helper the five files call. Built by asking every declared name of every registered class the
question its own signature asks, through the same `ExpressionBuilder.GetFilter` the engine tests
use. Names are deduplicated across overloads (a name is in a set when any overload
of it lands there, which is how the binder sees competitors); the counts the record keeps today
(`names/boolean names` per class) stay.

Per name the probe fills each parameter with the first thing that *binds*: a column of
`DummyEntity` that `Expression.Call` accepts for the parameter, else a written text, a single
character, the member of an enum - and `Expression.Call`, the framework's own type check, is what
decides acceptance, so the probe restates no rule of the binder. A name whose every overload
refuses every filling is out of reach. The probe sorts the names into one of three recorded sets:

- **filter names** - answers with a truth, binds as a filter of its own: `where[Name(args)]`;
- **value names** - answers with a value of a type a comparison of the row can be made of, and is
  probed as one: a column of the return type when the row holds one, else a written value of that
  family (a number, a text, a date, an enum member) - `where[DateDiffDay(x.DateTime, x.DateTime)
  >7]`, `where[JsonQuote(x.Name) == y]`;
- **out of reach** - with the reason the record states, not the docs' prose:
  - a parameter no column of the row and no written value binds - the recorded reason names the
    parameter (`String[] paths`, `IEnumerable<decimal> values`, the provider's own document type),
    which is the docs' "Out of reach" section as data;
  - a call with no arguments at all (`Random()`) - nothing between the parentheses for a query to
    mean;
  - a return type nothing of a query is compared with (`String[]`, `MySqlJsonString`, a document) -
    the call builds but no filter is made of it, so the reason names the return type.

The record is a literal in the test, as it is today: when a pack widens, its record changes in the
same commit or the test says so - `JsonContainsPath` joining the boolean set unnoticed is the case
that was missed.

### The trees - the EF call is the oracle

For every name the docs list as reachable, the test holds the EF call an author writes by hand -
`x => EF.Functions.Like(x.Name, "%B%")`,
`x => SqlServerDbFunctionsExtensions.DateDiffDay(EF.Functions, x.Born, x.Born) > 7` - and the
HLinq query that means the same thing, and asserts the two expression trees are the same tree:
the same `MethodInfo`, and each argument of the binder's call matching the twin's - a
`MemberExpression` of the same property path where the twin reads a column, a `Constant` of the
same value and type where the twin writes a value, with the one normalization of the this-parameter
(a null constant in the binder's call, the instance in the lambda). The C# compiler resolved the
overload and typed the arguments when the twin was written; the binder has to arrive at the same
tree from the query text, and any place it does not - a different overload, a column arriving as a
`Constant` of its text (the mistake that made a filter compare a row against a copy of itself), a
reordered argument - is a tree mismatch.

Each of the five engines gets such tests for its docs' names, both tables - at least a filter name
and a value name per engine, each in the argument shape the docs write - in the file of its engine
beside the record. The Postgres and SQL Server tests of today keep their hand-written expectations
where they assert something the twins cannot (refusals and their messages); the twin comparison is
what the reachability assertions become.

### The docs

The two tables of each `docs/db-functions/` page become the prose of the two recorded sets, and
the "Out of reach" section the prose of the third. A test reads the names out of the pages' tables
and fails when a page names a call the record does not hold, or the record holds a name no page
names - the `readme-check` job is the precedent for a test that reads a page of this repo.

## Open decisions

1. **Resolved: a file per engine.** Each pack's record lives beside its tree test, and one shared
   probe asks all five the same question - the same question is what makes a provider difference
   show up as a difference in the records, the files are only where the answers are kept.
   `ProviderFunctionSurfaceUnitTests` is split into the five per-engine files.
2. **Resolved: the framework and the EF twin are the oracle.** The probe does not mirror the
   binder's pairing rule and does not share a helper with it - it fills a parameter with the first
   column or value `Expression.Call` accepts, so the framework's own type check decides, and the
   reachability assertions compare the binder's tree against the hand-written EF call. Neither a
   mirror nor a shared helper can disagree with the binder the way an independent tree can.
3. **Value-name probe per return family.** A column when the row holds one of the return type,
   else a written value; a return type with neither (a `TimeSpan` no query writes and no
   `DummyEntity` column holds) is out of reach by the return-type rule, not a failure to probe.

## Delivery steps

- **S1** - the probe: fill-each-parameter-with-what-`Expression.Call`-accepts, the three sets
  computed for all five packs, `ProviderFunctionSurfaceUnitTests` split into a record file per
  engine with its literal written out and asserted; run it against the docs and fix whichever of
  the two is wrong wherever they disagree.
- **S2** - the value names: comparison probes per return family, recorded beside the filter names;
  the counts of today kept next to the sets.
- **S3** - the out-of-reach set with its reasons (the parameter that stopped it, the empty call,
  the return type nothing compares), and the docs-vs-record test so the pages and the record cannot
  drift apart.
- **S4** - the EF-twin tree tests per engine: the docs' names written as EF lambdas and as HLinq
  queries, the two trees compared - method, arguments in order, column where the twin reads one and
  value where the twin writes one; README untouched (no behavior of the binder changes),
  `docs/db-functions/` corrected where S1 found it wrong.

## Out of scope

- Nothing runs against a database. The tree is built from the query text and read back; that a
  provider *translates* a call is the provider's promise, and demo projects per engine would prove
  the provider's promise, not the binder's.
- No new packages, no new entities beyond what `DummyEntity` already holds.
- Widening a pack (handing over `SqliteDbFunctionsExtensions`, Npgsql's json or network classes) -
  the record is what makes such a widening visible, it does not decide it.
- Instance methods of a row (`x.Name.Length`) and the `MethodSource` machinery around them.

## Definition of done

`dotnet build HLinq.slnx`; unit, netstandard-unit and integration suites green;
`dotnet format HLinq.slnx --verify-no-changes` clean; every declared name of every registered class
of all five packs in exactly one recorded set with its reason; a tree test per engine comparing
the binder's tree against the hand-written EF call - method, argument order, column-vs-value
arguments; the docs' tables and the record checked against each other by a test; branch pushed,
MR opened against `alpha`, pipeline watched to green, then stop and wait for review.

# Grouping: `groupBy[...]` - one entry per key, the rows under it

Addresses the first line of the README roadmap.

## What was decided

- **`groupBy[x.Status]` alone answers a dictionary of arrays**: `{ "New": [row, row], "Paid": [row] }`.
- **A `select` after a `groupBy` works on a row, not on the group**, with one extra name available:
  `x.Key`, the key of the group the row is in. `groupBy[x.Status].select[x.Name]` answers
  `{ "New": ["Ann", "Bob"], "Paid": ["Cat"] }`, and `select[x.Key, x.Name]` repeats the key beside
  each row. No `x.Value.` prefix, and no nested roots: the type a `select` reads its properties from
  stays a row, so `select` keeps being one clause over one row everywhere it appears.
- **`take`, `skip`, `orderBy` and `count` after a `groupBy` work on groups** - `take[2]` keeps two
  groups, `count[]` answers how many groups there are.
- **Aggregates come later.** Counting rows per key is a real use case - "how many rows are older than
  this year" is exactly `groupBy[year].count` - and it is not in this plan. See *Out of scope* for
  what the measurement says about it.
- **Joins are out** and stay out: HLinq queries one resource, it is not a graph query language.

## Measured: what the provider translates

Run against the real Postgres of the integration suite through `ToQueryString`, so no rows are moved.
This corrects an earlier claim in this plan's first draft that a grouping always materializes - it
does not, and the difference matters.

| Linq | what the server is asked |
| --- | --- |
| `GroupBy(p => p.LastName)` | `SELECT p."LastName", p."Id", ... ORDER BY p."LastName"` - every row, ordered by the key; the grouping itself is done here |
| `GroupBy(...).Select(g => new { g.Key, Count = g.Count() })` | `SELECT p."LastName" AS "Key", count(*)::int AS "Count" FROM "Persons" GROUP BY p."LastName"` - a real `GROUP BY` |
| `GroupBy(...).Select(g => new { g.Key, Rows = g.Select(p => p.FirstName).ToList() })` | `SELECT p1."LastName", p0."FirstName", ... FROM (SELECT ... GROUP BY p."LastName") p1 LEFT JOIN "Persons" p0 ON p1."LastName" = p0."LastName"` - the groups are made by the server and the rows joined back |
| the same with `.Take(2)` | as above, with `LIMIT 2` **inside** the grouped subquery |
| `GroupBy(...).Select(Key, Count).Where(x => x.Count > 2)` | adds `HAVING count(*)::int > 2` |
| `GroupBy(...).Take(2)`, `.OrderBy(g => g.Key)`, `.Where(g => g.Key == "x")` | **not translated** - `InvalidOperationException` |

Three facts fall out:

1. **The shape that translates is key plus rows in one projection.** A bare `GroupBy` sends every row
   and groups here; `Select(g => new { g.Key, Rows = ... })` lets the server make the groups. So
   HLinq should build the second shape, which is also what the decided syntax means: the key of the
   group and the rows of the group, named together.
2. **Ordering and paging must be inside that projection**, not applied to an `IGrouping` afterwards -
   the last row of the table is the one that refuses, and it is the shape a query written after a
   `groupBy` would naively produce.
3. **An aggregate is not a translation problem.** `count(*)` and `HAVING` are there in the SQL above;
   what HLinq lacks is a way to write a call as a value (`select[R=Reverse(x.Name)]` is refused
   today). That is a syntax question, and it is a separate plan.

## Design

### One projection: the key and the rows

`groupBy[x.Status].select[x.Name]` is one `Queryable.Select` over the grouping, not a `groupBy` step
followed by a `select` step:

```
GroupBy(x => x.Status).Select(g => new { Key = g.Key, Rows = g.Select(r => new { Name = r.Name }).ToList() })
```

The row projection reuses `PropertyAssignment` exactly as `select` does today, with the row as its
source and `Key` added to what a name may resolve to - which is why `Key` is built first: if it
resolves as an ordinary property of the row context, the projection needs no new converter.

`groupBy` with no `select` is the same projection with every row of the entity as the row shape.

### The response is a dictionary

`ApplyGetType` ends the pipeline with `Enumerable.ToArray` and hands back a list; every root hands the
next one an `IQueryable` and a current result type. A dictionary of arrays is neither, so the
grouping is where the queryable chain ends and the response begins: the list of `{Key, Rows}` the
server answers is folded into `{ key: rows }` on the way out. Nothing grouping-specific is added to
serialisation beyond carrying a dictionary where today only a list fits.

### Ordering and paging after a `groupBy`

Written **before** `groupBy` they page rows and are the database's. Written **after** it they page or
order groups - and per the measurement they have to be applied inside the projection (`LIMIT` in the
grouped subquery), not to an `IGrouping` afterwards, which the provider refuses. `count[]` after a
`groupBy` counts groups; the exact translatable form for that is to be measured in S1, since the
table above covers `Take` and not `Count`.

### `Key` is a name of the row context, and what it collides with

`Key` resolves only where the query is grouped; on a plain row it stays the
`InvalidPropertyPathException` it is today. When the entity has a property of its own called `Key`,
the two collide - the plan's answer is to report the ambiguity the way member resolution reports an
ambiguity today, never to guess; whether the entity's own property should win, or the query should be
refused, is an open decision below.

A composite key (`groupBy[x.Vendor, x.Category]`) is the anonymous type `SelectRoot.Converter` builds
today, so `x.Key.Vendor` is a path of two members. As a dictionary key it has no string form - see
the open decisions.

### The words of the other languages, and the client

`groupBy` is a root token, so `HamsterWheel.HLinq.Lang.Pl` gets a word beside `gdzie` and `wybierz`
(`grupujPo`, to be settled), and the grammar validator runs over the language as it is built. The
client gains `GroupBy<TKey>(...)`, and the builder it returns deserialises a dictionary of arrays.

## Out of scope

- **Aggregates** (`Count`, `Sum`, `Average`, `Min`, `Max`) and any call in a value position, in
  `select` or in `orderBy`. The measurement says the SQL is available; what is missing is the syntax,
  and it is one mechanism shared with the roadmap's nested count (`[NumberOfAddresses=x.Addresses.Count[]]`).
  One plan should answer both, and grouping does not need it to exist first.
- `GroupJoin`, `Join`, any query over two resources.
- A key selector that is not a property path (a call, an arithmetic expression).
- A `where` after a `groupBy` reading a member of the group.

## Open decisions

1. **`Key` on an entity that has its own `Key`** - the entity's wins, or the query is refused with both
   candidates named.
2. **A composite key as a dictionary key** - a dictionary needs a string. Either a composite grouping
   answers an array of `{Key, Rows}` pairs instead, or the key is spelled out of its parts.
3. **`count[]` after a `groupBy`** - the translatable form, measured in S1.
4. **A grouping with no `select` on a large table** sends every row, ordered - the honest answer is
   `where` before `groupBy`, and the README should say so rather than let a query fetch a table.

## Delivery steps

- **S1** - measure the remaining shapes (`count` on a grouping, ordering groups, the composite key)
  against the provider and settle the open decisions; build `Key` resolving in a grouped row context.
- **S2** - the `groupBy` root, the key-plus-rows projection, paging and ordering inside it, and the
  dictionary response; unit tests on an in-memory source, integration tests against Postgres asserting
  the result against hand-written Linq.
- **S3** - the Polish word, the client, README (the clause where the other roots are documented, and
  the roadmap line ticked).

## Definition of done (per AGENTS.md)

`dotnet build HLinq.slnx`; unit, netstandard-unit and integration suites green (integration against
postgres); `dotnet format HLinq.slnx --verify-no-changes`; the grammar validator run over every
language with the new word; README roadmap line ticked and the clause documented where the other
roots are documented; branch pushed, MR opened, pipeline watched to green, then stop and wait for
review.

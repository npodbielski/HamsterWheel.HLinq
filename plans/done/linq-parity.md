# Linq <-> HLinq parity - which Linq operators does HLinq not speak yet?

HLinq is a query language over `IQueryable`, so its natural horizon is the Linq operator set:
every operator Linq has either has an HLinq spelling, has a deliberate reason not to, or is a gap
nobody has named yet. This plan built that inventory - every public `Queryable` operator (60, the
.NET 10 surface included) in one of exactly three states - and turned the expressible gaps into
roadmap lines.

## What was measured

The operator set enumerated from `System.Linq.Queryable` by reflection, each operator checked for
an HLinq spelling in README and tests, and everything without a spelling probed as the equivalent
expression tree against all three packs' providers (EF Core 9: npgsql, sqlite, pomelo - SQL read
from `ToQueryString`, no database needed).

### Spoken

| Linq | HLinq |
| --- | --- |
| `Where` | `where[...]` - and/or/not, groups, comparisons, math, db functions |
| `OrderBy[Descending]` / `ThenBy[Descending]` | `orderBy[...]` / `thenBy[...]`; the key may be computed: `orderBy[(x.A+x.B)*2]` |
| `Select` | `select[...]` - members, renames, constants, calls, math, nested type selects |
| `Skip` / `Take` | `skip[n]`, `take[n]`, `take[a..b]` (ranges are a HLinq extra) |
| `Count` | `count[]`, also after `groupBy` (then it counts groups - which makes a distinct-value count expressible as `groupBy[x.Status].count[]`) |
| `GroupBy` + key | `groupBy[k1,k2]`, nested `groupBy`; the `{Key,Rows}` answer is a HLinq extra |
| group aggregates `Count/Sum/Average/Min/Max/Any` | `x.Sum(x.Age)`, `x.Any()` - after `groupBy` in where/orderBy/select, and over a navigation in a select (`select[N=x.Addresses.Count()]`) |
| `string.Contains/StartsWith/EndsWith` (+ case-insensitive) | `x.Name.Contains(...)`, in `where` and in `select` |

Correction to this plan's starting table: **`in` is not spoken** - no membership operator exists
in grammar, tests or README; the table claimed it from the plan's own draft.

### By design absent (each with its measured ground)

- `First/FirstOrDefault/Last/Single/ElementAt` as roots - a query answers a page; `take[1]` says
  First. As scalar subqueries EF translates them on postgres only (`LEFT JOIN LATERAL`); sqlite
  and mysql need APPLY, so they are unreachable even as `select` values.
- `Join/GroupJoin/LeftJoin/RightJoin/SelectMany` - navigations are the join, nested selects shape
  the collection. Measured: `SelectMany` over a navigation does translate (INNER JOIN), but it
  changes the row's entity mid-query, which the one-root-entity answer refuses.
- `Include` and eager loading - the answer is shaped by `select`, not by loading (an EF extra).
- `FromSql`, `AsQueryable`, `Shuffle` - raw SQL, or not a query operator, or non-determinism.
- `Aggregate`, `AggregateBy`, `CountBy`, `MinBy`, `MaxBy`, `SequenceEqual`, `Zip`, `Chunk`,
  `Index`, `TakeWhile`, `SkipWhile`, `Append`, `Prepend`, `TakeLast`, `SkipLast` - each needs a
  second sequence, an accumulator lambda, or order semantics SQL cannot express; measured: EF
  itself refuses every one of them on all three engines.
- `Reverse` - bare: EF refuses; with an ordering it is just that ordering reversed, which
  `orderByDescending` spells.
- `Cast`/`OfType` - EF translates them, but they are CLR type-system tools; the entity type is the
  host's and a query does not pick types.
- `Order()/OrderDescending()` without a key - EF orders by primary key; a HLinq ordering always
  names its key.
- Set operators `Union`/`Concat` translate on all three packs, `Intersect`/`Except` only on
  postgres and sqlite - but every one of them needs a second root sequence in the language, which
  a query (one root entity) does not have; they stay absent with that reason, not on the roadmap.

### Gaps - the verdicts, each measured

- **list membership** - `where[[Paid,New].contains(x.Status)]`: a written collection is the
  source of the LINQ `Contains` (the LINQ spelling of SQL `IN`), and EF answers
  `new[]{...}.Contains(x.Id)` as `WHERE ... IN (...)` on all three packs. Grammar: a window
  opening with `[` dies in the tokenizer today, so nothing clashes; the list literal is one token
  whose own text rule swallows to its first `]`, the way `RangeOperator` absorbs `10..` -
  lexical, no lookahead. **Roadmap.**
- **`distinct[]`** - `groupBy` counts distinct values of a column already, but as an answer
  shaped like groups; a deduplicated projection (`select[x.Status].distinct[].count[]`) is the
  shape an application writes. EF translates `Distinct` on all three packs. **Roadmap.**
- **column folds** - `sum[x.Age]`, `avg`, `min`, `max` beside `count[]`: each translates as a
  scalar subquery on all three packs; the answer shape (rows or one number) waits for the
  union-type decision (#24). **Roadmap, after #24.**
- `DefaultIfEmpty` translates on all three packs but the row-of-nulls is an artifact for a JSON
  consumer - rejected, moved to by-design.
- `DistinctBy`, `UnionBy/IntersectBy/ExceptBy` - EF refuses them on all three engines; a gap EF
  cannot translate is *no*.

## Decided

- **Three states only** - a row that cannot decide between *by design* and *gap* is a row the plan
  has not finished; the matrix is honest or it is noise.
- **The inventory is the deliverable**, and its gaps became roadmap lines - the plan does not
  build the operators it finds (each is its own plan later); it made the map true.
- **EF behaviour was measured per gap** with scratch expression-tree probes over the three
  engines; a gap EF cannot translate is *no*, not *roadmap* - this disqualified more than half of
  the unspellen operators on its own.
- **Which Linq**: the `IQueryable` surface of `System.Linq.Queryable`, .NET 10 included - the
  `IEnumerable`-only shape of the aggregation operators is what EF refuses, so both readings
  agree.
- **The packs are the uniformity test**: a *spoken*-worthy gap requires all three to translate it;
  the set operators showed the rule working (mysql refuses `Intersect`/`Except`).

## Steps

- **S1** - the operator set enumerated by reflection (60 public `Queryable` methods, plus the EF
  extras `Include`/`FromSql`/`LeftJoin`/`RightJoin`/`Shuffle`), spoken rows read off README and
  tests - where the plan's starting table itself got corrected (`in` was never spoken).
- **S2** - every gap probed as the equivalent expression tree on all three engines, with the
  grammar feasibility note (list membership is a token, not a lookahead question) and the verdict
  above.
- **S3** - the three roadmap lines landed in the README Roadmap (!141), each naming its query and
  its measured engine behaviour; the matrix file `docs/linq-parity.md` and the compiled parity
  test were deliberately dropped when the maintainer asked for a report, not code - the plan's
  result lives here and in the Roadmap, and keeping a second prose matrix only gives it a way to
  rot.

## Out of scope (as delivered)

- Building any operator - the three roadmap lines are each their own plan later.
- The db functions - already inventoried per engine in `docs/db-functions/`.
- `IQueryable` providers other than EF Core - parity is stated against EF Core, the only provider
  the packs target.

## Definition of done (as delivered)

A report, not code, per the maintainer's ruling: the inventory above, the three roadmap lines in
the README through !141, and this plan moved to done.

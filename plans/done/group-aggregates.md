# Aggregates over group rows - `where[x.Count()>5]`, `orderBy[x.Max(x.Age)]`

The classic `HAVING`. The grouping plan deferred it: "aggregates are a separate plan, the SQL is
there and the syntax is not" (`plans/grouping.md`, Out of scope). This plan answers both halves
against the provider, and it is also the plan the roadmap's nested count
(`[NumberOfAddresses=x.Addresses.Count()]`) belongs to - one mechanism, a call in a value position.

## What was measured

Run against the real Postgres of the integration suite through `ToQueryString`, EF Core 9 / Npgsql,
so no rows are moved:

| Linq over a grouping | what the server is asked |
| --- | --- |
| `Select(g => new { g.Key, Count = g.Count() })` | `SELECT ... count(*)::int ... GROUP BY` - a real aggregate |
| the same `.Where(x => x.Count > 2)` | adds `HAVING count(*)::int > 2` |
| `Select(g => new { g.Key, Max = g.Max(p => p.Age) }).OrderBy(x => x.Max)` | `ORDER BY max(p."Age")` |
| the same `.Where(x => x.Max > 15)` | `HAVING max(p."Age") > 15` |
| `Sum`, `Average`, `Min`, `Count(predicate)` in one projection | `sum`, `avg`, `min`, `count(*) FILTER (WHERE ...)` |
| `Key + Count + Rows` in one projection | the grouped subquery joined back, aggregates computed in it |
| aggregate in `Where` + `OrderBy` + `Take` | `HAVING ... ORDER BY ... LIMIT` - all in one statement |
| `Any(predicate)` as a value | `EXISTS (SELECT 1 ... WHERE outer.Key = inner.Key AND ...)` |

So every aggregate HLinq would want (`Count`, `Sum`, `Average`, `Min`, `Max`, `Any`, `All`) translates,
in `select`, in `where` (as `HAVING`) and in `orderBy`, and they compose with paging and ordering of
groups. Nothing here needs client-side evaluation.

What the tokenizer does today with the candidate syntax: `groupBy[x.Status].where[x.Count[]>5]`,
`orderBy[x.Max[x.Age]]`, `select[C=x.Count[]]` - all `UnknownTokenException`. `Count` is a root token
and `[` is a clause bracket; neither reads inside a property path.

## The syntax

**Decided: method syntax.** `where[x.Count()>5]`, `orderBy[x.Max(x.Age)]`, `select[C=x.Count()]`.

An aggregate is a call over the group, in a value position. HLinq already has calls in filters -
`where[x.Name.Contains(test)]` - spelled with a dot, a name and round brackets, so the tokenizer
already walks `MethodName` + `(...)` after a property path inside a filter; the work is letting the
same shape appear in `select` and `orderBy` (the roadmap item "methods in select" is then the same
mechanism) and resolving the zero-argument `Count()` / one-argument `Max(...)` over the group. No
new token, no collision with roots - the bracket spelling first considered (`x.Addresses.Count[]`)
would have collided with the `count[]` root and a bracket inside a value, which is what the grammar
rules were shaped to avoid.

The nested count of the roadmap is then `x.Addresses.Count()` - a call over a navigation collection,
same shape, and whether EF translates *that* (it is a subquery over a related table, not an
aggregate over the group) is measured in S1 before promising it.

## Design sketch

A grouped query already keeps its groups in the chain (`plans/grouping.md` + the deferred
projection of !85): the stream after `groupBy` is `IQueryable<IGrouping<TKey,TRow>>` and the
`{Key, Rows}` answer is projected once, at the end. An aggregate is a third thing a clause after a
grouping can name - after `Key` (allowed) and `Rows` (refused):

- `GroupingState` gains what the grouping's projection must answer beyond `Key` and `Rows`: a list of
  named aggregate calls over the group parameter (`g => g.Count()`, `g => g.Max(p => p.Age)`).
- A `where`/`orderBy`/`select` after a grouping that names an aggregate resolves it against that
  list: the first projection over the grouping carries `Key` + aggregates + rows, and the clause is
  applied *after* that projection - which is the shape the measurement shows becoming `HAVING` /
  `ORDER BY agg`. Applied to the `IGrouping` directly it is the shape the grouping plan measured as
  not translated.
- `GroupedResultPropertyValidator` stops refusing everything but `Key` and starts refusing
  everything that is not `Key` or a resolvable aggregate - `where[x.Name==John]` after a `groupBy`
  stays refused, `where[x.Count()>5]` does not.
- After a *nested* grouping the aggregates name the rows of the innermost group, same as `select`
  shapes the innermost rows - clauses still act on the outer groups of the chain.

The resolution of `Max`/`Sum`/... over `IGrouping<,>` goes through the existing `MethodsCache`
generic-method machinery; the set of names is a closed list (`Count`, `Sum`, `Average`, `Min`,
`Max`), not arbitrary LINQ methods over rows - `x.Name.ToUpper()` over a group is not an aggregate
and stays refused until "methods in select" has its own answer.

## Open decisions

1. **Resolved: `select` answers aggregates in the response, in this plan.** `select[C=x.Count()]`
   puts the value in the answer, `where[x.Count()>5]` only filters; both land together because the
   projection that carries the aggregate is the shared mechanism - the `select` half is the same
   `GroupingState` list read by the projection instead of by a condition. What `select` adds on top
   is narrow: `RightCircleBracket` is not grammar-legal directly after `LeftCircleBracket` (so
   `Count()` is refused today), and `PropertyAssignment` knows only a property path or a constant as
   its value - a method call is a third kind.
2. **Resolved: the first cut is the zero- and one-property-argument forms only** - `Count()`,
   `Min()`, `Max(x.Age)`, `Sum(x.Age)`. `Count(predicate)` / `Any(predicate)` need a condition
   grammar inside call arguments - the nested-conditions problem, which does not exist yet - and
   wait for it; the closed list of names stays, only their argument shapes grow.

## Delivery steps

- **S1** - measure the remaining shapes over the deferred-projection shape HLinq actually builds:
  ordering groups by an aggregate *after* paging, and `x.Addresses.Count()` (navigation, not group)
  so the roadmap line can be ticked or corrected.
- **S2** - aggregates in `select` after a grouping (the projection carries them), with in-memory unit
  tests and Postgres integration tests asserting the SQL shape (`HAVING`/`ORDER BY agg`) and the
  result against hand-written Linq.
- **S3** - aggregates in `where` and `orderBy` after a grouping; `GroupedResultPropertyValidator`
  widened as above; the closed list of aggregate names refused with a clear message when not an
  aggregate.
- **S4** - README (the Grouping clause gains the aggregates, the roadmap nested-count line ticked or
  corrected), Polish words if any aggregate name is translated, client surface if the answer shape
  changed.

## Out of scope

- `Count(predicate)` / `Any(predicate)` - a condition inside call arguments is the nested-conditions
  grammar, which does not exist yet; `Any()` without one is trivially true over a group.
- Calls in a value position that are not aggregates (`select[reverse(x.Name)]`) - the roadmap keeps
  that line; this plan's mechanism may absorb it later, it does not promise it now.
- `GroupJoin`/`Join`, grouping over two resources.
- Aggregates written *before* a `groupBy` (they would be over nothing grouped yet).
- `Distinct`, `LongCount`, `Aggregate(seed, f)` - the closed list can grow; the first cut is the five
  names above.

## Definition of done (per AGENTS.md)

`dotnet build HLinq.slnx`; unit, netstandard-unit and integration suites green (integration against
postgres); `dotnet format HLinq.slnx --verify-no-changes`; integration tests assert the translated
SQL contains `GROUP BY`/`HAVING` where the query says so - an aggregate silently evaluated
client-side is a failure, not a fallback; README documents the syntax and the roadmap line is
ticked; branch pushed, MR opened, pipeline watched to green, then stop and wait for review.

## What shipped - the plan reviewed against the implementation

- **The design's "third thing in `GroupingState`" landed for `select` only, and `where`/`orderBy`
  took the simpler shape the measurement had already shown.** An aggregate in a condition or an
  ordering applies to the grouping itself - `Where(g => g.Count() > 2)`, `OrderBy(g => g.Max(...))` -
  which the measurement above (repeated here before building) proved the engine answers as
  `HAVING` / `ORDER BY agg` inside the grouped subquery, paging and all. The clauses after a
  grouping therefore stay what they were - `Where`/`OrderBy` over `IQueryable<IGrouping<,>>` - and
  only `select` writes the aggregate list into the state for the final projection to answer.
- **`GroupedResultPropertyValidator` stayed as it was.** With the calls applying to the grouping
  itself, nothing moved into the "resolvable name" question the plan predicted: `Rows` is still
  refused with its reason, `Key` still resolves, and an aggregate call never asks the validator -
  the closed list of names (`NotAnAggregateException`) and the member path of its argument carry
  every refusal instead.
- **After a nested grouping the aggregates count the rows of the outer group**, matching what the
  grouping plan shipped for `take`/`where`/`count` - clauses act on the groups of the chain, not
  on the innermost group.
- **`select[Name=x.Count()]` answers under the name the query gave it** (`C=x.Count()`), beside
  `Key` and `Rows`; the answer type is the dynamic shape `select` already builds, `Group<TKey,TRow>`
  only when there are no aggregates. Naming an aggregate `Key` or `Rows` is refused.
- **A bare `Min()`/`Max()` aggregates a collection whose own values compare** - `x.Prices.Max()`
  of a list of numbers, dates or text (the engine unnests and folds them; measured on Postgres).
  Written over a group of whole rows it is refused with the reason: a row does not compare to
  another row, so `x.Max()` of a grouping needs its property argument.
- **The nested count line is ticked**: `select[N=x.Addresses.Count()]` - EF's correlated subquery,
  one statement, asserted on Postgres by the recorded statement, not only by the rows.
- **`AggregateItemType` was not needed**: the outer subject of every clause after a grouping is
  already the outer `IGrouping<,>`, so an aggregate over the subject reads its item type from the
  context it is built in.

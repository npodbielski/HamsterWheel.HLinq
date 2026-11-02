# Linq <-> HLinq parity - which Linq operators does HLinq not speak yet?

HLinq is a query language over `IQueryable`, so its natural horizon is the Linq operator set:
every operator Linq has either has an HLinq spelling, has a deliberate reason not to, or is a gap
nobody has named yet. Today the gaps are known one by one (the Roadmap lists several) but there is
no inventory: nobody can answer "can HLinq express this Linq query?" without spelunking. This plan
builds that inventory - a parity matrix, maintained, with each cell in one of exactly three
states - and turns the gaps it finds into roadmap entries.

## What was measured

The starting point, read off the shipped code and README:

| Linq | HLinq today |
| --- | --- |
| `Where` | `where[...]`, free-text values, `and`/`or`/`not`, comparisons, `in`, db functions |
| `OrderBy` / `ThenBy` | `orderBy[x.A].thenByDesc[x.B]` |
| `Select` (members) | `select[x.A,x.B]`, renaming, nested type selects (nested-selects plan) |
| `Skip` / `Take` | `skip[n]`, `take[n]` (capped) |
| `Count` | `count[]` |
| `GroupBy` + aggregates | `groupBy[key]` with `Count()`/`Sum()`/`Average()`/`Min()`/`Max()` |
| db functions | the packs (`docs/db-functions/`): per-engine whitelists of EF-translatable statics |
| `Distinct`, `Join`/`SelectMany`, set ops (`Union`/`Intersect`/`Except`), `Reverse`, `Chunk`, `Zip`, `Append`, `Aggregate`, `All`/`Any`/`First`/`Single`/`ElementAt`, `ToDictionary`/`ToLookup`, `Cast`/`OfType`, `DefaultIfEmpty`, `SelectMany`-style flattening | **no spelling** - some are unreachable by design (a query answers a page of a fixed entity; there is no join syntax - navigations are the join), some are simply unbuilt |

Also measured: the Roadmap already carries four of these gaps in operator form (nested counts,
methods in select, property operations in select, deeper query analysis), and the grammar design
(AGENTS.md) says how a new operator is added - token(s) + pattern + adjacency rule + tree element +
converter + builder - so the inventory must say, per gap, whether the grammar can admit it at all
(the tokenizer's no-backtracking rule is a real constraint: an operator whose syntax needs
lookahead is not a gap but a design question).

## Design

### The matrix

`docs/linq-parity.md`, one row per public `System.Linq` operator (and the EF Core extras that
matter to a query language: `Include`-style eager loading - answered by navigations + nested
selects; `FromSql` - out of the language by design). Each row:

- **Linq shape** - the C# one-liner.
- **HLinq shape** - the query, or the reason there is none.
- **State** - one of exactly three:
  - **spoken** - works; links to the README section and to the test that proves it.
  - **by design absent** - the language refuses it on purpose (e.g. `First()` - a query is a page,
    `Take(1)` says it; `Include` - the answer is shaped by `select`, not by loading). The cell
    carries the reason, AGENTS-style, once, here and not in code.
  - **gap** - expressible in principle, unbuilt. Each gap gets a paragraph: what the query would
    look like, whether the grammar can admit it under the tokenizer's rules, and what EF does
    with the equivalent expression tree (measured, not guessed - a scratch expression-tree probe
    per gap, like the nested-select plan measured the providers).
- **Verdict** for gaps: *roadmap* (worth a plan), *maybe* (needs an open decision), or *no*
  (rejected, with the reason - the row moves to by-design).

### The verification, so the matrix cannot rot

- A **parity test** in the unit project: for every row marked *spoken*, the HLinq query from the
  matrix parses and builds against the dummy model - the matrix is compiled, not prose. (Gaps are
  not tested; they are the plan's output, not a claim.)
- A **readme-check extension**: the parity file's *spoken* rows must link a README section that
  exists - the same anti-rot trick as the badge check.
- The **Roadmap gains a pointer** to the matrix, and every gap whose verdict is *roadmap* gets a
  roadmap line, so the two documents agree.

### What the first pass is expected to find (hypotheses to measure, not conclusions)

- `Distinct` looks like the cheapest real gap: `distinct[]` as a clause, `query.Distinct()` in the
  applier - one token, one tree element, EF translates it for a projected row.
- `Any`/`All` over a navigation in a filter (`where[x.Addresses.Any(a => a.City == Warsaw)]`) is
  the filter-side twin of the nested select; the nested-select tokenizer work (alias, inner call)
  is most of its grammar. Likely the highest-value gap.
- Set operators and `Zip`/`Chunk`/`Aggregate` will land *no* or *maybe*: they need a second
  sequence on the query, and a query has exactly one root entity.
- `Select` with expressions (the roadmap's "property operation in select") and nested `Count()`
  are already gaps with plans' worth of shape - the matrix links them instead of re-deciding.

## Decided

- **Three states only** - a row that cannot decide between *by design* and *gap* is a row the plan
  has not finished; the matrix is honest or it is noise.
- **The matrix is the deliverable**, and its gaps become roadmap lines - the plan does not build
  the operators it finds (each is its own plan later); it makes the map true.
- **EF behaviour is measured per gap** with scratch expression-tree probes over the three engines,
  the way the nested-select plan measured joins - a gap EF cannot translate is *no*, not *roadmap*.

## Open decisions

1. **Which Linq** - `System.Linq` over `IEnumerable`, over `IQueryable`, or both? (Propose: the
   `IQueryable` set, since HLinq builds expression trees; the `IEnumerable`-only operators
   (`Aggregate`, `Zip`, `Chunk`) then state *no* by construction.)
2. **Does the matrix track the packs separately?** A function is per-engine; an operator should be
   engine-uniform. Propose: one matrix, and a *spoken* row requires every pack to translate it (a
   row that works on two of three engines is *gap* with the engine named).
3. **Where `distinct[]` etc. go when built** - this plan only names them; each built operator is
   its own plan and MR.

## Delivery steps

- **S1** - enumerate the operator set (the `IQueryable` surface of `System.Linq` + EF extras),
  fill the matrix's *spoken* rows from the README and tests, and the obvious *no* rows with their
  reasons; commit the matrix with the parity test wired into the unit project.
- **S2** - the gap rows: for each, the scratch provider probe (what does the equivalent expression
  tree do on Postgres/SQLite/MySQL), the grammar feasibility note (can the tokenizer admit it
  without lookahead?), and the verdict.
- **S3** - roadmap lines for every *roadmap* verdict, README pointer to the matrix, readme-check
  extended to the matrix's links; move the plan to done.

## Out of scope

- Building any operator - the matrix decides *what is next*, it does not build it.
- The db functions - already inventoried per engine in `docs/db-functions/`; the matrix links
  there rather than duplicating.
- `IQueryable` providers other than EF Core - parity is stated against EF Core, the only provider
  the packs target.

## Definition of done (per AGENTS.md)

`dotnet build HLinq.slnx`; unit, netstandard-unit and integration suites green; `dotnet format
HLinq.slnx --verify-no-changes`; the parity test green; branch pushed, MR opened, pipeline watched
to green, then stop and wait for review.

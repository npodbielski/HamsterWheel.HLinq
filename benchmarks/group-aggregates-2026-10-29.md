# Tokenizer benchmarks: aggregates in a value position (`x.Count()`, `x.Max(x.Age)`)

Compared `alpha` (bf5ba77) with the group-aggregates change: `RightCircleBracket` completing directly
after `LeftCircleBracket` (the empty argument list), and `NameOrValue` refusing to *start* at a
parenthesis. Two adjacency/pattern rules, no new word - the question is what the walk pays for them
in queries that never use the syntax.

Method: the `TokenizerBenchmarks` queries plus the grouping and aggregate shapes the plan adds, run
as a tight loop (60 000 iterations, best of 5 rounds per process), the two builds **alternating
round-robin on one pinned core, six process pairs, first side alternated per pair**, best of the
processes per side. The alternation matters: a first batch with alpha always first showed the later
process systematically ~30% faster (a warm-core effect on a loaded machine), large enough to invent
or hide any delta this table could produce; alternating sides removed it (a null run of alpha against
alpha then lands inside the reported noise). BenchmarkDotNet `ShortRun` remains unfit here for the
same reason the nested-select notes give: its StdDev exceeds the deltas.

## Result

| query | alpha | branch | time | alloc |
|---|---|---|---|---|
| `where[x.Int==5]` | 1.55 µs | 1.59 µs | +2.6% (noise) | 1000 → 1000 B |
| chain (`where…select…orderBy…take`) | 7.82 µs | 7.97 µs | +1.9% (noise) | 2928 → 2928 B |
| select27 (27 renames) | 19.38 µs | 19.77 µs | +2.0% (noise) | 6280 → 6280 B |
| `groupBy[x.Status].select[x.Name]` | 2.54 µs | 2.60 µs | +2.4% (noise) | 1312 → 1312 B |
| nestedSelect (`.select(a.Street,a.City)`) | 4.06 µs | 4.14 µs | +2.0% (noise) | 1912 → 1912 B |
| `where[x.Count()>5]` | 1.89 µs | 1.93 µs | +2.1% (noise) | 1216 → 1216 B |
| `groupBy[…].where[x.Count()>5]` | 3.43 µs | 3.46 µs | +0.9% | 1720 → 1720 B |
| groupChain (`groupBy…where[x.Count()>2].orderBy[x.Max(x.Age)].select[C=x.Count()]`) | 7.99 µs | 8.11 µs | +1.5% (noise) | 2992 → 2992 B |

## What the table says

- **Nothing moved.** Every delta is +1-3%, the same constant across queries that never see a
  parenthesis (`whereInt`, `select27`) and queries built of the new syntax - it is the jitter floor
  of this protocol on this (loaded) machine, not a cost of the rules. Allocations are byte-identical
  everywhere: the rules changed no candidate list size and no window walk, only when two existing
  candidates refuse a window.
- **The two new rules cost what a refusal costs.** `RightCircleBracket` after `LeftCircleBracket`
  is one more adjacency matcher evaluated at windows opening after `(` - it fired once per `()`
  pair and is not measurable; `NameOrValue` starting at `(`/`)` is a first-character check inside a
  pattern that already scans the window.
- The aggregate queries are tokenized at the speed of the ordinary ones of the same token count
  (`groupByWhereCount` costs what a `where` on a property costs); `where[x.Count()>5]` on alpha is
  the *rejected* spelling of the same text, and it costs the same - the walk was never the price of
  the refusal.

In absolute terms: a query - old syntax or new - pays ~8 ns per extra token, and the aggregates add
no constant. The tokenizer remains a fraction of a percent of a request whose cost is the EF
translation and the database round trip.

## Files

- this file
- the rules: `src/HamsterWheel.HLinq/Pipeline/Tokenizer/Grammar.cs`, `src/HamsterWheel.HLinq/Tokens/NameOrValue.cs`
- benchmark project (the queries): `test/HamsterWheel.HLinq.Benchmarks/TokenizerBenchmarks.cs`

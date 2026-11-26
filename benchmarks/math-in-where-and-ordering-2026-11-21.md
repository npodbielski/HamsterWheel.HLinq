# Parse benchmarks: math in `where` and in the ordering roots

Compared `alpha` before the branch (`6d77357`) with `alpha` after it (`bc0b02f`, !130). The four
operators, the written operand and the group became words of `where` and of the four ordering
roots; measured over the built grammar, the group of words of a clause root grew

| clause root | words before | words after |
|---|---:|---:|
| `where`, `orderBy`, `orderByDescending`, `thenBy`, `thenByDescending` | 29 | 34 |
| `select` | 30 | 30 |
| `take`, `groupBy`, `count`, `skip` | 29 | 29 |

so the question is what a query that never writes an operator pays for the words its clause now
carries, and what a query that does write one pays for the answer.

## Method

`dotnet run -c Release --project test/HamsterWheel.HLinq.Benchmarks -- --filter
'*TokenizerBenchmarks*'`, job `ShortRun`, pinned to one core, run against both worktrees.
`DOTNET_TieredCompilation=0` is set in the environment, and that is not cosmetic: the `ShortRun`
job's three warmup rounds do not reliably promote the tokenizer's hot methods to tier 1, so with
tiering on the same build measures the same query at 1.5× or at 4× its time depending on which
tier the run happened to settle in, and no comparison of two runs means anything. With tiering off
every key of every run agrees within 1 % - the errors below are BenchmarkDotNet's own, all under
1 %. The tiered absolute times are the ones `baseline-2026-09-18.md` and the notes after it
record, so those tables are comparable to each other and not to this one.

The full pipeline (bind + tokenize + parse, no `Apply`) is measured with the same paired protocol
the earlier notes use - the two builds alternating round-robin on one pinned core, six pairs,
first side alternated per pair - because that path is not in the committed project. Its tokenizer
absolutes land within a few percent of BenchmarkDotNet's (`whereInt` 2.63 against 2.72 µs), and
its tokenizer deltas point at the same clauses, a little higher than the +10-14 % of the table
above (+15 to +16 %), since a tight loop spends a larger share of its time tokenizing than a
bind+tokenize+parse does.

## Tokenizer (`TokenizerBenchmarks`, `ShortRun`: N=3)

| key | query | before | after | d | allocated |
|---|---|---:|---:|---:|---:|
| `whereInt` | `where[x.Int==5]` | 2.723 µs | 3.006 µs | **+10.4 %** | 1 080 → 1 080 B |
| `whereName` | `where[x.Name==John]` | 2.706 µs | 3.088 µs | **+14.1 %** | 1 080 → 1 080 B |
| `whereDate` | `where[x.Date>2023-10-12]` | 2.857 µs | 3.178 µs | **+11.2 %** | 1 080 → 1 080 B |
| `whereDoubleDot` | `where[x.Double==1.21]` | 2.764 µs | 3.130 µs | **+13.2 %** | 1 080 → 1 080 B |
| `orderbyLower` | `orderby[x.Name]` | 2.074 µs | 2.313 µs | **+11.5 %** | 1 016 → 1 016 B |
| `orderByDescending` | `orderByDescending[x.Name]` | 2.498 µs | 2.759 µs | **+10.4 %** | 1 016 → 1 016 B |
| `chain` | `where[…].select[…].orderBy[…].take[10..20]` | 14.565 µs | 15.597 µs | **+7.1 %** | 3 089 → 3 089 B |
| `nestedSelect` | `select[…nested…]` | 8.606 µs | 8.849 µs | +2.8 % | 2 152 → 2 152 B |
| `select27` | `select[…]` (27 renames) | 53.409 µs | 53.016 µs | -0.7 % | 8 481 → 8 481 B |
| `takeRange` | `take[10..20]` | 1.668 µs | 1.653 µs | -0.9 % | 984 → 984 B |
| `takeOne` | `take[10]` | 1.264 µs | 1.270 µs | +0.5 % | 864 → 864 B |
| `groupByOnly` | `groupBy[x.Name]` | 2.099 µs | 2.074 µs | -1.2 % | 1 016 → 1 016 B |

The three shapes the branch makes possible; the base refuses them, so there is no "before" - the
row says what the answer costs, not what a change cost. Allocation is of the tokenize call itself,
which on the base ends in the refusal:

| key | query | after | tokenizer allocated, refusal → answer |
|---|---|---:|---:|
| `whereDifference` | `where[x.Int-x.Int>5]` | 4.508 µs | 1 736 → 1 360 B |
| `whereFoldedGroup` | `where[x.Int>(2*3)]` | 4.626 µs | 1 264 → 1 360 B |
| `orderByKey` | `orderBy[x.Int+1]` | 3.100 µs | 1 728 → 1 080 B |

## Full pipeline, bind + tokenize + parse (paired medians)

| query | before | after | d | allocated before → after |
|---|---:|---:|---:|---|
| `where[x.Int==5]` | 11.9 µs | 12.7 µs | **+6.5 %** | 8 368 → 8 256 B |
| `where[x.Name==John]` | 12.0 µs | 12.7 µs | **+5.5 %** | 8 376 → 8 264 B |
| `where[x.Date>2023-10-12]` | 12.1 µs | 12.9 µs | **+6.6 %** | 8 384 → 8 272 B |
| chain | 57.7 µs | 61.1 µs | **+4.8 %** | 43 713 → 42 801 B |
| `select[…]` (27 renames) | 219.4 µs | 221.0 µs | +0.7 % | 363 757 → 361 981 B |
| `groupBy[x.Name]` | 11.3 µs | 11.1 µs | -1.8 % | 8 992 → 8 880 B |
| `take[10]` | 6.1 µs | 6.0 µs | -0.8 % | 5 296 → 5 296 B |
| `where[x.Int-x.Int>5]` | 7.7 µs (refused) | 18.5 µs | the answer | 3 504 → 11 673 B |
| `where[x.Int>(2*3)]` | 12.1 µs (refused) | 21.7 µs | the answer | 7 376 → 14 025 B |
| `orderBy[x.Int+1]` | 8.2 µs (refused) | 10.9 µs | the answer | 5 232 → 7 168 B |

## What the tables say

- **The clauses that gained the five words pay ~10-14 % of their tokenize time, and the clauses
  that gained nothing pay nothing.** `where` and the ordering roots moved +10.4…+14.1 %; `take`,
  `groupBy` and `select` moved -1.2…+2.8 %, which is the spread of a run against itself. That the
  cost lands exactly on the clause roots whose word group grew, and not on the ones that did not,
  is the mechanism: after every committed token the walk asks every word of the current clause
  which of them its adjacency matchers allow next, so five more words are five more refusals at
  every window of every query of that clause. Nothing in the parse is involved - it reads the
  children the tokenizer produced and asks nothing of them per query.
- **It is time only, not memory.** Allocation is the same to the byte on every unchanged answer -
  a word the window does not match allocates nothing. On the full pipeline the where shapes
  actually allocate 112 B *less* than before (what the branch's refactors left behind), and
  `select27` 1 776 B less.
- **The math shapes cost ~1.5 µs over a plain where of the same length**, and a written group
  (`(2*3)`) does not cost more than a difference of members - the whole group folds to a constant
  at build time, not per query. Tokenizing them allocates no more than tokenizing a plain where
  (1 080-1 360 B): the refusal the base answered them with already cost that much, and the answer
  is not heavier at this stage - it is the tree and the expression built from the tokens that add
  the 4-8 KB of the full-pipeline rows.
- **The parse absorbs most of it.** Tokenizer +10-14 % is +5-7 % of bind+tokenize+parse, because
  the parse and the bind did not change.
- **Against a request this is noise.** An `Apply` over `where[x.Int>3]` spent 364 µs and
  allocated ~37 KB before any database round trip (`baseline-2026-09-18.md`); ~1 µs of extra
  tokenize time is a quarter of a percent of what the server spends per request.

## Files

- this file
- the words: `src/HamsterWheel.HLinq/Pipeline/Tokenizer/Grammar.cs` (`MathRoots`, their adjacency
  matchers), and `MathGroup` / `MathConstant` for the parse side
- benchmark project (the math shapes and the controls):
  `test/HamsterWheel.HLinq.Benchmarks/TokenizerBenchmarks.cs`

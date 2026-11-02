# Tokenizer benchmarks: the alias word of a nested select

Compared `alpha` with the nested select: one new word (`InnerAlias` - the `a` of `a.Street` inside
`x.Addresses.select(a.Street)`), free text completing at the dot, offered only while a call stands
open (a bracket count over the committed tokens) and only inside the `select` clause, plus `Comma`
gaining `)` as a predecessor. The word is what keeps a value that holds dots (`select[Version=1.2]`)
a value while a name that roots a path inside a call is not swallowed by it.

Method: the same queries as `TokenizerBenchmarks`, run as a tight loop (60 000 iterations, best of
9 rounds per process), the two builds alternating round-robin on one pinned core, **best of five
processes per side**. BenchmarkDotNet `ShortRun` was tried first and thrown out: its StdDev exceeded
the deltas being measured, and a single run disagreed with the next by more than the effect. A
variant with the alias's bracket check stubbed to `false` ran in the same rotation, to separate the
cost of the word from the cost of the check.

## Result

| query | alpha | branch | time | alloc |
|---|---|---|---|---|
| `where[x.Int==5]` | 1.65 µs | 1.54 µs | noise | 984 → 1000 B |
| chain (`where…select…orderBy…take`) | 8.05 µs | 7.86 µs | noise | 2913 → 2929 B |
| select27 (27 renames, no call) | 17.06 µs | 19.33 µs | **+13%** | 6265 → 6281 B |
| nestedSelect (`select[x.Name,addresses=x.Addresses.select(a.Street,a.City)]`) | 3.45 µs | 4.13 µs | **+20%** | — |

The same two queries on the branch with the bracket check stubbed out:

| query | branch | branch, no bracket check | of the delta, the check |
|---|---|---|---|
| select27 | 19.33 µs | 18.77 µs | ~3-5 points of the +13% |
| nestedSelect | 4.13 µs | 3.46 µs | all of the +20% |

## What the table says

- **A query without a call in the `select` clause pays nothing.** `where`, `take`, `orderBy` walks
  never evaluate the alias at all - the word is scoped `insideClauseOf: Select`, and the clause
  group is what the walk iterates, so the cost is not paid where the word cannot stand.
- **A wide `select` of renames pays ~10-15%**, and most of it is the extra candidate itself (one
  more word in the clause group, asked its adjacency at every `Comma`/`Assignment` window rebuild),
  not the bracket check. The check is O(committed tokens) per evaluation - O(n²) in tokens in the
  worst case, with n the token count of one query: tens.
- **A query that uses the new syntax pays ~20% over what alpha spent tokenizing the same text** -
  text alpha could not tokenize at all. The alias walk is the feature: completing the alias at the
  dot is what separates `a` `.` `Street` from one value blob.
- **Allocations move by one slot** in the two scratch arrays the walk sizes by word count: +16 B
  per query, invisible next to the token list.

In absolute terms the regression shape is ~2 µs on a wide rename-heavy select. The tokenizer is a
fraction of a percent of a request whose cost is the EF translation and the database round trip -
and the nested select itself adds no round trip at all (the integration suite counts the commands).

## Files

- this file
- the word: `src/HamsterWheel.HLinq/Tokens/Selecting/InnerAlias.cs`
- its adjacency and the bracket check: `src/HamsterWheel.HLinq/Pipeline/Tokenizer/Grammar.cs`
- benchmark project (the queries): `test/HamsterWheel.HLinq.Benchmarks/TokenizerBenchmarks.cs`

# Benchmark comparison — grammar refactor vs pre-refactor baseline (2026-09-20)

Question: what did the whole tokenizer/grammar refactor (S2–S4, releases
0.9.0 + 0.10.0) cost or gain against the **pre-refactor tokenizer**?

- **pre**: master @ `3568248` — tokenizer exactly as merged in !20, the code of
  [baseline-2026-09-18.md](baseline-2026-09-18.md) (numbers reproduce it within noise)
- **now**: master + `perf/grammar-adjacency-alloc` (0.10.0 with the
  adjacency-closure fix below)

Paired `ShortRun` runs, same machine, back to back:
`dotnet run -c Release --project test/HamsterWheel.HLinq.Benchmarks --no-build -- --filter '*'`

## Tokenizer only (`TokenizerBenchmarks`)

| Query (key)       | pre Mean → now Mean | pre Alloc → now Alloc |
|-------------------|--------------------:|----------------------:|
| whereInt          | 4.99 → 2.64 µs  (-47%) | 8.62 → 0.99 KB  (-88%) |
| whereDoubleDot    | 8.09 → 2.73 µs  (-66%) | 12.3 → 0.97 KB  (-92%) |
| whereDate         | 9.10 → 2.73 µs  (-70%) | 12.95 → 0.99 KB (-92%) |
| orderbyLower      | 5.31 → 2.06 µs  (-61%) | 14.58 → 0.93 KB (-94%) |
| orderByDescending | 6.36 → 2.38 µs  (-63%) | 18.96 → 0.95 KB (-95%) |
| takeRange         | 4.30 → 1.69 µs  (-61%) | 7.49 → 0.87 KB  (-88%) |
| chain (36 chars)  | 37.68 → 13.75 µs (-64%) | 55.63 → 2.97 KB (-95%) |
| select27          | 87.07 → 34.48 µs (-60%) | 109.15 → 6.90 KB (-94%) |

Allocation is now **flat ~1 KB per query** regardless of length (it used to
grow ~200–400 B per character); time is roughly halved to two-thirds faster.

## Full pipeline (`PipelineBenchmarks`, in-memory `IQueryable`, 100 rows)

| Query            | Tokenize          | Tokenize+Parse      | Apply (noise ±20%) | Alloc(Apply)        |
|------------------|-------------------|---------------------|--------------------|---------------------|
| `where[x.Int>3]` | 4.99 → 2.64 µs    | 13.23 → 9.07 µs     | 330 → 326 µs       | 36.9 → 22.1 KB (-40%) |
| chain (36 chars) | 15.18 → 5.14 µs   | 36.11 → 16.25 µs    | 731 → 722 µs       | 73.8 → 32.0 KB (-57%) |
| select27         | 10.84 → 4.25 µs   | 26.62 → 13.13 µs    | 396 → 391 µs       | 62.5 → 28.1 KB (-55%) |

`Apply` time stays expression-tree-dominated (unchanged, as planned); its
allocation still drops ~half because tokenization no longer allocates.

## The regression this run caught (`perf/grammar-adjacency-alloc`)

Measured *before* the fix, 0.10.0 was tokenizing with **more** allocation than
0.9.0 (chain 28.2 KB vs 15.7 KB): S4's `GrammarRule.PreviousTokensMatch` used
`PreviousTokensMatchers.Any(m => m(previousTokens))` — a closure allocated per
candidate per window. Replaced by a plain foreach. The S3 comparison file was
green at the time; this closes the gap that its "no hot-path change" assumption
left unmeasured for S4.

## Correctness

953 + 886 + 322 tests green with the fix; corpus untouched.

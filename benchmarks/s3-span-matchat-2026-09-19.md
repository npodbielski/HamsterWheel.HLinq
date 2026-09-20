# Benchmark comparison — S3 span `MatchAt` (2026-09-19)

Branch `feat/span-match-at` vs `master` @ `40c5084` (post !23), paired
`ShortRun` runs on the same machine back to back
(`dotnet run -c Release --project test/HamsterWheel.HLinq.Benchmarks --no-build -- --filter '*'`),
so both tables include the same environment; baseline file for reference:
[baseline-2026-09-18.md](baseline-2026-09-18.md).

What changed: `IHLinqTokenPossibility.CanBeAt` int scores (0/50/100) replaced by
`MatchAt` (`No`/`Possible`/`Complete(completeAt)`), grammar adjacency hoisted to
once per window (`MatchesPreviousTokens`), tokenizer commit-on-Complete with jump
behind the match. All per-character LINQ (`OrderBy`, `Any`, `First`, list copies),
the per-call possibility array rebuild and the per-candidate `next`-char rescan are
gone — the loop is two reused buffers plus span slices.

## Tokenizer only (`TokenizerBenchmarks`)

| Query (key)        | master Mean → S3 Mean | master Alloc → S3 Alloc |
|--------------------|----------------------:|------------------------:|
| whereInt           | 5.84 → 3.33 µs  (-43%) | 8.70 → 3.52 KB  (-60%) |
| whereDoubleDot     | 8.48 → 3.46 µs  (-59%) | 12.24 → 3.08 KB (-75%) |
| whereDate          | 9.74 → 3.37 µs  (-65%) | 13.27 → 3.52 KB (-73%) |
| orderbyLower       | 5.94 → 2.60 µs  (-56%) | 15.68 → 2.77 KB (-82%) |
| orderByDescending  | 7.06 → 2.90 µs  (-59%) | 19.56 → 2.45 KB (-87%) |
| takeRange          | 4.80 → 1.99 µs  (-59%) | 8.06 → 2.14 KB  (-73%) |
| chain (36 chars)   | 41.33 → 17.39 µs (-58%) | 57.18 → 15.71 KB (-73%) |
| select27           | 95.53 → 44.42 µs (-53%) | 127.8 → 36.9 KB  (-71%) |

Allocation dropped to roughly **0.7–1.0 KB per token** independent of query length
(master scaled with per-character LINQ), time roughly halved everywhere.

## Full pipeline (`PipelineBenchmarks`, in-memory `IQueryable`, 100 rows)

| Query                | Tokenize          | Tokenize+Parse     | Apply (noise ±20%)  | Alloc(Apply)      |
|----------------------|-------------------|--------------------|---------------------|-------------------|
| `where[x.Int>3]`     | 5.46 → 3.24 µs    | 14.25 → 9.99 µs    | 356 → 331 µs        | 37.2 → 26.7 KB    |
| chain (36 chars)     | 15.84 → 6.46 µs   | 38.10 → 18.98 µs   | 791 → 725 µs        | 78.2 → 38.8 KB    |
| select27             | 11.75 → 5.49 µs   | 28.72 → 15.40 µs   | 410 → 392 µs        | 59.8 → 33.8 KB    |

`Apply` stays dominated by per-call expression-tree building (out of scope per
the plan); tokenize+parse is ~1.5–2× faster and every pipeline stage allocates
~35–50% less.

## Correctness

Unmodified differential corpus (176 queries, `TokenizerSnapshotData.cs`),
947 + 886 + 322 unit/integration tests pass byte-identically.

# Quoted values (2026-09-23)

`NameOrValuePattern` gained a quote state, and that pattern runs once per character of every value
in every query, so the change is measured rather than assumed - see
`root-aware-grammar-2026-09-22.md` for what an unmeasured cleanup cost.

`ShortRun`, three runs per side, medians, the two binaries built and run back to back in one
session (`dotnet run -c Release --project test/HamsterWheel.HLinq.Benchmarks -- --filter
'*TokenizerBenchmarks*'`). The three runs are printed so a suspicious median can be seen:

| case | master | quoted values | delta | runs (master → quoted) | alloc |
|---|---|---|---|---|---|
| chain | 11.31 µs | 11.38 µs | +0.6% | 11.19, 11.31, 11.66 → 11.37, 11.38, 11.47 | 2897 B (same) |
| select27 | 22.28 | 22.13 | -0.7% | 21.73, 22.28, 22.77 → 22.09, 22.13, 22.23 | 6249 B (same) |
| whereInt | 2.29 | 2.30 | +0.6% | 2.24, 2.29, 2.29 → 2.26, 2.30, 2.35 | 968 B (same) |
| whereDate | 2.34 | 2.37 | +1.3% | 2.33, 2.34, 2.42 → 2.32, 2.37, 2.39 | 968 B (same) |
| whereDoubleDot | 2.39 | 2.33 | -2.5% | 2.35, 2.39, 2.41 → 2.32, 2.33, 2.38 | 968 B (same) |
| takeRange | 1.44 | 1.45 | +0.7% | 1.41, 1.44, 1.46 → 1.44, 1.45, 1.48 | 872 B (same) |
| orderByDescending | 2.31 | 2.31 | 0.0% | 2.26, 2.31, 2.38 → 2.27, 2.31, 2.36 | 904 B (same) |
| orderbyLower | 1.81 | 1.78 | -1.7% | 1.80, 1.81, 1.82 → 1.77, 1.78, 1.80 | 904 B (same) |

Nothing moved outside the spread of the runs, and allocation is byte-identical: the quote state is a
bool and one comparison per character, and no benchmark query holds a quote. A value that *is*
quoted pays one more pass of the same loop, which no case here measures - correctness, not speed, is
why it exists.

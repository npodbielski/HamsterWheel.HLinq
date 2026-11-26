# Pipeline benchmarks: the comparison lift (!134)

Compared `alpha` before the branch (`dd04a2f`) with the branch after it. The branch moves the
comparison building - the lift of a nullable and its underlying type, the numeric promotion, the
`Expression.Equal/...` switch and the refusal - from `Condition`'s converter to
`IExpressionBuilder.BuildComparison`, and asks the builder one extra method call on every
comparison the query writes. The question is what a comparison that never meets two types pays
for that hop.

## Method

`dotnet run -c Release --project test/HamsterWheel.HLinq.Benchmarks -- --filter
'*PipelineBenchmarks*'`, job `ShortRun`, pinned to one core, run against both worktrees, with
`DOTNET_TieredCompilation=0` - the same protocol `math-in-where-and-ordering-2026-11-21.md`
records and for the same reason: the `ShortRun` warmup does not reliably promote the pipeline's
hot methods, and with tiering on the same build measures itself at different speeds. The `Apply`
rows are the ones that run the changed code; the two suites were run alternating
alpha - branch - alpha - branch, three rounds.

## Existing queries: the extra hop is noise

| key | query | alpha | branch | d | allocated |
|---|---|---:|---:|---:|---:|
| `Apply` | `where[x.Int>3]` | 338.0 µs | 337.2 µs | -0.2 % | 24.65 → 24.65 KB |
| `Apply` | `orderBy[x.Name].take[0..10]` | 596.6 µs | 597.7 µs | +0.2 % | 32.6 → 32.7 KB |
| `Apply` | `where[x.Name==entity12].take[10..20]` | 691.0 µs | 693.1 µs | +0.3 % | 35.2 → 35.3 KB |

All three deltas sit inside the runs' own error (±1-3 % between rounds of the same build), and
the allocation per operation says the same on both sides - the jitter that row carries (`order`
measures 30.45 KB in one round of each build, never in the other's alone) is the enumerators'
avoidable boxing falling out differently between rounds, not the branch asking more bytes: the
lift asks `left.Type != right.Type` and is off the path when the types already agree. The
`Tokenize` and `TokenizeAndParse` rows of the same runs agree to well under 1 %: the tokenizer
and the parse never see this change, which is what the three-stages split promises.

## The new answer: what a lifted comparison costs

The branch makes these build and run; the base refuses them, so there is no "before" - the row
says what the answer costs, over the same 100-row in-memory source:

| key | query | Tokenize | Tokenize+parse | Apply | allocated (Apply) |
|---|---|---:|---:|---:|---:|
| new | `where[x.Int==x.NullableInt]` | 7.99 µs | 31.3 µs | 407.0 µs | 26.34 KB |

Against `where[x.Int>3]` on the same build (332-338 µs): the comparison of two members meets in
`int?`, so the enumeration pays the conversion of the `Int` member to `int?` and the lifted
nullable comparison per row - 1.21× a comparison to a constant. The same ratio was measured over
the same source and the same pinned core for the same LINQ written by hand - a compiled
`e => e.Int == e.NullableInt` enumerated the way `Apply` enumerates costs 315 µs against the
252 µs of `e => e.Int > 3`, 1.25× - so the lift asks of the enumeration what C# itself asks of
it, and the difference between the two absolutes is HLinq's own pipeline, not the comparison.

# The culture parameter read in the binder (2026-09-27)

The parameters of a request are read by the binder and the tokenizer is handed the query alone. That
placement is what this file measures: what the alternatives cost, and what this shape costs.

`ShortRun`, three runs per side, medians, both binaries built and run back to back in one session
(`dotnet run -c Release --project test/HamsterWheel.HLinq.Benchmarks -- --filter '*'`, so the tokenizer
and the pipeline were measured on the same machine state). Allocations identical except where noted.

## Tokenizer: not touched, measured anyway

| case | master | this | delta |
|---|---|---|---|
| chain | 12.00 | 11.60 | -3.3% |
| select27 | 23.24 | 23.46 | +0.9% |
| whereInt | 2.43 | 2.43 | +0.1% |
| whereDate | 2.50 | 2.45 | -1.8% |
| whereDoubleDot | 2.47 | 2.49 | +0.6% |
| takeRange | 1.55 | 1.53 | -1.4% |
| orderbyLower | 1.89 | 1.84 | -2.7% |
| orderByDescending | 2.44 | 2.42 | -0.9% |
| `orderBy[x.Name].take[0..10]` (pipeline) | 3.86 | 3.90 | +1.0% |

No possibility was added to the grammar, so this is the noise floor of the session: ±3% at `ShortRun`,
allocations identical to the byte. It is quoted because the alternative below is not.

## What the alternatives cost, measured while they were still the design

Both alternatives put the parameter in the walk instead of in the binder, and the walk pays for a
possibility at every window rebuild and then at every character the window grows:

- **`culture` as a token the walk reads past** (one grammar possibility, consumed and never emitted):
  +1% to +6.5% of a tokenized query, and +16 B per query for the two scratch arrays the walk sizes by
  possibility count. Measured over two sessions, which is what the ±3% spread means for a change of
  this size: the same code came out at +2.5% one day and +1% the next.
- **the separator `&` as a token of its own** on top of it: +6% to +14%, because a second candidate
  joins the possibility set of every clause.
- **a token broad enough to read a foreign parameter** (any `name=value` segment, the shape that asked
  the tokenizer what the request carries): +24%, every character of a request growing every candidate.

The first of those three is the one worth keeping in mind: it is not slow, it is *taxed* — a parameter
read once per request charged to every clause of every query, including the queries that carry no
parameter at all.

## Binder: the cost of taking the parameters out

`TokenizeAndParse` runs the binder, so it carries the split of the request and the keyword test per
segment:

| case | master | this | delta |
|---|---|---|---|
| `where[x.Int>3]` | 8.37 | 8.40 | +0.4% |
| `where[x.Name==entity12].take[10..20]` | 15.26 | 15.25 | -0.0% |
| `orderBy[x.Name].take[0..10]` | 12.52 | 12.54 | +0.2% |

About a third of a percent of a parse: one split of a string that is already allocated, one keyword
comparison per segment, no scan of the query. `Apply` did not move outside its noise (-0.0% to +0.9%).
A request that carries parameters pays the same split with more segments; the endpoint pays for its own
`[FromQuery]` binding on top of it either way.

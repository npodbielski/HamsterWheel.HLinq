# Tokenizer benchmarks: clause-aware grammar

Compared `master` with the clause-aware grammar: a rule declares which clause its token may stand in
(`insideClauseOf:` / `outsideClauseOf:`) and the tokenizer groups possibilities per clause root once,
switching group when a root is committed. This retires the next-character rule `Assignment` carried,
and lets a comparison compare a value on either side.

Method: `dotnet run -c Release --project test/HamsterWheel.HLinq.Benchmarks -- --filter '*TokenizerBenchmarks*'`
(`ShortRun`), three runs per side, **median of the three**, both sides built and run back to back on
the same machine. A single run disagrees with the next by up to 8% on the long query, so single runs
are not evidence - the numbers below are the ones that survived three runs on both sides.

## Result

| query | master | branch | time | alloc |
|---|---|---|---|---|
| chain | 13.43 µs | 13.36 µs | -0.6% | 2969 → 2945 B |
| orderByDescending | 2.32 µs | 2.46 µs | +5.9% | 952 → 952 B |
| orderbyLower | 1.95 µs | 2.01 µs | +3.3% | 928 → 928 B |
| select27 | 33.50 µs | 26.64 µs | **-20.5%** | 6897 → **6249 B** |
| takeRange | 1.55 µs | 1.64 µs | +5.6% | 872 → 872 B |
| whereDate | 2.64 µs | 2.74 µs | +3.9% | 992 → 992 B |
| whereDoubleDot | 2.58 µs | 2.66 µs | +3.1% | 968 → 968 B |
| whereInt | 2.50 µs | 2.55 µs | +2.2% | 968 → 968 B |

The query with many properties in one clause is 20% faster and allocates 9% less: the walk iterates
the possibilities of the clause it is in, so in `select[...]` the six comparison tokens are not asked
anything at all. The short queries pay a few tens of nanoseconds (+2-6%) for the group switch - a
where clause excludes only the rename, so there is nothing there to save, and the walk now reads a
local array instead of a field.

## What was measured and thrown away

Both alternatives were measured on the same harness; both are recorded so nobody re-measures them.

- **Clause asked per possibility, per candidate rebuild** (`possibility.MatchesClauseRoot(root)` in
  the filter): **+30-39% on every query**. The clause question is cheap, an interface dispatch per
  possibility per rebuild is not - it made the *cheaper question* the expensive one. Grouping once at
  construction is what turned the same idea into a win.
- **Clause as an adjacency matcher** (`OutsideClauseOf<Select>(tokens)` next to the tail check): parity
  on short queries, **+11% on `select27`**. Adjacency is asked per possibility and answers from the
  token list, which has no pointer to the clause, so the matcher walked the list backwards to the last
  root: O(n) per committed token, O(n²) per query. Memoizing the answer per (list, length) per thread
  fixed the walk but not the design - and a memo storing its cached *reference* on every read was
  slower than having no memo at all (a `[ThreadStatic]` reference store is a write barrier, paid per
  rule of every token: 38.97 µs with the writing memo, 37.37 µs storing only on change, 83 µs with no
  memo). Deleting the clause checks entirely measured 35.28 µs, so ~2 µs of that +11% was the lookup
  alone - more than the answer was worth in that place.
- Pruning the out-of-clause possibilities *earlier* (clause matcher before the tail matcher) was
  another ~20% worse than the other ordering on `select27`: the comparison tokens are one- and
  two-character keywords, so a smaller candidate set saves almost no text matching while the clause
  question is then asked where the tail could never have matched. Ordering by cost matters, and the
  final design does not need the ordering trick because the possibilities are never in the list.

## A `chore:` that cost a third of the runtime

A later cleanup commit reformatting this code path changed

```csharp
foreach (var matcher in PreviousTokensMatchers) { if (matcher(tokens)) { return true; } }
```

into `PreviousTokensMatchers.Any(matcher => matcher(tokens))`. The lambda captures the token list, so
every call allocates a closure - and this method runs for every possibility on every candidate
rebuild. Measured on the same harness, against the state before the cleanup and against the same
`master` rebuilt in the same session:

| query | master | cleanup (LINQ) | loop restored |
|---|---|---|---|
| chain | 14.06 µs | 17.47 µs | 12.18 µs |
| select27 | 35.77 µs | 38.09 µs | 23.56 µs |
| whereInt | 2.64 µs | 3.67 µs | 2.47 µs |
| takeRange | 1.69 µs | 2.30 µs | 1.51 µs |
| takeRange alloc | 872 B | **3.69 KB** | 872 B |
| select27 alloc | 6897 B | **65.08 KB** | 6249 B |

+30-44% time and 4-9× allocation, in a commit labelled `chore:`, with the unit, netstandard and
integration suites green and the inspection job happy. Only this benchmark sees it - which is the
argument for keeping it in the repo rather than running it ad hoc.

## Files

- this file
- benchmark project: `test/HamsterWheel.HLinq.Benchmarks/TokenizerBenchmarks.cs`
- grouping: `src/HamsterWheel.HLinq/Pipeline/Tokenizer/HLinqTokenizer.cs`
- declaration: `src/HamsterWheel.HLinq/Pipeline/Tokenizer/GrammarRule.cs`, used by the rules in
  `Grammar.cs`

# Parse benchmarks: refusing two members that answer under one name

Compared `alpha` before the rule (`c9cec4f`) with `alpha` after it (`026c247`, !118): a `select`, a
nested `select` and a `groupBy` member list now refuse two members answering under one name, which
means the parse reads every member's name and remembers what it read, and quotes both values when it
refuses. The question is what a query that is *accepted* pays for a check it never fails.

Method: BenchmarkDotNet `ShortRun`, the two builds in separate worktrees of one machine, runs
interleaved base-then-head. Allocation is the number this table can stand behind - it is reproducible
to the byte across runs of the same build (a `GC.GetTotalAllocatedBytes` probe of 5000 parses agreed
with the report within ~2 %, the difference being what the harness itself carries). Timing is not: a
null run of head against head, alternating round-robin on a pinned core (the protocol the earlier
notes use), scattered by −9 % to +3 % per query with individual pairs reaching ±100 %, which is
wider than any delta here. This machine was loaded throughout (load 2-7).

## Result

| query (tokenize + parse) | before | after | time | alloc |
|---|---|---|---|---|
| `where[x.Int>3]` (no select at all) | 15.48 µs | 11.27 µs | noise | 7.65 → 7.65 KB |
| `select[x.Name,x.Int,x.Double,x.Flag]` | 25.11 µs | 29.45 µs | noise | 19.17 → 24.13 KB (**+5.0**) |
| `select[A=x.Name,B=x.Int,C=x.Double,D=x.Flag]` | 32.08 µs | 39.40 µs | noise | 25.94 → 28.44 KB (**+2.5**) |
| `select[x.Name,x.Children.select(x.Name,x.Id)]` | 37.65 µs | 30.79 µs | noise | 19.82 → 23.89 KB (**+4.1**) |
| `select[x.Name,x.Children.select(A=a.Name,B=a.Id)]` | 31.22 µs | 32.36 µs | noise | 24.97 → 28.79 KB (**+3.8**) |
| `groupBy[x.Name].select[x.Key,C=x.Count(),x.Int]` | 33.01 µs | 39.62 µs | noise | 28.45 → 32.51 KB (**+4.1**) |

The same head with the three call sites commented out, to separate the rule from the two refactor
commits that sit between the two sides - the GC probe then lands on the base's probe numbers exactly
(`select[...]` 20 096 B both sides, every query matching), so the whole delta above is the rule and
the refactors cost nothing.

The same head with the rule split into parts, same query (`select[x.Name,x.Int,x.Double,x.Flag]`):

| variant | alloc | of the +5.0 KB |
|---|---|---|
| rule off | 20 096 B | — |
| every member's name read, nothing kept | 24 242 B | +4.1 KB |
| names read and kept in a case-insensitive dictionary | 25 170 B | +0.9 KB |
| the dictionary also able to quote both values (`GetValue`) | 25 170 B | **+0 B** |

## What the table says

- **A query with no `select` pays nothing, in bytes or in time** - the rule sits in the three clause
  parsers, and `where` never reaches one.
- **An accepted `select` pays ~2.5-5 KB**, and it is not the refusal's cost: `GetValue` - the walk
  that quotes the text of both values - ran zero times, because these queries are accepted. Nearly
  all of it is reading each member's name one more time than the parse used to; the dictionary that
  remembers the names is under 1 KB per member list.
- **How the name read splits.** Reading the name of a member written as a path through the last
  `PropertyName` token alone, instead of `GetPath(...).LastOrDefault()` with its `string[]` of every
  segment, recovers 1.1 KB of the ~5 KB `select4` pays (25 010 → 23 849 B, probe of 20 000 parses;
  shipped as `IProperty.GetLastName`). What no name read touches is the rest: the LINQ over a member's
  children that `FinalPropertyName` walks, and the dictionary's own keys and entries. A renamed
  member names itself by the token it wrote and never walks a path, which is why the renamed shape
  moves by less than the path shape does.
- **Timing cannot be resolved on this machine.** The tight-loop paired protocol gave a consistent
  sign (+0.3 % where there is no `select`, +2.6 to +6.9 % on the selects), but a null run of head
  against head scattered wider than that, so no percentage here is claimed.
- Against a request, this is noise: an `Apply` over the same shapes allocates 24-35 KB and spends
  390-790 µs before any database round trip.

## Files

- this file
- the rule: `src/HamsterWheel.HLinq/Tree/Selecting/MemberNameCollision.cs`, its three call sites in
  `SelectRoot.cs`, `NestedSelect.cs`, `GroupByRoot.cs`
- benchmark project (the queries): `test/HamsterWheel.HLinq.Benchmarks/SelectParseBenchmarks.cs`

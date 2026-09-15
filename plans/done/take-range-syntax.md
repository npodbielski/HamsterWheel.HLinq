# Plan: `take[10..20]` range paging syntax

Closes #8. ✅ Done - implemented in !17 (tokens & parsing), !18 (apply), !19 (client & docs), released in 0.7.0.

## Goal

Support `take[10..20]` as sugar for `skip[10].take[10]` (take the rows at
indices 10..19). All existing `skip[n]` / `take[n]` usages must keep working,
including chained ones like `where[x.Name=John].orderBy[x.DateOfBirth].skip[10]`.

## Semantics (decisions locked here, open questions at the bottom)

- **C# `Range` semantics**: `take[start..end]` skips `start`, returns
  `end - start` rows. `take[10..20]` ≡ `skip[10].take[10]` (10 rows, indices 10–19).
- **Open ends**: `take[10..]` ≡ `skip[10]` (no take cap beyond `MaxTake`);
  `take[..20]` ≡ `take[20]`. Support both from the start — the parser has to
  recognize the operator anyway and half-support is a wart.
  `take[..]` is invalid (nothing is being expressed).
- **Validation**: `start < 0`, `end < 0` or `start >= end` → throw
  `InvalidTokenCollectionException` with the take-root example tokens (same
  shape as today's `take[]` error). Do **not** silently clamp.
- **MaxTake cap**: the cap applies to the row *count* (`end - start`), reusing
  `options.HttpDefaultMaxTakeRecords`, same as plain take. Skip side is not capped.
- **Execution**: do **not** use the .NET 10 `Queryable.Take(Range)` overload —
  the library multi-targets netstandard2.0 and EF providers will not translate a
  `Range` argument anyway. Apply as
  `Queryable.Skip(queryable, start)` then `Queryable.Take(…, end - start)` via the
  existing `IMethodsCache` reflection path.

## Implementation steps

### S1 — `RangeOperator` token (`..`)

- New `src/HamsterWheel.HLinq/Tokens/Paging/RangeOperator.cs`, mirroring
  `Dot.cs` (keyword `".."`, `PreviousTokenMatchImpl` delegating to the rule).
- `Grammar.cs` rules:
  - `new GrammarRule<RangeOperator>(false, [typeof(NameOrValue)])`
    — `NameOrValue` is the only token that can precede `..` (inside take/skip
      brackets). `Dot` is *not* allowed after `NameOrValue` today, so the
      tokenizer has no ambiguity with `.` member access.
  - extend the `NameOrValue` rule's previous-token matchers to also accept
    `RangeOperator` (needed for the `..20` side).
- Verify tokenizer picks `..` greedily: `take[10..20]` must not tokenize the
  first `.` as an unknown/`Dot`. Add tokenizer tests in
    `test/.../Pipeline/Tokenizer/TokenizerPagingTests.cs`.

### S2 — parse: `RangeConstant` element

- New `SkipOrTakeRange` tree element next to `SkipOrTakeConstant`
  (`Tree/Paging/`), a leaf-like branch wrapping
  `[SkipOrTakeConstant] RangeOperator [SkipOrTakeConstant]` with either side
  optional. `ValidParents`: `TakeRoot` only — skip keeps its single value
  (`skip[10..20]` stays an error; if we ever want it, it means the same thing
  as take, which is confusing).
- `TakeRoot.Parser.BuildBranch` still starts at `[Take, LeftSquareBracket, ..]`;
  `SkipOrTakeRange.Parser` claims what follows. `TakeRoot.GetTakeNumber`
  generalized to read either a plain constant or the range.
- Parser unit tests: `take[10..20]`, `take[10..]`, `take[..20]`, chained
  `skip[5].take[10..20]`, and rejects: `take[..]`, `take[10..20`,
  `take[a..b]` (non-numeric is fine to *parse* — fails at convert time like
  today — but must produce a sane error), `take[10..2..3]`.

### S3 — apply

- `TakeRoot.Applier.ApplyImpl`: when a `SkipOrTakeRange` child is present,
  resolve optional sides (missing start → `0`, missing end → no take call for
  `take[10..]` / plain take semantics), validate order, cap count at `MaxTake`,
  then `Skip` + `Take` via `IMethodsCache` (see Semantics).
- Note: `HLinqQuery` auto-appends an empty `TakeRoot` when no `count`/`take`
  is present (`Request/HLinqQuery.cs:52`) — make sure a range take satisfies
  "explicit take" so no second take is appended.
- Integration tests against the in-memory query source: result rows identical
  to the equivalent `skip[x].take[y]` chain for a matrix of start/end values.

### S4 — client + docs

- `HLinqClientQueryBuilder`: add `Take(Range range)` overload emitting
  `take[start..end]` (netstandard build polyfills `System.Range` already via
  `Abstractions/CompilerAttributes/Range.cs`; if the overload is awkward there,
  ship `Take(int start, int end)` instead — decide in review).
- README paging section: document `take[10..20]`, `take[10..]`, `take[..20]`.
- Add `PLANS.md` index (this file) to match sibling repos' convention.

### S5 — versioning

- Feature addition → minor bump (0.7.0) in the release commit, per repo
  convention (see `4570bea` style bumps).

## Sequencing / MR shape

S1+S2 in one MR (tokens must land together for tests to run), S3 next,
S4–S5 last. Each MR green on CI (existing pipeline, no changes needed).

## Open questions

1. Should `skip[10..20]` be rejected outright (current plan) or accepted as an
   alias for the take range? Rejected is simpler; revisit if users ask.
2. `take[10..]` without a cap — leave uncapped (like bare `skip`, matching
   today's behavior) or clamp to `MaxTake` like a bare `take[]`? Plan follows
   bare `skip` (uncapped); flag for review.
3. Whitespace: `take[10 .. 20]` — the tokenizer already strips whitespace
   between tokens; confirm it falls out naturally, otherwise document
   no-spaces-only in S4.

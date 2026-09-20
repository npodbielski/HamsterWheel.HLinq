# Plan: Grammar rules & span-matching tokenizer

Closes #4. Enables #1 (reverse comparison), #3 (extensibility), #5 (culture),
#7 (date/time constants).

## Goal

Make the query language declaratively extensible: a new syntax should mean a new
token pattern + a new grammar rule, never a change to the tokenizer loop. On the
way, measure the tokenizer and make it faster (or prove it does not matter).

The tokenizer keeps its fundamental shape — one forward pass, no backtracking,
no peeking at syntax (AGENTS.md). What changes is *who holds the knowledge* and
*how often it is asked*.

## Baseline knowledge (what hurts today)

- Adjacency is declared three ways that do not feed each other:
  `GrammarRule.allowedPreviousTokens` (Type list, feeds `GetDelimiters`),
  `previousTokensMatchers` (lambdas, do NOT feed delimiters — using the Type list
  instead silently made `.` a `NameOrValue` delimiter and broke floats in !17),
  and lexical rules living inside `CanBeAt` overrides (`RangeOperator`,
  `NameOrValue`).
- The score protocol `0 / 50 / 50+50·len/len / 100` is false precision: only
  0 (`No`) and 100 (`Complete`) have semantics.
- Keyword prefix check `next != Keyword[subset.Length]` is case-SENSITIVE while
  full matching is `OrdinalIgnoreCase`; lowercase `orderBy` only survives because
  every possibility is rescored against the whole window at every character —
  an invariant that nothing states and the optimization below must keep.
- Work per query: `query_length × possibilities` `CanBeAt` calls, each rescanning
  the growing window.

## Design (target)

Three layers, one algorithm:

```text
1. TokenPattern   per token, context-free: what text does it match?
                  Keyword("orderBy", next: '['), Word, QuotedString, Number(culture),
                  RangeLiteral (`10..`), ... composed from small hand-rolled parts
                  (no regex - netstandard2.0 hot loop).
2. GrammarRule    per token, syntactic: after which token-type sequences may it
                  appear? One declaration (patterns over token types) from which
                  BOTH PreviousTokensMatch AND the delimiter set are derived.
3. Tokenizer      single pass, event-based advance, asks only the pattern API.
```

Match API replacing the score protocol (per token, over the whole remaining span):

```csharp
TokenPattern.MatchAt(ReadOnlySpan<char> rest) ->
    No | Possible(maxPrefix) | Complete(completeAt, maxPrefix)
```

`completeAt` already includes the next-character condition. The tokenizer at a
window start asks all possibilities; a `Complete` commits and the window jumps to
its `completeAt`; otherwise the window end jumps to the smallest `completeAt` /
`maxPrefix` among alive possibilities (an "event"), never rewinding. This turns
`query_length × possibilities` per-char calls into `tokens × possibilities`
span calls and makes the `orderBy` → `D` → `orderByDescending` walk exactly the
deterministic jump discussed, no heuristics.

## Steps (one MR each, master green, merged only on approval)

### S1 — Benchmarks & baseline (no behavior change)

- New `test/HamsterWheel.HLinq.Benchmarks` (BenchmarkDotNet, net10.0, not part
  of `dotnet test`; added to solution for `dotnet build HLinq.slnx`).
- Benches: tokenize-only for representative queries — plain `where[x.Int=5]`,
  value with dots `where[x.Double==1.21]`, `where[x.Date>2023-10-12]`,
  `orderByDescending[x.Name]` (dead-prefix rescoring), `take[10..20]`,
  `select[...]` with ~27 renames, full `where(...).select(...).orderBy(...).take[...]`
  chain; plus tokenize+parse+apply for two of them to show the tokenizer's share
  end-to-end.
- Commit the baseline `benchmarks/baseline-<date>.md` results into the repo.
- Deliverable: numbers + a go/no-go note; every later step must re-run benches
  and commit a comparison file. Acceptance for S3: tokenize-only not slower than
  baseline; the jump design should be clearly faster.

### S2 — Extract `TokenPattern`s (pure refactor)

- Introduce `TokenPattern` types; move the logic of `TokenPossibility.CanBeAt`
  (keyword prefix/complete, delimiters, letters/digits/underscore) and the
  `RangeOperator` / `NameOrValue` overrides into patterns. `CanBeAt` becomes a
  thin adapter over patterns; per-character protocol and scores unchanged.
- Differential harness (test): tokenize a corpus of every query string appearing
  in tests with the S2 tokenizer and assert identical token streams to the
  pre-S2 tokenizer (snapshot or in-test compare). All suites green.

### S3 — Span `MatchAt` + event-based advance (breaking → 0.9.0)

- `IHLinqTokenPossibility` gains `MatchAt`; the int-score `CanBeAt` is removed
  (public API break; nothing else consumed it in the wild yet - pre-1.0).
- Tokenizer loop: window-start queries, commit-on-Complete, event advance;
  whitespace skip and end-of-query error behavior byte-identical (all existing
  tokenizer/parser/applier/integration tests must pass unmodified).
- Keyword matching uniformly `OrdinalIgnoreCase` (kills the prefix-check case
  trap); the "rescore-all, no narrowing" invariant is replaced by "all
  possibilities matched at window start" and documented.
- Re-run benches, commit comparison. If tokenize-only regressed > small
  tolerance vs baseline, stop and reconsider before S4.

### S4 — Unify adjacency + grammar self-check (the actual #4 close)

- One grammar entry per token carrying BOTH halves: its text rule
  (`TokenPattern`) and its adjacency rule. The entry probably belongs to the
  grammar (a token's characters are grammar too), but it MUST stay resolvable
  per DI registration, not hardcoded per token type in `Grammar`: localizations
  replace a token by registering a different possibility class
  (`AddTokenPossibility<pl.CountPossibility>()` turning `count` into `zlicz`,
  see `demo/.../Translations/pl/`), so the grammar must read text rules and
  adjacency from the *effective registered possibilities*, never own copies —
  otherwise swapping a possibility would silently keep English text rules or
  English adjacency behind the localized token.
- One adjacency declaration (patterns over token types, e.g.
  `After(StartOfQuery)`, `After(RightSquareBracket, Dot)`, `AfterAny(NameOrValue)`);
  `allowedPreviousTokens` and `previousTokensMatchers` collapse into it;
  `GetDelimiters` derives from the full declaration so a rule can never silently
  change (or fail to change) another token's delimiters.
- Startup/unit-test self-check: every token has exactly one rule; every
  keyword-prefix collision is disambiguated by a next-char rule or fails; every
  token reachable from `StartOfQuery`; delimiters round-trip against rules. The
  self-check runs against the effective (post-localization-override) grammar, so
  a localized keyword collision (`zlicz` vs any English keyword) fails startup.
- Update the AGENTS.md design-rule paragraph to describe patterns/rules and the
  event advance so future agents extend the grammar, not the loop.

### S5 — Follow-ups unlocked (separate plans, not this one)

- #7 date/time constant pattern; #5 culture-parameterized number pattern;
  #1 reverse comparison via rule + parser branch. Each should be a plain
  pattern + rule + tests change.

## Risks / open questions

- `IHLinqTokenPossibility` is public: S3 is breaking → minor bump to 0.9.0
  (separate `chore: bump version` commit). Any external implementers to care
  about? (Extensibility story is #3, not shipped - probably none.)
- netstandard2.0 span slicing costs: bench target is net10.0; keep the
  netstandard build compiling the same algorithm (existing
  `HLinqTokenizer.Net90/NetStandard` split shows the pattern if needed).
- Event advance must reproduce today's tie behavior (multiple `Complete` at the
  same index pick the first registered possibility - keep, and pin with a test).
- Differential corpus (S2/S3) is the correctness backbone: prefer generating it
  from all test query strings automatically over hand-listing.

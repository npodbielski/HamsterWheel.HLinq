# Agent instructions

Standard rules for AI coding agents (and humans) working in this repository.

## Git workflow

- **Never merge a merge request without explicit approval** from the maintainer.
  Push the branch, open the MR with `glab`, monitor the pipeline until it is
  green — then stop and wait for review.
- **Never push directly to `master`.** All changes go through merge requests
  targeting `master`.

Flow: `feature branch` → MR → `master` (merged only after maintainer approval).

## Comments and documentation

- **No one-line summary comments.** Do not add doc comments that merely restate
  what the code says (a `<summary>` echoing the member name, or a "see also"
  pointer); the name and signature are the documentation. Write a comment only
  for the *why* that the code cannot show, and put cross-cutting design rules
  here in AGENTS.md rather than repeating them in code.
- **Code comments must be readable without repo history.** No issue numbers,
  plan step names ("S4"), or MR references in source comments — say what the
  code does and why, in itself; history lives in git and the issue tracker.

## Code style

- **Braces on every loop and branch body**, even single statements, and one
  statement per line (no `foreach x foreach y yield return z;` chains, no
  `if (x) return;`).
- **Multi-line XML doc blocks.** `/// <summary>` opens on its own line, text on
  following lines, `/// </summary>` closes on its own line — never a one-line
  doc comment.

## Versioning

- Semantic versioning, pre-1.0 (`0.x.y`), where a minor bump plays the role of a
  "major" bump (breaking changes are minor bumps):
  - bugfix → patch: `0.6.0 → 0.6.1`
  - new feature, backward compatible → minor: `0.6.0 → 0.7.0`
- Bump `VersionPrefix` in `Nuget.props` in a separate release commit
  (`chore: bump version to x.y.z`). Merging to `master` publishes to the GitLab
  package registry and nuget.org (`package` job, `master` + `src/**` changes only).

## Pipeline

- `build` — `dotnet build HLinq.slnx` (libraries multi-target net10.0 + netstandard2.0)
- `tests` — unit + integration tests (postgres CI service, `POSTGRES_HOST` env var)
- `package-alpha` / `package` — pack & push from `alpha` / `master` branches

## Definition of done (before pushing)

1. `dotnet build HLinq.slnx`
2. `dotnet test test/HamsterWheel.HLinq.UnitTests` and
   `dotnet test test/HamsterWheel.HLinq.UnitTests.NetStandard`
3. Integration tests locally against a postgres (`POSTGRES_HOST=127.0.0.1
   POSTGRES_DB=Demo POSTGRES_PASSWORD=*** dotnet test
   test/HamsterWheel.HLinq.IntegrationTests`) or rely on CI.
4. Push the branch and monitor the pipeline via the GitLab API until green.
   **Do not merge — wait for approval.**

## Design rules

- **The tokenizer stays a single window walk: no backtracking, no peeking at
  syntax.** For each window (text after the last committed token),
  `HLinqTokenizer` evaluates the grammar half of every possibility once
  (`MatchesPreviousTokens` — adjacency cannot change while the window grows) and
  the lexical half (`IHLinqTokenPossibility.MatchAt(window, next)` returning
  `No` / `Possible` / `Complete(completeAt)`) at every character. The first
  possibility completing at an offset commits its token and the walk jumps
  behind the match; there are no scores, no ordering of possibilities and no
  rewinding — a window that matches nothing dies. Both halves are grammar:
  `TokenPattern` is the token's text rule (which characters a token consists of,
  i.e. `name = ALPHA *( ALPHA | DIGIT | "_" )`), `GrammarRule` is adjacency
  (which tokens may stand next to which). The tokenizer must not special-case
  characters (i.e. `.` vs `..`) or look ahead for syntax. Ambiguity is resolved
  by the tokens' own grammars (their `TokenPattern`s): e.g. `NameOrValue` never
  holds `..` and `RangeOperator` matches `10..` together with its number, so
  `take[10..20]` and `take[10.2]` diverge without the tokenizer knowing either
  exists. If new syntax cannot be expressed this way, prefer a new token whose
  grammar absorbs the ambiguity — simplicity beats complexity.
- **One grammar rule per token, declared as adjacency matchers only.** The
  characters that end a free-text token (its delimiters) are *derived* from
  which tokens the rule allows to follow it — never declared separately, so a
  rule change cannot silently stop making a following token a delimiter (or
  vice versa). `IGrammar.Validate()` self-checks the effective grammar (one
  rule per token, every token reachable, no keyword swallowing a longer one it
  competes with) against the registered possibilities, so localized grammars
  (`AddTokenPossibility`) validate as they run; call it in tests when touching
  grammar.

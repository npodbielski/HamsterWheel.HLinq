# Agent instructions

Standard rules for AI coding agents (and humans) working in this repository.

## Git workflow

- **Never merge a merge request without explicit approval** from the maintainer.
  Push the branch, open the MR with `glab`, monitor the pipeline until it is
  green — then stop and wait for review.
- **Never push directly to `master`.** All changes go through merge requests
  targeting `master`.

Flow: `feature branch` → MR → `master` (merged only after maintainer approval).

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
  syntax.** `HLinqTokenizer` advances one window and scores *all* token
  possibilities against it at every character; a possibility scoring 100 commits
  and resolves the window. It must not special-case characters (i.e. `.` vs `..`)
  or look ahead for syntax. Ambiguity is resolved by the tokens' own grammars:
  each possibility returns 0/possible/100 from `CanBeAt` (validated by the `next`
  character), and possibilities that stay possible simply keep the window alive —
  e.g. `NameOrValue` never holds `..` and `RangeOperator` matches `10..` together
  with its number, so `take[10..20]` and `take[10.2]` diverge without the
  tokenizer knowing either exists. If new syntax cannot be expressed this way,
  prefer a new token whose grammar absorbs the ambiguity — simplicity beats
  complexity.

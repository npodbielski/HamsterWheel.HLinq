# Agent instructions

Standard rules for AI coding agents (and humans) working in this repository.

## Git workflow

- **Never merge a merge request without explicit approval** from the maintainer.
  Badges, tags, releases and the public mirror are the pipeline's job (see
  *Pipeline*); no release step is done by hand.
  Push the branch, open the MR with `glab`, monitor the pipeline until it is
  green — then stop and wait for review.
- **Never push directly to `master` or `alpha`.** All changes go through merge
  requests targeting `alpha`.

Flow: `feature branch` → MR → `alpha` → `master`. Both merges happen only after
maintainer approval.

- **`alpha` is the integration branch.** A merge into it packs the libraries with
  an `-alpha-<job>` suffix (`package-alpha`) and pushes them to this project's
  package registry, which is how a change is tested from another project — e.g.
  Platform consumes `0.x.y-alpha-*` before the release exists. Nothing is
  published to nuget.org, no tag is made.
- **`master` is the release branch.** It moves only by merging `alpha` into it,
  and that is what runs `release` and `package`: the tag, the GitLab release, the
  GitHub mirror, and the packages for the registry and nuget.org.

So a feature MR is against `alpha`, and releasing what `alpha` holds is a second,
explicit `alpha` → `master` MR.

## Comments and documentation

- **Comments state what the code does and its contract** — never history ("we switched to X
  because Y was not working"), justification or speculation ("otherwise the caller would read it
  as 500"), or plan references ("the plan mentioned A but it is not possible so we use B").
  Keep comments as short as they can be; future work goes in `TODO(scope): ...` markers, not
  prose. History lives in git and the issue tracker.
- **No one-line summary comments.** Do not add doc comments that merely restate
  what the code says (a `<summary>` echoing the member name, or a "see also"
  pointer); the name and signature are the documentation. Write a comment only
  for the *why* that the code cannot show, and put cross-cutting design rules
  here in AGENTS.md rather than repeating them in code.
- **A documented member carries its `<summary>` and nothing else.** No
  `<remarks>`, no `//` comment above or below the doc block — what does not fit
  twenty words is either not summary material (a `//` comment where the code
  needs it, away from the block) or design material (AGENTS.md).

## Code style

- **No `out` and `ref` parameters.** A method that may or may not produce a value returns it
  nullable (or a tuple when it produces more than one); an `out` parameter hides which value is
  the answer and which is a by-product.
- **Braces on every loop and branch body**, even single statements, and one
  statement per line (no `foreach x foreach y yield return z;` chains, no
  `if (x) return;`).
- **XML doc summaries are twenty words or less.** A summary is read in the tooltip of one
  member, not as a paragraph; what does not fit is design and belongs in AGENTS.md or in a
  comment where the code needs it. `<summary>` and `</summary>` each always get a line of
  their own — never text flowing after the opening tag or before the closing one:

  ```csharp
  // allowed
  /// <summary>
  /// My class.
  /// </summary>

  // disallowed - one line
  /// <summary>My class.</summary>
  ```

## Test conventions

- **A test reads as arrange, act, assert**, marked by the bare comments `//arrange`, `//act`
  and `//assert` — the comment is the marker and nothing else, never a sentence after it.
  Where the lines of one section answer for another, one combined marker says so
  (`//act + assert`); a section that is empty is skipped, not explained.
- **Helper methods of a test class live below its tests** — the tests are read top-down and
  the helpers are what they lean on; a helper between two tests breaks the list.
- **Test files follow the comment rules of code**: a summary of a test class or helper is
  twenty words or less and carries nothing around it.

## Versioning

- Semantic versioning, pre-1.0 (`0.x.y`), where a minor bump plays the role of a
  "major" bump (breaking changes are minor bumps):
  - bugfix → patch: `0.6.0 → 0.6.1`
  - new feature, backward compatible → minor: `0.6.0 → 0.7.0`
- Bump `VersionPrefix` in `Nuget.props` in a separate release commit
  (`chore: bump version to x.y.z`) — the release commit, not a feature branch:
  the branch that adds a feature does not decide the version it ships in.
  Merging to `master` publishes to the GitLab
  package registry and nuget.org (`package` job, `master` + `src/**` changes only).

## Pipeline

- `build` — `dotnet build HLinq.slnx` (libraries multi-target net10.0 + netstandard2.0)
- `tests` — unit + integration tests (postgres CI service, `POSTGRES_HOST` env var)
- `package-alpha` / `package` — pack & push from `alpha` / `master` branches
- `badges` + `badges-failed` — publish the README's badge SVGs from the suites'
  own report files. Two instances of one template job: the badge states the
  pipeline outcome, a pipeline is `running` while its own jobs run, and a rules
  list applies only its **first** match. `allow_failure` — a report must not
  block a release, nor make its sibling instance look like a broken build.
- `release` — tag, GitLab release and one synthetic commit on the public GitHub
  mirror, for the version in `Nuget.props`. It runs on every green master
  pipeline and publishes only the build its tag names, so commits that land
  after a release wait for the next bump; `GIT_DEPTH: "0"` because the tag
  anchor walks the merged history. `package` `needs:` this job: a version must
  not reach nuget.org untagged.
- `readme-check` — fails an MR whose README references a badge the pipeline does
  not write, or whose own-package `Version=` snippets disagree with `Nuget.props`.
- Each test job writes its own `TEST_RESULTS_DIR`. Jobs that share a directory
  write the same `rspec.xml` / `coverage.cobertura.*.xml`, and a later job that
  downloads every test job's artifacts into one workspace keeps only the last
  extraction.

## Definition of done (before pushing)

1. `dotnet build HLinq.slnx`
2. `dotnet test test/HamsterWheel.HLinq.UnitTests` and
   `dotnet test test/HamsterWheel.HLinq.UnitTests.NetStandard`
3. Integration tests locally against a postgres (`POSTGRES_HOST=127.0.0.1
   POSTGRES_DB=Demo POSTGRES_PASSWORD=*** dotnet test
   test/HamsterWheel.HLinq.IntegrationTests`) or rely on CI.
4. Push the branch and monitor the pipeline via the GitLab API until green.
   **Do not merge — wait for approval.**

## The three stages

The query is built in three stages and each reads only what the stage before it
produced:

1. **the tokenizer** turns the text into tokens (`HLinqTokenizer`), reading
   nothing but the current window;
2. **the parser** turns tokens into a tree of elements (`HLinqParser`, the tree
   types under `Tree/`); this is where the query's own facts get committed — a
   clause root is parsed, a grouping is applied, the order of the roots is
   fixed — and therefore where the questions those facts decide get answered:
   which member of a select shapes a group's rows, whether a call stands beside
   a group or shapes a row, and what a clause may name after a `groupBy`;
3. **the applier** turns the tree into `IQueryable` calls, with the expressions
   the builders produce; it builds out of decisions the tree already says.

So a stage never reaches forward: the parser does not consult the queryable or
the provider, and the applier does not re-read the query text or
re-derive from tokens what the built tree already answers. When an applier finds
itself asking what a word of the query *meant*, that question belongs to the
parse. A clause that needs to know whether a `groupBy` came before it asks the
parsing context (`IParsingContext.GroupingApplied`) while it is parsed, never the
queryable the applier happens to hold. Three questions are deliberately not
the parse's, because they are answered against a type and not against the text:
what a name resolves to in the host's CLR type (member resolution), whether the
subject a call hangs on has anything to fold (a row that is itself a collection
folds like any LINQ source, with no `groupBy` in the query), and what a provider
can translate (the packs, measured per engine).

## Design rules

- **A clause reads its values by what answers them, never by a position among its
  children.** A call written over a member is two children of the clause - the member
  and the call - so `where[x.Children.Count()>1]` holds four children around the
  operator and `where[x.Int>1]` holds three. `ConditionExpression` is what a clause reads as
  one value (the call, with the member it hangs on, or the single element written in
  place of a call), and `Condition.Sides` splits the children at the operator and hands
  each side to it; the ordering roots read their one value the same way. A converter
  that indexes `Children` by hand therefore breaks the moment the query writes a call,
  and a comparison builds the side that says what is compared first, because a value the
  query wrote becomes whatever the side it stands against is.
- **The tokenizer stays a single window walk: no backtracking, no peeking at
  syntax.** For each window (text after the last committed token),
  `HLinqTokenizer` evaluates the grammar half of every candidate word once
  (`IGrammarRule.PreviousTokensMatch(tokens)` — adjacency cannot change while
  the window grows) and the lexical half (`TokenPattern.Match(window, next)`
  returning `No` / `Possible` / `Complete(completeAt)`) at every character. The
  first word completing at an offset commits its token and the walk jumps
  behind the match; there are no scores, no ordering of words and no
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
- **Clause scope is a property of the words of a language, not of adjacency.** The
  walk knows which clause it is in because committing a clause root
  (`IClauseRootToken`) says so — the words of a language are grouped per clause
  root once, when the language is built, and committing a root switches the
  group. A token
  of another clause is therefore never asked anything. This is not lookahead:
  the clause is a committed fact, the same kind of fact adjacency reads. The
  question a rule answers about the clause (`MatchesRoot`) is separate
  from its matchers — see the next rule.
- **One grammar rule per token, declared as adjacency matchers only.** The
  characters that end a free-text token (its delimiters) are *derived* from
  which tokens the rule allows to follow it — never declared separately, so a
  rule change cannot silently stop making a following token a delimiter (or
  vice versa). Clause scope is the one thing declared beside the matchers, never
  as one: a matcher sees the tokens before it and can answer no question about a
  clause, and deriving delimiters must stay clause-blind (`=` is a delimiter of
  `NameOrValue` in every clause, or floats and GUIDs in values break). Two
  tokens sharing a prefix (the `=` of `Assignment` and the `==` of `Equality`) are
  kept apart by their clauses, not by a next-character rule. `GrammarValidator`
  self-checks a grammar (one rule per token, every token reachable,
  no keyword swallowing a longer one it competes with *in a clause where both are
  allowed*) against the words of one language, so a language with words of its
  own (`AddTranslatedKeyword`, `AddWord`) is checked as it is built;
  call it in tests when touching grammar. What a grammar answers — which element
  opens a query, whose child each element is, what word spells a token, which
  tokens a token stands after and before, which clause root it is a word of — is
  recorded per class for the invariant language and compared across every language
  of one tokenizer (`GrammarFactsData`, `GrammarFactsUnitTests`), so a widened
  matcher shows up as the line of the class and the question it changed in.
- **One grammar per language, one word object per token.** A language is the
  words of one culture plus the invariant ones, and two cultures never share a
  grammar: the characters that end a free-text token are *derived* from the words
  that may follow it, so handing the invariant language a word of another culture
  makes that word's first letter compete with the letters of a name. A word
  is one object — which token it spells, with which keyword, of which culture
  (`IHLinqWord`) — and answers no grammar question, so one object is
  walked in every language it belongs to and nothing is instantiated per culture.
  `HLinqTokenizer` builds a grammar per language from the words it is
  handed, and each language carries the validator of its grammar;
  `ValidateLanguages` checks them all and the API calls it as it starts
  (`IStartupFilter` in the AspNet package): a table of words that does
  not make a valid grammar stops the API instead of answering 500 to the first
  query written in it. Deliberately not from `ConfigureHLinq` — there is no
  container there, and only the words say which cultures there are.

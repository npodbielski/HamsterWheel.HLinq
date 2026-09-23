# Plan: Root-aware grammar (`=` renames in select, compares in where)

Closes #1 (reverse comparison, `where[10<x.Int]`). Retires the `Assignment`
next-character workaround. Ambiguous member names (#9) are designed in
`plans/member-resolution.md`.

## Goal

Make the grammar aware of the clause (root) a token stands in, so one character can
belong to different tokens in different clauses: `=` is the rename of
`select[FullName=x.Name]` and the comparison of `where[x.Int=10]`. The tokenizer must
not change shape — still one forward window walk, no backtracking, no peeking at
syntax (AGENTS.md). A clause root is just another token in the token list the
adjacency rules already receive; nothing looks ahead at surrounding syntax.

## Baseline knowledge (what hurts today)

- `=` is one character with two roles. In `select[FullName=x.Name]` it is the
  `Assignment` token; in `where[x.Name==test]` it is the first character of the
  `Equality` keyword `==`. Both are allowed directly after a `NameOrValue`, so
  `Assignment` carries a next-character hack — its rule has
  `nextCharMatcher: c => c != EqualityTokenKeyword[0]` and its possibility overrides
  `NextIsAllowedWhenKeywordMatch` — purely so that `==` is not swallowed as an
  `Assignment` plus a dangling `=`.
- Consequences of that hack: `where[Name==5]` works only because of it; a single `=`
  in a where clause (`where[x.Int=5]`) dies in the tokenizer; `where[2=x.Int]` tokenizes
  as `NameOrValue Assignment Entity ...` and is rejected by the parser on purpose, so
  reverse comparison (#1) has no way in.
- No rule knows about clauses: every adjacency matcher looks only at the tail of
  `previousTokens` (`After(typeof(NameOrValue))`), identical in select, where, order and
  paging clauses.
- The grammar self-check cannot state that `=` and `==` never actually compete, because
  adjacency does not say in which clause each of them is legal — it only sees the hack.

## Design

### 1. Clause root is a token property

New marker interface `IClauseRootToken : IToken`, implemented by the nine root tokens
(`Select`, `Where`, `OrderBy`, `OrderByDescending`, `ThenBy`, `ThenByDescending`,
`Skip`, `Take`, `Count`). Localizations replace *possibilities*, never token types
(`AddTokenPossibility<SelectPossibility>()` registers keyword `wybierz` for the
`Select` token type), so a localized root stays a clause root for free.

### 2. The walk keeps the clause; the rule says whether a token may stand in it

A clause is not adjacency: it is not a fact about the token before, and a matcher - which only
ever sees `previousTokens` - must not become the place where it is decided. So the rule gains a
second, separate question and the tokenizer keeps the answer to the first:

```csharp
new GrammarRule<Assignment>(false, [After(NameOrValue)], insideClauseOf: typeof(Select));
new GrammarRule<Equality>(false,   [AfterValue],          outsideClauseOf: typeof(Select));

// HLinqTokenizer: possibilities grouped per clause root once, at construction
clausePossibilities = _possibilitiesByClauseRoot[committedRoot.GetType()];
```

Committing a clause root switches the group the walk iterates, so a possibility belonging to
another clause is never asked anything - not adjacency, not text. The clause is a committed fact
(`where[a==1].select[b=a]` has committed `Select` before the `=` of the rename and only `Where`
before a comparison operator), which is why this is not syntax peeking.

Delimiter derivation stays clause-blind and untouched: `GetDelimiters` tests the *matchers*
against synthetic marker lists, `[Marker(NameOrValue)]` carries no clause, so `=` remains a
`NameOrValue` / `PropertyName` delimiter in every clause (inside select that is what makes
`select[FullName=x.Name]` end the free text token; outside, it keeps floats and GUIDs intact).

### 3a. A keyword that is also a word

Allowing `Entity` after a comparison would let the single-letter entity keyword swallow values that
start with it (`where[x.Name==xander]`), so `Entity` gets `WordKeywordPattern`: like a free text
token it completes only when the character after it is one the grammar allows to follow it - its
derived delimiters, now also available to keyword tokens (`TokenPossibility.Delimiters` lost the
"keywords have no delimiters" shortcut, the set is derived the same way for both).

### 3b. What `=` does not become

A single `=` does not compare, and that is a language decision rather than a technical one: the
comparison operators of a query are the C# ones (`==`, `!=`, `<`, `>`, `<=`, `>=`), and `=` renames
in `select[...]` — in every other clause it is no token at all.

An earlier revision made `=` compare outside select (`Equality` matching `=` optionally followed by
`=`). It bought nothing that the clause scoping of §2 does not already buy: with `Assignment` legal
only inside a select clause, `where[x.Int=10]` dies in the tokenizer on its own, and `where[Name==5]`
needs no next-character rule either. It is gone.

What remains of the workaround is its deletion: `Assignment` loses the `nextCharMatcher` on its rule
and the `NextIsAllowedWhenKeywordMatch` override, because the clause — not the next character — is
what keeps `=` and `==` from claiming each other, and `IGrammar.Validate()` checks competition per
clause now, so it says so if that ever stops being true.

### 4. Reverse comparison, and two paths

`Condition.Parser` gains a branch for a constant on the left
(`[NameOrValue, IComparisonToken, Entity ...]`), which replaces the explicit
reverse-comparison throw. `Condition.Converter` builds the *property* side first — that
is where the comparison type comes from — and converts the constant into the left
operand. Operands are never mirrored: `where[10<x.Int]` builds `LessThan(const, prop)`,
so the expression says what the query says.

With `Entity` legal after a comparison, `where[x.Name==x.Int]` stops comparing a property against
the five characters `x.Int` (what it did, silently matching nothing) and compares two paths. Two
members that have no comparison in common cannot build an expression; that becomes
`ComparisonOperandTypesCannotBeComparedException`, an `HLinqQueryException` — a 400 naming both
types, where it used to be an unmapped `InvalidOperationException` from `Expression.Equal`.

### Behavior changes (each one is one line of the tokenizer corpus)

| query | before | after |
|---|---|---|
| `where[x.Name==test]`, `where[Name==5]`, `select[FullName=x.Name]` | works | works, unchanged |
| `where[x.Int=10]` | tokenizer: unknown token | `Equality` — compares |
| `where[2=x.Int]`, `where[10<x.Int]` | parser: reverse comparison is invalid | filters (closes #1) |
| `where[1=1]` | `Assignment`, parser rejects | `Equality`, condition builder rejects it (two values, no property) with a message that says so |
| `select[x.Name==test]` | tokenized, parser rejects | tokenizer: unknown token (no comparison inside select) |
| `where[x.Name==x.Int]` | compared the text `x.Int` silently | two property paths compared; incompatible types report `ComparisonOperandTypesCannotBeComparedException` instead of an unmapped one |
| `where[x.Int=>5]`, `where[x]`, `orderBy[x\r\n.Name]` | tokenizer: unknown token | read as values, rejected while parsing (as `where[x.Int> =5]` already was) |

## Steps

One MR. The two steps share grammar rules and corpus lines, so they land as one implementation
commit and are reviewed as S1 first, S2 second.

### S1 — Clause roots in the grammar, workaround retired

- `IClauseRootToken` + implementations on the nine root tokens.
- `GrammarRule`: `insideClauseOf:` / `outsideClauseOf:`; `Assignment` scoped to the select
  clause, the comparison tokens scoped outside it.
- `HLinqTokenizer`: possibilities grouped per clause root at construction, the group switched when
  a root is committed; `IGrammarRule` / `IHLinqTokenPossibility` gain `MatchesClauseRoot`.
- Drop the `Assignment` next-character rule (its rule and its possibility override).
- Corpus: edit the affected `TokenizerSnapshotData` lines, add lines for `=` in where,
  `=` vs `==` in select, comparisons after a closing `]` of a select clause
  (`select[a=b].where[x.Name==c]`).
- Tests: grammar self-check (default + localized grammar) still green; delimiter sets
  unchanged; a test naming the clause scope explicitly (`=` is `Assignment` in select,
  `Equality` in where, unknown inside select); benches re-run and compared — root
  matchers scan at most one clause per committed token, never per character.

### S2 — Reverse comparison (closes #1)

- `Entity` becomes legal directly after a comparison token, which needs its word rule (3a) and the
  corpus lines for the values it lets through (`where[x.Name==xander]`, `where[x.Name==x.Int]`).
- `Condition.Parser` branch for a leading constant; drop the throw.
- `Condition.Converter`: property side first, constant converted to its type; keep a
  clear failure for const-vs-const (`where[1=1]`).
- Unit + applier tests for `where[2=x.Int]`, `where[10<x.Int]`, `where['a'==x.Name]`,
  `where[x.a=b]`-style reversed method/property chains, and `&&`/parentheses mixed with
  a reversed condition; README: document `=` as a comparison and reverse comparison.
- Update the exception message that tells users reverse comparison is unsupported.

### S3 — Release commit

`chore: bump version to 0.11.0` in `Nuget.props` (new, backward compatible for
previously *accepted* queries; a previously rejected or rejected-later query now behaves
differently, which is the feature).

### S4 — Not here: ambiguous property and method names (#9)

An out of `Plans`, designed in `plans/member-resolution.md`: a name that matches two members will
pick the better candidate and report the choice, not throw — an error would force new syntax on users
whose queries work today.

## Risks / open questions

- Comparisons become illegal inside a select clause. Nothing applied them: the applier
  only binds a property path or a constant, and the parser rejected everything else —
  but the failure moves from parse time to tokenize time. Confirm no test or demo query
  relies on the later failure.
- Delimiter derivation must stay clause-blind: `GetDelimiters` asks the *adjacency* matchers
  against synthetic marker contexts, which carry no clause. If it ever consulted the clause
  question, the derived delimiter sets would silently shrink and floats, GUIDs and dates inside
  values would break (the historical delimiter trap). `Validate()` now also has to consider
  clauses when it decides that two keywords compete: `=` and `==` share characters and are never
  legal in one clause, so they do not need a next-character rule from each other.
- `where[1=1]` and `where[x>5]` (entity compared directly) keep working / failing as
  today; this plan changes only who rejects a const-vs-const condition, not that.
- Does `=` as a comparison invite `!=`, `<`, `>`-style single-character forms users
  expect to be interchangeable with the two-character ones? They already are — only
  `=`/`==` differ today.
- The clause must not be looked up through the token list. A matcher that walks `previousTokens`
  backwards to the last root is O(n) per committed token and O(n²) per query, and it was measured at
  +11% on a wide select; caching the answer per token list made it a wash, not a win. Grouping
  possibilities per clause at tokenizer construction is both the cheaper and the simpler place — see
  `benchmarks/root-aware-grammar-2026-09-22.md` for the numbers, including the version that asked the
  clause per possibility (+30-39%) and why a `[ThreadStatic]` cache lost to no cache at all.
- A root token whose possibilities the tokenizer was not given falls back to asking all
  possibilities rather than none: an exotic custom root degrades, it does not break a query.
## Definition of done (per AGENTS.md)

`dotnet build HLinq.slnx` (net10.0 + netstandard2.0 + demo) and unit, netstandard-unit
and integration suites all green locally; corpus diff reviewed line by line;
`IGrammar.Validate()` green for the default grammar *and* the demo localizations;
branch pushed, MR opened with `glab`, pipeline watched to green, then stop and wait for
review — never merge, never push `master`.

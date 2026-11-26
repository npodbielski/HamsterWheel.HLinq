# Math in `where` and `orderBy` — `where[x.Total-x.Paid>5]`, `where[x.Date<=(2022-12-31)]`

The select plan shipped the operators, the operands, the group and the fold; this plan moves them
out of the select clause into the two roots where a computed value answers most: a comparison of a
`where` and the key of an `orderBy`. The rule that keeps the language unambiguous is one sentence:
**math that begins with a member is written bare anywhere the operators are words of the clause,
and math that begins with a written value is written inside a group that stands where a value
stands — anywhere a free text is expected, the text means what it means today.**

## What the select plan already decided (and this plan reuses)

- `MathTerm`, `MathGroup`, `MathConstant` (`Tree/Selecting/`) say what the query wrote;
  `IExpressionBuilder.FoldMath` answers what it computes — C#'s precedence, promotion, refusals.
  None of them knows about select; only their `ValidParents` say so.
- `MathOperand` ends at its derived delimiters, so its self-end is local to its pattern and the
  tokenizer never special-cases a character.
- The four operators are one family (`IMathOperatorToken`), one rule each, clause-scoped.
- A written four-digit year reaches a date through the chain of converters a host can override.

## Why the contest is only at the window where a value starts

A free text owns the window that opens at its first character: after a comparison operator, after
`[`, after `,`, after `=`. The operators are words that start after a member, a call's `)` or an
operand — never after a free value — so the moment an operator's previous-token matcher ends in
`PropertyName`, `RightCircleBracket` or `MathOperand`, the contest is decided without the
tokenizer knowing anything about dates:

| query | today | with this plan |
| --- | --- | --- |
| `where[x.Name==2023-10-12]`, `where[x.Guid==4c23c1e0-3358-4380-8a9b-92d7991afa19]`, `where[x.Name==a+b]` | one free value | untouched — after `==` the value grammar owns the window and no operator ever starts after a value |
| `where[x.Total-x.Paid>5]` | refused (the window `x.Total-...` has no token for `-x.Total...` after a name; such a query dies today) | math after a member, bare — nothing contested: no free text ever ran after a member |
| `orderBy[x.Total-x.Paid]`, `thenBy[x.Count()*2]` | refused | math as the key, bare — ordering reads its one value, and no free text stands after a member there either |
| `where[x.Int>(2*3)]` | refused (`(` after a comparison is nothing today) | a `(` that stands on a value side — its previous token is `IComparisonToken` or `IMathOperatorToken` — is a math group |
| `where[x.Date<=(2022-12-31)]` | refused | math: two subtractions — the same decision select made for `(2022-12-31)` |
| `where[(2-1)>x.Int]` | refused | stays refused: the `(` after `[`, `And`, `Or` opens a condition group, and a written value is not a token that starts after it — the user moves the computation to the value side: `where[x.Int<(2-1)]` |
| `where[(x.Total-x.Paid)>5]` | the condition group reads its inside as a condition | math works inside it, because inside is exactly "after a member" — the group is a condition group and its members may carry math, as anywhere |

The user-facing sentence, which is the decision of this plan: **a computation that starts with a
written value needs the group and the group needs to stand on a value side — `x.Date<=(2022-12-31)`
is arithmetic and `x.Date<=2022-12-31` is a date; `x.Total-x.Paid>5` needs nothing.**

## The syntax

- **`where`**: the operators are words of the `where` clause with the select's own previous-token
  rule, so a math run may hang on members and calls on either side of a comparison —
  `where[x.Total-x.Paid>5]`, `where[x.Name.ToUpper()+x.Name==q]`. A math run that opens with a
  written value, or one that the precedence wants computed before the value it compares to, is a
  group on a value side: `where[x.Int>(2*3)]`, `where[x.Date<=(2022-12-31)]`.
- **`orderBy` / `orderByDescending` / `thenBy` / `thenByDescending`**: bare math as the key —
  `orderBy[x.Total-x.Paid]`, `orderBy[(x.A+x.B)*2]`. Ordering names nothing, so no rename rule.
- **`groupBy` keys stay members** — a computed key changes what `x.Key` names and is a naming
  decision, not this plan's gap.
- The fold is the select's: same operators, same precedence, same refusals; written numbers are
  read in the culture of the query, as everywhere.
- **A folded constant compared against a date is read like a written value is**: `where[2001>x.Date]`
  reads `2001` as the year and compares `2001-01-01`; `where[(2*1000+1)>x.Date]` folds to the
  constant `2001` and the same year rule reads that constant into the date, through the same
  chain of converters. A computation that is not constant — `where[(x.A+x.B)>x.Date]` — has no
  value to read and is refused as a type mismatch, the refusal C# gives `someInt > someDate`
  written by hand.

## Design

### Tokenizer (adjacency only — no backtracking, no lookahead, clause is a committed fact)

- `GrammarRule` learns its clause scope as a set of clause roots instead of the single
  `insideClauseOf` it holds; a rule matches when the clause the walk committed is one of the roots
  the rule names. One word stays one object walked in every language and every clause it belongs
  to (the AGENTS rule), and each language's `GrammarValidator` revalidates with the set scopes.
- `Plus/Minus/Multiply/Divide`: previous-tokens matcher as today
  (`PropertyName`, `RightCircleBracket`, `MathOperand`), clause scope: select **and** `where` and
  the four ordering roots. No `InsideCircleBrackets` test: after a member no free text ever runs,
  so a bare operator there contests nothing in any clause — that is why the forcing is only needed
  where a value may start.
- `LeftCircleBracket` gains a matcher: `(` is a math group when its previous token is
  `IComparisonToken` or `IMathOperatorToken` (any clause). After `[`, `And`, `Or` it stays what it
  is today — a condition group.
- `MathOperand`: gains "after `LeftCircleBracket` whose previous token is a comparison or a math
  operator" and "after a math operator" outside select (where, ordering); it keeps select's
  after-`=`-group matcher.
- adjacency edges the closers learn: comparison tokens follow `MathOperand` (so `>` ends an operand
  in a `where`), `And`/`Or`/`)`/`]`/`,` close a math run where their rules need it — all
  adjacency, each one a clause-scoped matcher; delimiters stay derived and clause-blind, and the
  self-check test of `GetDelimiters(MathOperand)` grows accordingly.
- snapshots and refusals assert the boundary: the value rows of the table stay one value, and
  `(2-1)>x.Int` keeps dying in the window walk (no token of `where` completes at a `-` after a
  free value).

### Parser

- `MathTerm`/`MathGroup`/`MathConstant` gain `ValidParents`: a side of `Condition`, the ordering
  root elements; their parsing rules otherwise unchanged.
- `Condition.Sides()` splits at the operator as today; `ConditionExpression` grows from "one
  value" to "one value or one math run" the way `PropertyAssignment` already tells its
  `IsMathExpression` — a side holding `MathTerm`/`MathGroup` folds through the builder and answers
  the fold's type, which is what `ExplicitType` says and what sets `ComparisonType` for the other
  side's free value.
- the ordering roots read their one value the same shape; the same fold-if-a-math-element-stands-
  here branch covers them.

### Builder

- no new question: the side converter and the ordering converter call `FoldMath` where they call
  `ToExpression` today; a folded constant that meets the comparison's type goes through
  `DefaultConverter` like a written value does — the year rule and every host override apply to
  a computed constant exactly as to a text.

### Providers

- comparison and ordering over computed values are plain LINQ (`Where(x => x.Total - x.Paid > 5)`,
  `OrderBy(x => x.Total - x.Paid)`) — arithmetic of that shape is what the select plan already
  recorded as translated in every pack; the per-engine tests assert the statements the translators
  are asked, the way the select plan's do.

## Decisions

- **A value-initial computation needs a group, and the group is recognized by its previous token
  alone** — no lookahead to a `)`, no smarter value grammar. The user pays one pair of parentheses
  where date-shaped arithmetic would otherwise be text; the language keeps every free value it
  has ever read. `(2-1)>x.Int` remains a refusal, with the reason it dies: nothing of the clause
  is a token at that `-`.
- **Math hanging on members is bare in `where` and in `orderBy`**: after a member no free text
  runs, so nothing is contested; the operators' select rule, clause-set wider, says all of it.
- **A folded constant meets a date through the converter** — the year rule and its host overrides
  stand for computations as for written values; a non-constant computation against a date is
  refused rather than guessed at.
- **`groupBy` keys stay members** — computed keys are a naming decision about `x.Key`.

## Steps

1. `GrammarRule` clause scope as a set; every existing language builds an identical grammar
   (snapshots prove it); validator green.
2. `where` and ordering gain the operators bare after a member: tokenizer tests, `Condition` and
   ordering roots read the fold, engine probes (`OrderBy(x => x.Total - x.Paid)`,
   `Where(x => x.Total - x.Paid > 5)`).
3. the value-side group: group opener and `MathOperand` matchers, folded constants read through
   the converter against the comparison's type (`(2*1000+1)` meets `x.Date` as `2022-12-31` meets
   it).
4. snapshots and self-checks (derived delimiters, the refusal rows), README, per-engine assertion
   tests.

## What the implementation added to the design

- the group opener gained one matcher: `(` is a token after a comparison in any clause, and
  `MathOperand` after that pair of tokens - adjacency only, no lookahead. Which of the two a bracket
  *means* - a math group or the condition group it was - is then the parse's question, and the
  parser asks its own built tree: the last element committed under the clause is a comparison, an
  operator or a group when the bracket stands on a value side, and nothing else when the filter's
  `(` opens a condition group after `[`, `and` or `or`. No new fact had to join the parsing context;
  the tokenizer's window walk stayed a single pass.
- a group holding one written value (`where[x.Int>(5)]`) commits that value as the free text of the
  clause: no operator distinguishes one number from the number of a date. The parser reads a
  `NameOrValue` that is quoted text or a number as the operand of such a group; a bare word is no
  operand, as it is nowhere else. The integration test that refused `(5)` for answering 400 now
  asserts it answers the same rows as `5` - the parentheses group, they are no accounting negative,
  which the converter refuses on purpose.
- a run of written values alone folds to a binary expression and not to one constant — an expression
  tree gets no compiler — so the builder evaluates the fold once, at build time, and hands the
  compared type the text of the answer through the converters the comparison context carries; the
  year rule and every host override then read a computed number exactly as they read a written one.
  A run that hangs on the row is never evaluated: it travels to the engine as the expression the
  same Linq written by hand sends.
- the closers a written operand ends at grew by the words of a filter that read the value the operand
  is part of (`= ! < > & |`), derived from adjacency exactly as the ones the select plan shipped, and
  the self-check test of `GetDelimiters(MathOperand)` says so.
- what a grammar answers is recorded per class — which element opens a query, whose child each
  element is, what word spells a token, which tokens a token stands after and before, which clause
  root it is a word of — for the invariant language, and compared across every language of the
  tokenizer, so step 1's claim that the languages stay one grammar is proven and not asserted.
- a condition builds no expressions of its own: the builder answers `BuildValue` for one value a
  clause reads and `AnswersWithItsOwnType` for the question of which side of a comparison says what
  is compared, so the filter, the ordering roots and a select member are built by one machinery.

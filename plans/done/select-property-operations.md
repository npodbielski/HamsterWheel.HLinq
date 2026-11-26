# Property operations in select - `select[FullName=x.Name+" "+x.Surname]`

The roadmap's "property operation in select": a member of a select that is not one member of the
row but what the query *writes* - `FullName=x.Name+' '+x.Surname`. The call plan (methods in
select) built the machinery for a member that is a call; this one gives select the one operator
the text can spell without taking something away from every other value of the language.

## What was measured

### What EF Core translates (expression-tree probes, EF 9, stopped at `ToQueryString`)

The tree built for a join was `Expression.Add(left, right, string.Concat(string,string))` - the
user-defined-operator form LINQ C# compiles `a + b` over strings to - and a non-text operand
reached it as a parameterless `ToString()` call:

| expression | npgsql | mysql | sqlite |
| --- | --- | --- | --- |
| `Name + ' ' + Surname` | `COALESCE(r."Name",'') \|\| ' ' \|\| COALESCE(r."Surname",'')` | `CONCAT(CONCAT(COALESCE(...), ' '), ...)` | `COALESCE(...) \|\| ' ' \|\| ...` |
| `Name + Int.ToString()` | `\|\| r."Int"::text` | `CONCAT(..., CAST(... AS char))` | `\|\| CAST(... AS TEXT)` |
| `Name + NullableInt.ToString()` | `\|\| COALESCE(r."Int"::text,'')` | same with cast | same with cast |
| `Name + Decimal/Char/Guid.ToString()` | `::text` | `CAST AS char` | `CAST AS TEXT` |
| `Name + Flag.ToString()` | `CASE WHEN ... THEN 'True' ELSE 'False' END` | same | same |
| `Name + Enum.ToString()` | **two columns, no concatenation at all** | same | same |
| `ToUpper() + Surname`, `(Name+Surname).ToUpper()` | `upper(...) \|\| ...` | same shape | same shape |
| `Int + 1`, `Int - 1`, `Int * 2`, `Int / 2`, `Int % 2` | `+ - * / %` directly | same | same |
| `Int + Long` (members) | `r."Int" + r."Id"` | same | same |
| `Int + Decimal` (mismatched) | refused client-side: "The binary operator Add is not defined for the types..." | same | same |

The enum row is the trap: EF answers *two columns* instead of a concatenation and no exception -
the query silently gets a different shape, so an enum operand is refused by HLinq before EF
ever sees it. The mismatched-numbers row is C#'s own refusal (the same one hand-written Linq
gets), so HLinq passes it through as a named refusal. Everything else translates in all three
packs; the concatenated `+` is left-associated into nested `||`/`CONCAT` by EF itself.

In memory (the dummy applier tests) the same tree compiles and evaluates: `string.Concat` for
text, C#'s own `+` for numbers.

### How select decides a value vs a call after `=` today

`Assignment` (`=`) is select-only; after it, `PropertyAssignment.Parser` opens on
`[NameOrValue, Assignment, ..]` (a rename), `[Entity, Dot, ..]`/`[SubEntity, Dot, ..]` (a member)
and `[MethodName, LeftCircleBracket, ..]`/`[Assignment, MethodName, LeftCircleBracket, ..]` (a
call); the tokenizer kept value-vs-call honest without knowing any of this - in the window
`reverse(`, `NameOrValue` cannot complete at `(` while `MethodName` completes exactly there, and
in `abc]` only `NameOrValue` ever completes. The same mechanism, not a new one, is what this plan
needs for what may stand after a `+`.

### What a `+` costs the tokenizer (measured with a prototype over the real grammar)

A `ConcatOperator` word (`+`, `insideClauseOf: typeof(Select)`) whose one adjacency rule allows
it only after `PropertyName`, `RightCircleBracket` (a call's close) or a quoted text, plus a
`QuotedText` word allowed only after `ConcatOperator` - and the whole existing suite still
passes except the self-check that *derives* delimiter sets, exactly as the design says it should:

- `+` became a delimiter of `PropertyName` (and of the new free-text `QuotedText`) - in every
  clause, the way `=` is one of `NameOrValue`'s delimiters in every clause: the derivation is
  clause-blind by rule. A `+` written in a `where` therefore still dies, as an unknown token
  instead of a broken name (`where[x.Name+1==a]` refused before and after).
- the value grammar is untouched: `+` is not a delimiter of `NameOrValue`, because `+`'s rule
  never starts after a value - so `where[x.Name==a+b]`, `where[x.Name==x+1]`, `where[x.Name==+48123]`
  and `select[A=+17]` tokenize exactly as they do today (all measured, `a+b` still one value),
  and the old char `+` of the value is safe because `NameOrValue` already swallows it.
- a signed number after `=` stays a number: `+` may not stand after `Assignment`, so `A=+17` is
  `Assignment NameOrValue(+17)` unchanged.
- `select[FullName=x.Name+' '+x.Surname]` tokenizes to `Assignment Entity . PropertyName
  ConcatOperator QuotedText ConcatOperator Entity . PropertyName`, with renames, several joins,
  joins inside a nested select (`a.City+' '+a.City`), joins hanging on calls
  (`x.Name.ToUpper()+'x'+x.Text`, `reverse(x.Name)+'x'`) and around them all measured tokenizing;
  whitespace around the operator costs nothing (the walk slides whitespace windows away already).
- trailing garbage (`x.Name+`, `x.Int+1`, `x.Name+]`) dies: after `+` only a member, a call or a
  quoted text is a token, and a bare number is not - see the decision below.
- a quoted text after the `=` of a rename opens no join: `select[FullName='Mr '+x.Name]` stays
  the free value of the assignment, because the first window after `=` is read by the value
  grammar and none of the select's own words competes for a window that opens with a quote. A
  join opens with a member or with a call.
- on the wire the operator travels percent-encoded (`%2B`), measured against the demo endpoint:
  the binder URL-decodes every segment of the query string before tokenizing, and the decoding of
  a query string turns a bare `+` into a space - the same rule that makes an `&` of a value
  travel as `%26`. A query handed to the parser directly writes the operator as itself, which is
  what the language tests do, and the README says both spellings.
- `GrammarValidator` is green: one rule per token, both new tokens reachable, and `+` swallows
  no longer keyword it competes with.

## The syntax

**`+` inside a select joins the answers written on either side of it, left to right, no
precedence and no parentheses.**

- `select[FullName=x.Name+' '+x.Surname]`, `select[FullName=x.Name+'-'+x.Nested.Name]`.
- **A written value inside a join is quoted text**: `' '`, `'|'`, `'Mr '`. A bare text or a bare
  number after `+` is not a token, on purpose: making one would put `+` into `NameOrValue`'s
  delimiters in every clause and break values like `where[x.Name==a+b]`, which is the shipped
  language.
- **A join names itself by nothing, so it must be renamed.** An unnamed `select[x.Name+' '+x.Surname]`
  is refused with the reason ("a joined value has no name of its own; write `FullName=`"). A call
  names itself by its method because a method has a name; a join has none, and no spelling of it
  would be the same answer in every culture's language.
- **What `+` does is what the answers are**: text answers concatenate (`x.Name+' '+x.Surname`);
  two numeric answers add (`x.Int+x.Long`) - the CLR decides both, so a combination C# cannot add
  (`x.Int+x.Decimal`) is refused with the same words hand-written Linq gives its author; any
  other answer (bool is text via `True`/`False`, dates and GUIDs cast) follows EF's own cast.
  This is *arithmetic over the row's own members*, and it is all the arithmetic there is: a
  written number (`x.Int+1`) needs an unquoted number after `+`, and that price is the broken
  `where` values above, so it is not paid. `- * / %` are refused by not being words at all.
- **A join may hang on a call** (`x.Name.ToUpper()+'!'`, `reverse(x.Name)+'x'` - the packs' set,
  same as calls) **and a join's pieces may be calls**, built by the existing call machinery.
- **Inside a nested select the spelling is identical** and a piece reads whichever scope its name
  hangs on (`C=a.City+' '+x.Name`), the existing rule for inner renames and inner calls.
- An enum member as a piece is refused: EF answers no concatenation for one at all - two columns,
  no exception - and a wrong answer is not something to hand a caller.

## Design

### Tokenizer (two words, no backtracking, no lookahead)

- `ConcatOperator` - keyword `+`, clause `select`, one rule: previous tokens end in
  `PropertyName`, `RightCircleBracket` or `QuotedText`. Adjacency carries the whole meaning
  ("the `+` of a join is the `+` that follows what can be joined"), so a `+` starting a window -
  a sign in a value, the first character of a value - is never its window.
- `QuotedText` - no keyword, free text between a matching `'...'` or `"..."` pair (pattern over
  the window, complete at the closing quote; a bare unopened window dies immediately), allowed
  only after `ConcatOperator`, clause `select`.
- adjacency edges added where the join can stand or end: `Entity`, `Comma`, `RightSquareBracket`,
  `RightCircleBracket`, `MethodName` and `SubEntity` learn to follow a `QuotedText` or the
  operator as their places need (`MethodName`/`SubEntity` after `+`, closers after a quoted
  text). Delimiters are derived, so nothing declares `+` as an ending character.
- derived consequences asserted as tests: `GetDelimiters(typeof(PropertyName))` gains `+`
  (the one self-check that must change), and the snapshot file gains the new spellings plus the
  unchanged old ones (`where[x.Name==a+b]` staying one value is a snapshot entry, not a hope).

### Parser

- a new tree element `Concatenation` (`TreeBranch`, valid parent `PropertyAssignment`) opens at
  the `+` itself and claims only it, exactly the way `PropertyAssignment` opens on `[Comma]` and
  lets children claim what follows; the elements it gathers are the same elements the assignment
  gathers beside it: `Property`, `InnerProperty`, `Method` (their valid parents gain it). One
  element per written `+`, so a join of three pieces is two `Concatenation`s and one first value.
- `InitializerConstantValue`/renames are untouched; `PropertyAssignment` gains
  `Concatenations` (the children, in the order written) and says `NeedsObjectInitializer` when
  it holds any.
- the naming rule sits where the other naming rules sit: `FinalPropertyName` stays rename-only
  for a join (a join contributes no name), and the refusal is one exception on `PropertyAssignment`.
- the values a join reads are read by what answers them, not by position - a `Concatenation`
  holding `[Property, Method]` is a call hanging on a member (the `ConditionExpression` shape),
  one holding a lone `Method` is a static, one holding `Property`/`InnerProperty`/`QuotedText`
  is that thing; a `JoinedValue` internal composition (this file's, beside `ConditionExpression`)
  builds one piece's expression against the builder context.

### Builder

- `PropertyAssignment.MemberBindingConverter` gains the join branch before the constant branch:
  `GetPropInfo` asks the fold's type, `Build` binds it.
- the fold: build the first piece, then fold each `Concatenation` left to right. When either
  side answers text: `Expression.Add(left, Text(right), stringConcat)` where `Text(x)` leaves a
  string alone and makes any other answer `ToString()` - the exact tree EF translated in the
  probe. When neither does: plain `Expression.Add(left, right)` and let the CLR refuse what it
  cannot add, wrapped as a named `HLinqQueryException` (code `refused`, both types in the
  sentence) like the comparison's operand refusal.
- an enum piece is refused before the fold, naming the member (the measured EF trap); a call
  piece goes through the very call path an assignment's call uses (entity aggregate first
  refusal, instance methods, packs' statics) - reuse, not a second implementation.
- the value of a piece written as quoted text is a `Constant` of the unquoted text - culture
  never touches it, it is text in and text out.

## Decided

- **One operator, `+`, and only in select.** What the roadmap line spells is what gets a word.
- **Written values inside a join are quoted**, decided by the measured cost of the alternative
  (bare text or numbers after `+` would reinterpret `+` inside every `where` value).
- **A join must be renamed** - it names nothing, and inventing a name for `x.Name+' '+x.Surname`
  (a "FullName" from two members?) would be the language guessing.
- **`+` adds numbers when both sides answer numbers** (members only), because EF translates it on
  all three engines and the CLR decides refusals - including the mixed widths, which C# adds by
  promoting and the tree must promote itself (`Expression.Convert` to the promoted type, which
  every engine's translator reads as its own cast); **written numbers in an expression are out**.
- **Enum pieces are refused** (measured: EF gives two columns and no exception).
- **No precedence, no parentheses**: a join is a written list of pieces, folded left; a parenthesized
  sub-expression is more tokens than one operator is worth.

## Out of scope

- arithmetic operators beyond `+`, written numbers inside expressions, comparisons or calls to
  operators in `where`/`orderBy` (filters keep values and calls).
- `- * / %`, boolean operators over members, `string.Format`-style spellings.
- the wildcard plan's star, which shares nothing with this beyond living in a select.

## Revision - the review asked for the whole language at once

The first commit of the MR answered only `+` with `'` quotes; the review asked, in the MR, for
what C# means in general and for fewer tokens. This is what shipped:

- **`+ - * /` are four words of one family** (`IMathOperatorToken`, beside `IComparisonToken` and
  `ILogicalOperatorToken`), select-clause-scoped. An operator stands only after what can be a
  left operand - a member, a call's closing bracket, a written operand, a group - and never
  after a free value, which is what keeps the clause-blind delimiter derivation from touching
  any free text anywhere: the dates, GUIDs and search texts of every clause keep being one
  value, asserted as snapshots.
- **one written operand token, `MathOperand`**, two self-ends decided by its first characters
  the same way `NameOrValue` decides its quote mode: quoted text completes at its matching
  quote (a bracket, a comma, an operator between the quotes is text), a number completes at
  the first character it cannot be made of (so `x.Total*2-1` chains operator after operator).
  Quotes are the language's existing `"`.
- **parentheses group an expression**: `(x.A+x.B)*2` folds the group first, and a group is
  where a written value gets the grammar of an operand - `(2022-12-31)` is two subtractions,
  because a bare date is what the free value grammar of a clause reads and a group is where
  the query says arithmetic. The brackets are the select's own element, of the shape the
  condition group of `where` has.
- **precedence is C#'s**: the flat list of values and operators folds the multiplicative runs
  first and the additive ones left to right, so `x.A+x.B*2` is `x.A + (x.B*2)`; a group is one
  more value in that list.
- **a four digit number compared to a date is the year** it spells - the moment the year
  starts - in the converter's `DateTime` and `DateTimeOffset` branches, the reading the whole
  text gives when it is written with month and day.
- an operand of a rename after `=` alone stays the free value of the clause (`Name="x"` says
  text, `Date=2023-10-12` says that text), an expression opens with a member, a call or a
  group - the shipped spellings are untouched, the new ones say arithmetic by themselves.
- unary minus stays out: a `-` opening a window is a sign or a value character, never the
  operator.

## Delivery steps## Delivery steps

- **S1** - the two words and their adjacency; tokenizer tests for every measured query (joins,
  renames, nested, calls, refusals, the unchanged `where` values), snapshot entries, the
  delimiter self-check updated, `GrammarValidator` green.
- **S2** - `Concatenation` element + `PropertyAssignment` members + the naming refusal; parser
  structure tests (tree shapes for rename+join, nested join, call pieces).
- **S3** - the builder: fold in `MemberBindingConverter`, text/numeric decision, enum refusal,
  quoted-text constants; in-memory applier tests (concat of members and text, rename required,
  call pieces, numeric add, refusal sentences).
- **S4** - provider translation tests (npgsql/sqlite/mysql): `||`/`CONCAT` shape, the cast for a
  numeric piece, one nested-select statement; integration test against postgres answering a full
  name; README (Selecting gains "Joining members"), roadmap tick, `PLANS.md` row, plan moved to
  done.

## Definition of done (per AGENTS.md)

`dotnet build HLinq.slnx`; FULL unit + netstandard suites (never filtered); integration against
postgres or CI; `dotnet format --verify-no-changes`; `jb inspectcode` with no new issues over the
origin/alpha baseline; README documents the syntax; branch pushed, MR to `alpha`, pipeline green,
then stop and wait for review.

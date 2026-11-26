# Wildcard selection of properties - `select[x.*Name]`

A select names its members one by one: `select[x.Name,x.Text]`. When a row wants many members that
share a naming pattern - every `*Name`, every `Nullable*`, every date - the query writes each of
them, and the list is the caller's duplication of what the entity already says. The wildcard spells
the pattern instead: `select[x.*Name]` answers every member of the subject whose name matches.

## What was measured

Nothing by probe yet - the measurement is the shipped code itself:

- **Which members does the pattern mean?** Member resolution is shipped and settled
  (`plans/done/member-resolution.md`): a name answers one member by a ranked, reported decision.
  A pattern is the same contest with more than one winner, so its answer is a *set* of members,
  and the warning report the resolution already writes is the shape for saying which members the
  set holds. Measured: matching a pattern is set membership over `PropertiesCache.From(type)` -
  declaration order, `StringComparison.OrdinalIgnoreCase`, `*` mapped to a prefix/suffix test -
  it does not need the ranking at all (a pattern names no candidate to rank), only the cache and
  the report shape.
- **What is the pattern's own grammar?** The tokenizer commits a token per window with no
  backtracking and no lookahead; a word's delimiters are *derived* from the tokens its rule
  allows to follow it (`Grammar.GetDelimiters`), and clause scope is `GrammarRule.MatchesRoot`
  (a word belongs to a clause root, set when the language is built - see `plans/done/root-aware-grammar.md`).
  `*` opening a name (`*Name`) cannot be read as anything today - a name token never starts with
  `*` - so a leading-star word clashes with nothing.
  A trailing star (`Name*`) cannot be a *token*: the walk commits the **first** completion, and
  the name `Name` completes the moment the window reaches `*` (`*` is a derived delimiter of a
  name - multiplication may follow a member in select), so a `Name*` word could never win over
  `Name`; making it win means longest-match, which is lookahead the design forbids. But the
  trailing star needs no token: a `*` standing where multiplication cannot (before `,`, `]`, or
  the end of a value - a multiply with no right operand is a parse error today) is a fact the
  committed stream already states, and the parser reads the pair `Name` + dangling `Multiply`
  as one element. The two readings are exclusive by construction: multiplication always needs a
  right operand, and a pattern cannot carry member access after it, so `x.Name*x.Age` - the
  collision feared - is only ever the multiplication, and `x.Name*,x.Id` - the only reading
  that closes - is the wildcard. See the grammar design in AGENTS.md: a stage never reaches
  forward, and what a committed word *meant* is decided by the parse, not the walk.
- **Is there a cap on the answer's width?** Yes, shipped: `HLinqOptions.MaximumNumberOfProperties`
  (default 100), checked per root by `HLinqQueryApplier.ValidateNumberOfProperties` with
  `TooManyPropertiesException`. The expansion must count into it, not around it.
- **How deep does one element answering many members reach?** The applier's contract is 1:1:
  `ExpressionBuilder` reads the answer shape as `members.Select(m => scope.ToPropInfo(m))` and
  builds it as `members.Select(m => scope.ToMemberAssignment(m, shape))`, through
  `ElementToMemberAssignmentConverter<T>` - one tree element, one `PropInfo`, one
  `MemberAssignment`. A wildcard is one element answering N members, and it can only expand where
  the type is known - inside this very pipeline. Changing `ToPropInfo` (or the converter
  interface) to answer N would ripple through every converter, every caller and the shape
  computation alike - a change across the library to teach it something only `select` can ask.
- **Where is `MaximumNumberOfProperties` checked?** On the tree, before any type is known
  (`HLinqQueryApplier.ValidateNumberOfProperties` -> `MaxNumberOfProperties()`): a wildcard is
  one element there, so it would sail past a cap it is supposed to obey.

## Design

- **The parse answers the pattern, the builder resolves it.** What the text means is "every member
  of this subject whose name matches `*Name`" - a fact of the text and the pattern. Which members
  the CLR type has is member resolution, which is deliberately not the parse's question (AGENTS.md,
  *The three stages*): so the tree holds the wildcard and its pattern, and the select's member
  list is expanded where the type is known.
- **A wildcard is one member source among many.** `select[C=count, x.*Name]` keeps a rename and a
  call beside the expansion; the existing naming rules decide each member's name
  (`PropertyAssignment.FinalPropertyName`) and an expansion names each member by the member it
  matched.
- **Collisions are said, not silently kept.** Two wildcards, or a wildcard and a spelled-out
  member, can name the same property twice - the group's `Rows`/`Key` collision handling already
  reports a name with two candidates and that report is the shape to follow.
- **The language owns the word.** A wildcard is a word of a language like any other
  (`IHLinqWord`, registered per culture), so a culture can spell the star differently; the
  grammar validator checks it as the language is built (`GrammarValidator`, `ValidateLanguages`).

## Decided

- **No bare star.** `x.*` is not the language: select-all only makes sense with a way to drop
  members again, and dropping is not a Linq query - the caller names the star it means.
- **Star positions: leading, trailing, and both.** They are different machinery, and the usage
  ranks them: a caller remembers a name's *beginning* more often than its ending, so `Name*`
  (starts-with) is the common case even though it is the harder half of the grammar.
  A leading `*` is a token (nothing completes at a window opening with `*`); a trailing `*` is
  the parse of a dangling multiply (above), and both positions together (`*Name*`) are the
  second, read by the same parser rule after the leading word - the pattern ends where a
  multiply dangles again: `x.*Name*,`. A wildcard operand of a real multiplication
  (`select[N=x.*Name*2]`) is refused: a pattern is a member source naming columns, not a
  value to compute on.
- **Wildcards match what a spelled select may name bare: scalars.** A collection member (an EF
  navigation, `List<Address>`) has no bare spelling - spelled select takes it only through
  `.select(...)` or an aggregate call - so a wildcard neither pulls nor silently skips one:
  a pattern whose match set is empty because only navigations matched is reported, not answered
  with a missing-column surprise.
- **Case-insensitive, like every other name the query writes.** Member resolution already
  compares names that way and warns when ordinal order decides a tie; the pattern comparison
  is the same comparison.
- **Expansion is a pre-pass at the two places that read a select's members, not a widened
  converter.** The wildcard element is replaced - in `GetSelect` and `GroupRowsSelection` - with
  the N ordinary `PropertyAssignment` elements the caller could have spelled, produced from
  `PropertiesCache.From(type)` filtered by the pattern, in declaration order, scalars only. The
  1:1 contract of `ToPropInfo`/`ToMemberAssignment` never learns about multiplicity; everything
  downstream - the shape, `MemberInit`, naming through `FinalPropertyName`, collision reports,
  the width cap measured on the real member list - sees spelled-out members and is untouched.
  The alternative - a `ToPropInfo` that answers several properties, a converter interface with an
  N in it - is rejected by the maintainer: it changes a contract every converter and every caller
  across the library honours, in several of the paths that can never see a wildcard (filters,
  orderings, nested scopes), to serve a question only the root select asks.
- **The existing cap counts what a wildcard answers.** A wildcard cannot sail past
  `MaximumNumberOfProperties`: the width check reads the expanded member list, not the tree. Where
  that check then runs is the plan's one open question (below).
- **What order the expansion answers in**: fixed and asserted, not left to reflection's
  enumeration order - declaration order of the matched members.

## Open decisions

1. **Where the width check runs once a wildcard exists** - validation knows the entity type, so
   it can expand and count the real width there, and the tree-level check disappears; or the
   tree-level check stays a coarse bound and the real width is measured at apply time, which
   moves when a too-wide query fails. Proposed: expand in validation too - the cap counts
   columns, and only the expansion knows how many a wildcard means.

## Steps

- **S1** - the grammar, both halves: the leading-star word (its `TokenPattern` and adjacency
  rule, `GrammarValidator`/`ValidateLanguages` passing, tokenizer tests for `x.*Name`,
  `x.*Name,x.Count()` and a value containing `*`), and the parser's trailing rule - the dangling
  `Multiply` after a name becomes the wildcard suffix, tested both ways as the contract the
  validator cannot guard: `x.Name*,x.Id` and `x.Name]` parse as wildcards, `x.Name*x.Age` parses
  as the multiplication it always was, `x.*Name*,` closes a both-positions pattern, and
  `x.*Name*2` is the refusal of a pattern under a multiply.
- **S2** - the tree and the expansion: the wildcard element (pattern, position) parsed from the
  token stream, and the pre-pass at `GetSelect` and `GroupRowsSelection` that replaces it with
  the spelled-out `PropertyAssignment`s it matches - scalars only, declaration order fixed and
  asserted, empty match reported, collisions reported the way `Key`/`Rows` collisions are; the
  width check reads the expanded list (open decision 1 decided with it).
- **S3** - integration: the statements each engine is asked for a wildcard select, asserted like
  the spelled-out select is, in every pack.
- **S4** - README *Selecting*. (The parity matrix this step once pointed at was dropped with the
  parity plan - the wildcard has no Linq twin anyway.)

## Out of scope

- Wildcards in `where`, `orderBy` or `groupBy` - a condition or an ordering names one member, so
  a set has no meaning there.
- Wildcards over a nested select's element (`select[A=x.Addresses.select(a.*)]`) - the same
  shape, decided after the root case works.
- Dropping members from a select (`select-all-but`) - not a Linq query, and the reason bare
  `x.*` is out.

## Definition of done (per AGENTS.md)

`dotnet build HLinq.slnx`; unit, netstandard-unit and integration suites green; `dotnet format
HLinq.slnx --verify-no-changes`; `jb inspectcode` at the baseline count; branch pushed, MR opened,
pipeline watched to green, then stop and wait for review.

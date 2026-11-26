# Parse-time clause scope - the grouping is a fact of the parse, not of the apply

The pipeline is three stages: the tokenizer turns text into tokens, the parser turns tokens into a
tree of elements, the applier turns the tree into `IQueryable` calls. Each stage may read what the
stage before it produced and nothing later. Today a `groupBy` changes what the words of the clauses
around it mean - `x.Count()` only has a meaning after one, `x.Rows` only after one, `x.Text` only
*before* one - and that difference is decided in the **applier**, which reads the tree, decides what
a member meant, and refuses a query that the parser was perfectly able to see was meaningless.

The grammar design (AGENTS.md) already says the clause is a *committed fact* the walk may read;
`!112` put the first piece of that into the parse: `IParsingContext.GroupingApplied`, set when a
`GroupByRoot` finishes parsing, and `SelectRoot.RowsSelection` resolved while the select is parsed.
This plan finishes the move: every question a `groupBy` changes is answered while the clause that
asks it is parsed, so an ill-formed query is refused by the parser and the applier builds
expressions and nothing else.

## What was measured

Applied against the shipped code (`Apply` over an in-memory queryable, `DummyEntity`):

| query | refused today | where |
| --- | --- | --- |
| `where[x.Count()>1]` | `'Count()' stands on the entity of the clause here, and there is no group to aggregate over` | apply |
| `orderBy[x.Count()]` | same | apply |
| `where[x.Rows==1].groupBy[x.Name]` | `Invalid property path 'Rows' ... Available properties at this point are: 'id', 'name', ...` | apply - **stays there**: no grouping was committed, so this is a plain name read against the entity, and the parser holds no entity type |
| `groupBy[x.Name].orderBy[x.Text]` | `Invalid property path 'Text' for entity 'IGrouping`2'` | apply |
| `groupBy[x.Name].where[x.Rows==1]` | refused with the reason (`GroupedResultPropertyValidator`) | apply |
| `groupBy[x.Name].select[Rows=x.Text]` | refused with the reason | parse (since !112) |
| `groupBy[x.Name].select[Rows=x.Rows.select(a.Text),x.Text]` | refused with the reason | parse (since !112) |

So the two halves of the same rule - *after a `groupBy` the clause stands on a group, before one it
stands on a row* - are decided in three different places, and five of the seven refusals above
happen after the tree was built. Of those five, the ones that turn on the grouping alone (rows 1,
2 and 5) are what this plan moves - the parser sees the clause, its position and the committed
fact, and needs no type to answer; the others are member resolution against the type at hand (rows
3 and 4, a name read against the entity or against `IGrouping`) and stay resolution questions.
The messages named above are already good; the plan moves them, it does not invent them.

Also measured: nothing in `src` decides "is there a group" from the *queryable* any more than it
needs to. `IQueryableContext.Grouping` is the runtime companion (it carries the grouping's state:
key member, row selector, aggregates), and the clauses read it to decide what a member means -
which is the parse's question. The parse already knows: `GroupingApplied` says whether a
`groupBy` was committed, and the order of the roots says which clause stands where.

## Design

### The fact, and where it is read

`IParsingContext.GroupingApplied` is the committed fact: set when a `GroupByRoot` finishes parsing,
read by the parser of any clause element that can hold a call over the group. It says that a
grouping was committed *before this point in the query* - which is exactly the grammar rule the
tokenizer already uses for clause scope ("committing a clause root says which clause we are in"),
one stage later and one question richer.

A clause element asks the question about itself while it is built, the way `SelectRoot` now does:

- `Method` (the call node, `Tree/Filtering/Method.cs`) knows its own name and whether it hangs on
  the entity (`x.Count()`) or on a member (`x.Addresses.Count()`). It gains the answer to "am I an
  aggregate over the group I stand on", stored on the element. `!112` left this decision in
  `PropertyAssignment.EntityAggregate`, which the select applier reads; it moves here, so the tree
  says what a call is and nobody re-derives it.
- `WhereRoot` / `OrderByRoot` / `OrderByDescendingRoot` / `ThenBy*`, while finishing, ask the same
  question of their own members: a call over the entity with no grouping applied is refused there
  (today: `EntityAggregateNeedsGroupException`, thrown from the aggregate converter at apply time);
  a path naming `Rows` or `Key` where a grouping *was* applied is refused there too (today:
  `GroupedResultPropertyValidator`, called from the appliers). The appliers stop asking.
- `SelectRoot` keeps what !112 gave it (`RowsSelection`), and the aggregate assignments it lists today
  at apply time come from what the `Method` elements already say about themselves.

`IQueryableContext.Grouping` stays: the applier needs the grouping's *state* to build the
expressions (key member, row selector, aggregates). What it must not do, and stops doing, is decide
what a word of the query meant.

### What moves out of the appliers

`WhereRoot.Applier`, `OrderBy*.Applier`, `SelectRoot.Applier` lose their `context.Grouping is not
null` branches that only validate or only interpret. They keep the branches that build: the grouped
projection, the row selector, the aggregate selectors. `GroupedResultPropertyValidator` and
`PropertyAssignment.EntityAggregate`/`AggregateName` as read-by-appliers disappear with them.

### What the parse cannot know

Two decisions stay in the builder, and the plan says so out loud:

- **which member a name means** - whether a path names a property of the row, of the group or of
  the entity is member resolution (the ranking of `member-resolution`), decided against the CLR
  type, which the parser has no business consulting; `x.Rows` where the *entity* has its own `Rows`
  property stays a resolution answer, refused or bound there and not here;
- **whether the provider can translate a call** - `!EF-translatable` is a property of the pack and
  the engine, measured per provider, never of the parse.

## Delivery steps

- **S1** - move the aggregate-over-the-group question to the tree: `Method` answers it while parsed
  (`EntityAggregate`/`AggregateName` read off it, not recomputed), and `where`/`orderBy` refuse
  a call over the entity with no grouping *at parse*. Tests: the four refusals above, asserted at
  `Parse` instead of `Apply`; every existing group-aggregate test keeps passing untouched.
- **S2** - move the "a clause after a group may name only the key" rule to the parsers of
  `where`/`orderBy`/`thenBy` (from `GroupedResultPropertyValidator`), delete the validator and the
  `context.Grouping` validation branches in those appliers.
- **S3** - `SelectRoot`: the aggregate list and the rows selection both read what the tree says; the
  applier only builds. `AGENTS.md` gains the rule: *a stage reads what the stage before it
  produced - a clause that needs to know whether a `groupBy` came before it asks the parse context,
  never the queryable*.

## Out of scope

- Any change to the tokenizer or the grammar rules. The tokenizer already commits clause roots;
  this plan is one step behind it, in the parser.
- `take`/`skip`/`count`: they work on groups after a `groupBy` by doing nothing different (they
  page whatever the queryable holds), so they ask no question and gain nothing.
- A grammar that makes `x.Count()` parse *only* after a `groupBy`. The token sequences are
  identical; the difference is what the entity type is at that point, which is not grammar's to
  know. The refusal is a parse-time semantic check, not a token-pattern one.
- The parity matrix (`plans/linq-parity.md`) and the security record
  (`plans/pentest-arbitrary-db-execution.md`) - both may cite where a refusal happens once this
  lands, neither blocks it.

## Definition of done (per AGENTS.md)

`dotnet build HLinq.slnx`, both unit suites, integration tests (CI), `dotnet format
--verify-no-changes`, `jb inspectcode` clean in touched files; `GrammarValidator` untouched (no
grammar change); every refusal named above asserted at the parse stage; the appliers hold no
`context.Grouping` branch that decides meaning.

## What shipped - the plan reviewed against the implementation

- **S1 moved the name and the position, and left the type where it belongs.** `Method.IsGroupAggregate`
  is set while the call is finished parsing - the call stands on `x` and its name is one the language
  folds a collection with - and `PropertyAssignment.GroupAggregate` reads that answer, so nobody works
  the name out again. The plan's *"a call over the entity with no grouping applied is refused at
  parse"* did not land, and cannot: whether there is anything to fold is a question of the **type**
  the clause reads, and the parse holds no type. `AggregateMethodToExpressionConverter` answers it,
  and `ReturnTypeOverSubject` makes the answer the same when only the type is asked as when the call
  is built.
- **That is what a row that is itself a collection needs.** `select[x.Children].where[x.Count()>1]`
  and `select[x.Children].select[C=x.Count()]` are legal - the select answers one collection per row
  and the clause after it folds them like any LINQ source - and alpha refused both. The partition of
  a select's members is the parse's half of the same fact: `SelectRoot.StandsOnGroups`, set from
  `IParsingContext.GroupingApplied`, says whether a call over the subject answers beside the group or
  shapes a row; with no grouping every call member is an ordinary call member.
- **S2: `GroupedResultPropertyValidator` is gone.** The rule is `IParsingContext.CheckAfterGroup`:
  the committed grouping and the question it decides are facts of the context, so a clause finished
  parsing asks `context.CheckAfterGroup()` and names nothing about where the answer comes from.
- **S3: the naming decision moved.** `PropertyAssignment.FinalPropertyName` says what a member that
  is a call answers under - the name the query wrote, or the method when it wrote none - so the
  select applier builds the aggregate list and the row shape without naming anything itself. The
  AGENTS.md rule is in *The three stages*.
- **Refusals that moved earlier now name their real reason.** `groupBy[x.Nope()]`,
  `select[C=x.Nope()]` and `select[C=x.ToUpper()]` failed in the applier with a name-not-found or a
  "no group to aggregate over" message although no group was even asked for; they are refused as what
  they are - a name that is no aggregate of the language - exactly as after a `groupBy`.
- **What stayed is what the plan said stays**: whether the subject of a call has anything to fold and
  which member a name means, both answered against the type, and whether the engine translates a
  call.

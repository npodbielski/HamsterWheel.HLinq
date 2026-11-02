# Nested type selects - `select[Addresses=x.Addresses.select(a.Street,a.City)]`

A roadmap line: selecting members of a related collection (or navigation) as a shaped sub-answer.
The old spelling embedded a root inside a clause (`select[Addresses=select[a.Street,a.City]]`); the
roadmap now spells it as a call - `x.Addresses.select(...)` - which is the same "call in a value
position" mechanism `plans/group-aggregates.md` builds for aggregates, seen from the other side: an
aggregate answers a scalar per group, a nested select answers a shaped collection per row.

## What was measured

### What EF translates

Against the real Postgres of the integration suite through `ToQueryString` (EF Core 9 / Npgsql):

| Linq | what the server is asked |
| --- | --- |
| `Select(x => new { Addresses = x.Addresses.Select(a => new { a.Street, a.City }).ToList() })` | `SELECT p."Id", a."Street", a."City", ... FROM "Persons" p LEFT JOIN "Address" a ON p."Id" = a."PersonId" ORDER BY p."Id"` - one statement, EF assembles the collections |
| the same with an inner `Where` | the inner filter becomes a subquery inside the `LEFT JOIN` |
| the same with an inner `Take(1)` | nested subquery with `LIMIT` inside the join |
| a scalar list (`Select(a => a.City)`) | the same flat join |
| outer `Where`/`OrderBy` + the nested select | one statement, `WHERE`/`ORDER BY` outside |
| an outer member captured inside the inner projection (`a => new { contact = x.Name, a.City }`) | `LEFT JOIN LATERAL (SELECT p."Name" AS contact, a."City", ...)` - the outer column rides into the inner subquery |
| a single (reference) navigation, several members (`new { x.Address.Street, x.Address.City }`) or one (`x.Address.City`) | a plain `LEFT JOIN`, no subquery - the reference is columns of the joined row |
| two levels (`a => new { a.Street, country = new { a.Country.Name } }`) | one statement: the inner join nests inside the collection's join subquery |
| three levels, collections all the way (`x -> a -> c -> s`) | still one statement - EF flattens the chain of joins; no depth the provider refuses was found |

So every shape translates; nothing here needs client-side evaluation beyond what EF always does for
collections (the split mapping over the join).

### What the tokenizer does today (measured over the shipped grammar)

The first draft of this plan claimed the call "already tokenizes" and the gap was only the parse
tree. That is true for the *outer* shape alone; measured over `HLinqTokenizer` with the shipped
words, three things are missing, and one of them needs a new token:

| query | what happens today |
| --- | --- |
| `select[addresses=x.Addresses.select(a.Street,a.City)]` | tokenizes - but `a.Street` and `a.City` arrive as two `NameOrValue` **blobs**: free text swallows the dot, so the inner path is one opaque value, not `alias . name` |
| `select[x.Addresses.select(a.Street),x.Name]` | **refused** - `Comma` is not allowed after `RightCircleBracket`, so a call cannot be followed by another member |
| `select[x.Addresses.select(a.Street,country=a.Country.select(c.Name))]` | the nested inner call is swallowed whole into one `NameOrValue` blob (`a.Country.select(c.Name)`) - depth is invisible to the tokenizer |
| `select[x.Name,address=x.Addresses.select(contact=x.Name,a.City)]` | the outer capture `x.Name` tokenizes right (`Entity Dot PropertyName`) - `x` is an `Entity` word and wins; only the *inner* alias is the problem |

The blobs are exactly the ambiguity AGENTS.md says a token's own grammar must absorb: `NameOrValue`
never holds `..` and swallows `.`, so a name that roots a path inside the call must be a word of
its own. The fix is one new free-text token - `InnerAlias` (the `a` of `a.Street`) - whose text rule
completes at the dot, whose adjacency is "inside the round brackets of the `select` clause, after
`(`, `,` or `=`", and which refuses the entity alias itself (`x.` beside inner members is the outer
row, already tokenized right). Two measured guards keep it from eating what a value already owned:
the rule is offered only while a call is open (more `(` than `)` committed - brackets behind the
window are committed facts, not lookahead), so `select[Version=1.2]` outside a call keeps its value;
and the alias must start with a letter and not be the letter `x`, so a value that starts with a
digit (`1.2`) or is the outer alias (`x.Name` - the capture) stays a value. No syntax peeking,
delimiters derived from adjacency, clause scope declared as every other word declares it. Beside
it, `Comma` gains `RightCircleBracket` as a token it may follow - a call is a member of a list, so
the comma after it is ordinary.

### What the answer machinery accepts

- `DynamicFactory.CreateType` returns the *same* type for the same `(Name, Type)` list (measured:
  reference-equal), so building a shape per nested level cannot explode with duplicate types, and a
  dynamic type holding a `List<dynamicType>` property serializes and round-trips (measured).
- The expression tree `x => new { x.Name, Addresses = x.Addresses.Select(a => new {...}).ToList() }`
  built by hand over dynamic types compiles and answers the nested JSON shape (measured) - the
  recursion is plain `Expression` composition, no provider machinery involved.

## Design

### The call

A `select` method hanging on a navigation - a collection or a single reference - in a `select`
member position (with `=` to rename, or bare):

```http
GET /data?select[x.Name,addresses=x.Addresses.select(a.Street,a.City)]
```

answers `[{"Name": "Ann", "addresses": [{"Street": "Main 1", "City": "Warsaw"}, ...]}]` - the path
left of `.select` is the navigation, the names inside the call are its members. A plain member of
the answer is a path (`x.Name`); the name left of `=` names the member of the answer, the same way
`select[FullName=x.Name]` renames today.

- The call hangs on the navigation path like a method on a property: `x.Addresses.select(...)`, the
  same shape `x.Addresses.Count()` has in the aggregates plan. One rule for both: a call is written
  on the path it operates over.
- The names between the brackets are members of the **related element** (the element type of the
  navigation collection), not of the entity. A name resolves against that type or the query is
  refused naming both candidates, the way member resolution refuses an unresolvable path today.
- Inside the call two paths are in scope and the prefix says which one a name is: `a.Street` is a
  member of the related element, `x.Name` is a member of the outer row - captured beside the inner
  members, which EF translates (measured: the outer column rides into the inner subquery). A name
  without a prefix is the related element's, there being exactly one of them in scope. So a mixed
  projection is expressible:
  `select[x.Name,address=x.Addresses.select(contact=x.Name,a.City,a.Street)]` answers each address
  with the person's name beside its own members. The outer alias is `x` like everywhere else; any
  other single-letter prefix names the element in scope (the first draft fixed it to `a`, but
  nothing needs to be fixed - a prefix that is not `x` can only be the element in scope).
- Renaming already works: the left side of `=` is `InitializerPropertyName` today, so
  `select[Delivery=x.Addresses.select(a.Street)]` answers under `Delivery` - the left name is a
  pure name of the answer, the right side says where the data comes from.

### Parse

Three elements, one per measured gap:

- **`InnerAlias`** (token, `Tokens/Selecting/`) - as measured above: free text completing at `.`,
  inside the `select` clause only, after `(`, `,` or `=`, never the entity alias. `PropertyName`
  and `Dot` gain it as a predecessor.
- **`NestedSelect`** (tree branch, `Tree/Selecting/`) - starts at `. select (` and finishes at the
  matching `)`, the same bracket-finish `Method` does for its own brackets. Its parent is a
  `PropertyAssignment` (the call is the value of a member, renamed or bare) or another
  `NestedSelect` (a member of the inner shape is itself a call). Its children are the inner
  members - `PropertyAssignment`s again, which is what makes depth free: the same element nests
  in itself, no per-level special case.
- **`InnerProperty`** (tree branch) - `a.Street`, `a.Country.Name`, or a bare `Street` where no
  other reading exists (a bare name inside a `NestedSelect` that is not a rename's left side).
  Valid parents: `PropertyAssignment` and `NestedSelect`. It is kept apart from `Property` because
  `Property` is always rooted at the entity alias and resolves against the outer row type; an
  inner path resolves against the element type of the scope its call opened.

`PropertyAssignment` grows the third value kind beside `Property` and `InitializerConstantValue`:
a `NestedSelect` child. Its `NeedsObjectInitializer` and name rules already cover the renamed and
bare spellings unchanged.

The name `select` as a method is the same word as the clause root - `MethodName` after a dot is
already legal after a property path, and the word `select` is already registered; what changes is
which tree branches may nest where, not which words exist. A translated language registers its
word for the `MethodName` spelling as it does for every keyword, and `GrammarValidator` covers it
as it covers every word.

### Convert

`PropertyAssignment.MemberBindingConverter` builds the value expression; for a `NestedSelect` over
a navigation member it is:

- **collection navigation**: `Enumerable.Select(source, a => new { ... }).ToList()` - the row
  projection machinery reused with the related element as its source, which is the same reuse
  `GroupingState.RowSelector` made for grouping;
- **reference navigation**: the same inner projection applied to the one related row - an object,
  not a list (measured: EF asks a plain `LEFT JOIN` for it).

The inner projection is a `MemberInit` over a dynamic type built from the call's members - the
same `DynamicFactory.CreateType` the outer `select` uses, one level deeper. A member of that
projection may itself be a call, so the projection builder recurses one call at a time - the same
machinery at every level, no special case for depth. The result type flows as the member's type,
so the dynamic answer type and the client deserialise the nested shape without new machinery
(measured: a dynamic type holding `List<dynamicType>` serializes).

Resolution of names inside the call goes through the same `IPropertiesCache.Resolve` as every
member does - so the ranking, the cheapest conversion and the warning report are the shipped ones,
asked of the element type instead of the entity type.

### Where it composes

- Beside plain members and constants in one `select` (measured: outer members + nested collection
  in one projection).
- After a `groupBy` (measured in S2): the rows of a grouping are the entity's rows until a `select`
  reshapes them, so the call resolves over them unchanged - `groupBy[x.Name].select[x.Key,
  addresses=x.Addresses.select(a.City)]` answers each group's rows shaped. After such a `select`
  the navigation reaches the row only if that `select` kept the navigation itself - the path
  either resolves on the row type or is refused as any unresolvable path is.
- With inner filtering/paging (a `where` inside the call) - **not** in the first cut; EF translates
  it (measured), but the syntax for a clause inside a call argument is the nested-conditions problem
  the aggregates plan already deferred.

## Decided

- **A nested select without `=`** - yes: `select[x.Addresses.select(a.Street)]` answers under the
  navigation's own name, like a bare `select[x.Name]` does. A call is a member of the list, with or
  without a name of its own.
- **Which navigations** - collections and single (one-to-one / many-to-one) references alike: the
  call over a reference answers an object, not an array - that is how owns-one references work.
  Measured: both shapes are a plain `LEFT JOIN` (`new { x.Address.Street, x.Address.City }` and a
  single captured member), no subquery, no `LATERAL` - the reference machinery is the collection
  machinery minus the list, and it is simpler.
- **Depth** - the parser recurses: a member of a nested select is itself a call, to any depth, with
  no limit of its own. Measured: two levels (collection then reference) and three levels of nested
  collections are each one statement - EF nests the joins and no provider-side refusal was found.
  The limit is the provider's, not the grammar's; a query too deep for the engine fails as a
  translation error, the same way any untranslatable query does.
- **The inner alias is a token, not a parse trick** (supersedes "the grammar needs no new token"
  from the first draft, which the measurement above refutes): `InnerAlias` absorbs the
  `a.Street`-vs-value ambiguity in its own text rule, as `NameOrValue` and `RangeOperator` absorb
  theirs, and the tokenizer still never looks at syntax.

## Open decisions

None - the design is settled; S1 confirms the measurements over the provider packs.

## Delivery steps

- **S1** - done: provider-pack tests (`Providers/NestedSelectTranslationUnitTests`) over a test-only
  EF model (row -> related collection, related -> country reference) on `UseNpgsql` / `UseSqlite`
  / Pomelo (with an explicit `ServerVersion`, so no server is needed), asserting `ToQueryString`
  of the built projection is one statement with a `LEFT JOIN` in each engine - the same assertion
  S3 makes for the live server.
  **Measured finding:** the collection, the reference and the call-under-call are one `LEFT JOIN`
  statement in all three engines - but the *outer capture* (`contact=x.Name` inside the call) is
  a `LATERAL`/`APPLY` join, and SQLite and MySQL refuse it ("requires the SQL APPLY operation,
  which is not supported on SQLite"). The limit is the engine's, exactly as the Decided section
  predicted for depth: the query fails as an untranslatable query, never as silent
  client-evaluation.
- **S2** - done: `InnerAlias` (grammar + word), `NestedSelect`, `InnerProperty`, the third value
  kind of `PropertyAssignment`; tokenizer and parser unit tests over every shape of the tables
  above, including `select[x.Addresses.select(a.Street),x.Name]` (a `Comma` after a call's `)`).
- **S3** - done: the sub-projection expression (collection and reference), the answer shape
  through the serializer; unit tests over in-memory sources asserting results against hand-written
  Linq; integration tests against Postgres asserting the result against hand-written Linq and -
  through a test context with a command counter - that one HLinq query is one round trip, not
  N+1. The demo model gained the navigations the tests query (`Person.Addresses`, and
  `Address.Country` for the depth case) with seeded rows.
- **S4** - done: README (the Selecting clause gained the nested select, the roadmap line ticked);
  the Polish language names the call with its own word for `select` (`wybierz`), which the parser
  accepts because the call is checked against the registered keywords of the root, not a literal.

## What shipped - the plan reviewed against the implementation

- **Token** - `InnerAlias` (`Tokens/Selecting/`): free text completing at the dot, refused for the
  entity alias itself and for a first character that is not a letter, offered only while a call is
  open (bracket count over committed tokens - a fact behind the window, not lookahead) and only
  inside the `select` clause. `Comma` gained `RightCircleBracket` as a predecessor; `Dot` and
  `PropertyName` gained the alias. Measured refusals hold: a dotted value outside a call
  (`select[Version=1.2]`) and a dotted value inside one (`StartsWith(v.2)`) stay values.
- **Parse** - `NestedSelect` (starts at `. select (`, finishes at the matching `)`, name checked
  against the registered keywords of the `Select` root - so a language naming its clause names the
  call: Polish answers `wybierz[x.Int,addresses=x.Int.wybierz(a.Int)]`), `InnerProperty` (alias-
  rooted or bare-inside-a-call member path), and `PropertyAssignment` as the third value kind -
  every member of a call is a member assignment again, which is what makes depth free.
- **Convert** - `NestedSelectBuilder`: the inner shape is a `DynamicFactory` type built from the
  call's members, a collection answers `Enumerable.Select(...).ToList()`, a reference answers the
  `MemberInit` bound over the related row (null when the row is missing, like a hand-written path
  would). The scope an inner name resolves in is `NestedScopeBuilderContext` - outer context plus
  the element's source and type, nesting so a capture of the outer row reaches through any depth.
  Member resolution goes through the same `IPropertiesCache.Resolve` as everywhere, so the
  ranking and warnings are the shipped ones asked of the element type.
- **Measurements held**: one statement with a `LEFT JOIN` in Postgres (asserted through the live
  server: result equals hand-written Linq, SQL is a single statement, command counter says one
  round trip), in SQLite and in MySQL/MariaDB (asserted through `ToQueryString` per pack). The
  outer capture rides a `LATERAL` on Postgres and is refused by SQLite/MySQL (`APPLY` unsupported)
  - an engine limit, said as a translation error, never a silent client evaluation.
- **Answer shape**: dynamic types nest (`List<dynamicType>` as a member), serialize, and the
  client deserialises them with no new machinery - the plan's claim, verified end to end.
- **`groupBy`**: measured and tested - the rows of a group are still the entity's rows, so the
  call resolves over them unchanged: <br>`groupBy[x.Name].select[x.Key,addresses=x.Addresses.select(a.City)]`
  answers each group's rows shaped, in-memory and through the EF translation like any other.
- **Demo** - `Person.Addresses` (collection) and `Address.Country` (reference) joined the demo
  model, seeded two rows per person; the navigations are `[JsonIgnore]`, so a query that does not
  shape them does not answer them either.

## Out of scope

- Filtering or paging inside the nested select (nested conditions - shared deferral with
  `plans/group-aggregates.md`).
- Joins across resources: the navigation must exist on the entity; HLinq still queries one resource.
- Aggregates inside a nested select (`x.Addresses.select(Total=x.Addresses.Sum(a.Total))`) - belongs
  to the aggregates plan if anywhere.

## Definition of done (per AGENTS.md)

`dotnet build HLinq.slnx`; unit, netstandard-unit and integration suites green (integration against
postgres); `dotnet format HLinq.slnx --verify-no-changes`; integration tests assert one translated
statement per query - a nested select that degrades to N+1 queries is a failure, not a fallback;
README documents the syntax and the roadmap line is ticked; branch pushed, MR opened, pipeline
watched to green, then stop and wait for review.

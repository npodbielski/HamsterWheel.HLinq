# Methods in select - `select[R=reverse(x.Name)]`, `select[U=x.Name.ToUpper()]`

The roadmap line "methods in select" with the syntax rule it committed to: the value of a call
names the column it answers (`R=`), and an unnamed call names itself by the method
(`x.Name.ToUpper()` answers `ToUpper`) - the same way an unnamed property names itself by its
last path segment. The filter half already exists (`where[DateDiffDay(x.From, x.To) > 7]`, the db
function packs); this plan gives `select` - root and nested - the same call machinery.

## What was measured

Against the shipped pipeline (tokenizer -> parser -> builder -> applier, dummies in memory, EF
9 packs):

| query | today |
| --- | --- |
| `select[x.Name.ToUpper()]` | tokenizes, parses; **builder refuses**: `'ToUpper' is not an aggregate over a group` |
| `select[R=x.Name.ToUpper()]` | same refusal - the Call branch of `PropertyAssignment` routes every non-entity call through the aggregate converter |
| `select[R=x.Name.Substring(0,2)]` | same refusal (arguments survive to the builder) |
| `select[R=x.Name.Contains(abc)]` | same refusal |
| `select[R=reverse(x.Name)]`, `select[Reverse(x.Name)]` | **tokenizer refuses**: `MethodName` may follow only `Dot`, `LeftSquareBracket`, a comparison token - not `Assignment` |
| `select[R=abc]`, `select[R=7]` | constants - parse and answer today; must stay constants |
| `groupBy[x.Name].select[C=x.Count()]`, `select[N=x.Children.Count()]` | work today (aggregate paths); must not regress |
| `select[A=x.Children.select(a.Name)]` | works |
| `select[A=x.Children.select(City=x.Name)]` | works (an inner rename reads the outer row - README) |
| `select[A=x.Children.select(C=x.Children.Count())]` | works - the inner assignment counts the **outer** row's collection (two inner rows, each answering the outer `x.Children.Count()`): a call mixed across the scopes works today through the aggregate path |
| `select[A=x.Children.select(a.Name.ToUpper())]` | parses; builder refuses with a *stale shape* error (`InvalidPropertyPath 'ToUpper' for 'DynamicAnonymousType0`1'`) - the Call branch of the inner assignment is reached before any call machinery |
| `select[A=x.Children.select(City=x.Name.ToUpper())]` | parses; same `NotAnAggregateException` as the root select |
| `select[A=x.Children.select(City=upper(x.Name))]` | tokenizer refuses, same as the root select's static call |
| `where[Reverse(x.Name)==abc]` | parses (MethodName after `[`); builder: `InvalidStaticMethodException` - *no statics are registered* on a bare engine; the packs add them |

The grammar question - can a call after `=` tokenize without the tokenizer knowing anything about
calls - was measured with a one-word change (`MethodName` also after `Assignment`):

- `select[R=reverse(x.Name)]` -> `... Assignment MethodName ( Entity . PropertyName ) ...`
- `select[R=x.Name.ToUpper()]` unchanged (after `=` the window starts with `x`; only `Entity`
  completes there)
- `select[R=abc]`, `select[R=7]` unchanged - the race is lexical, not ordered: in the window
  `reverse(`, `NameOrValue` cannot complete at `(` while `MethodName` completes exactly at `(`,
  so the call wins; in `abc]` only `NameOrValue` ever completes
- every existing select/groupBy/nested-select query tokenizes identically

So the ambiguity between "a text value after `=`" and "a call after `=`" is resolved by the
tokens' own patterns - exactly how `10..20` vs `10.2` are kept apart - and the tokenizer special
cases nothing.

## The syntax

**A call is a value of an assignment or a bare member, in any select, root or nested, spelled the
same:**

- `select[U=x.Name.ToUpper()]` and `select[x.Name.ToUpper()]` - a call hanging on a member of the
  row (instance method of that member's CLR type).
- `select[R=reverse(x.Name)]` - a call standing on nothing: a *static* of the provider packs, the
  very same set a filter may call (an EF `DbFunctions` extension of the installed pack). A static
  over the pack is what makes `reverse` work at all; on a bare engine the refusal names the empty
  set, as in filters.
- Renames keep working and keep precedence: `select[Upper=x.Name.ToUpper()]` answers `Upper`.
- **Unnamed calls name themselves by the method name** - decided. `x.Name.ToUpper()` answers
  `ToUpper`, `reverse(x.Name)` answers `reverse`. This is already what the parser produces today
  for the property-call shape (`Method` is a child of the assignment, the last segment names it);
  it costs nothing and matches how a bare property names itself. A property call that parses today
  (`select[x.Name.ToUpper()]`) therefore keeps its shape and only gains a builder.
- Inside a nested select the spelling is identical, with the inner rename as in the root select:
  `select[A=x.Addresses.select(City=x.City.ToUpper())]`. What the call reads is decided by whose
  name the call hangs on - the inner row's member, or the outer row's (`City=x.Name.ToUpper()`),
  the existing documented rule for inner renames of properties (README: `contact=x.Name` reads
  the person, `a.City` the address), and a rule calls already follow on both sides of the scope
  line (`x.Children.Count()` in an inner assignment counts the outer collection - measured, and
  `a.City` reads the inner row): a call is mixed exactly the way a property is - the source
  resolves in whichever scope its name hangs on.

## Design

Everything a call in select needs already exists in the filter path; the plan wires the select
path to it, in the same order the filter uses.

### Grammar (one matcher, no new token)

`Grammar`: `MethodName`'s rule gains `or Assignment` to its previous-token matchers. Checked
against the grammar validator's three questions - one rule per token (unchanged), reachability
(new adjacency, still reachable), keyword swallowing (`NameOrValue` has no keyword - free text -
so nothing competes with `MethodName`; and the change touches adjacency, not patterns).
`ValidateLanguages` runs the existing grammar tests unchanged.

### Parser

`Method.Parser.BuildBranch` accepts a bare `[MethodName, LeftCircleBracket, ..]` under a
`PropertyAssignment` as well (the static-call shape it already builds under `Condition`); the
existing `[Dot, MethodName, LeftCircleBracket]` (property call, root and nested) parses today and
needs nothing. The aggregate shapes (`x.Count()`, `x.Children.Count()`) parse through the same
children and are distinguished later by `Method.IsEntity` - the parser says *there is a call*, not
which kind of call it is.

### Builder - the one real change

`PropertyAssignment.MemberBindingConverter` currently routes the whole Call branch through
`aggregates.BindOverMember` / `aggregates.ReturnType`. After a grouping that is right for the
entity aggregate (`C=x.Count()`); it is wrong for everything else. The branch gains the filter's
own order:

1. `Method.IsEntity` (the call hangs on `x` itself) - aggregate over the group, unchanged.
2. a call hanging on a member - the aggregates converter gets first refusal (this is what makes
   `x.Children.Count()` a *count* and not a `List`), then the instance-method machinery the filter
   uses: `ResolveMethod` ranking over the member's CLR type, `ParametersConverter` for written
   arguments, culture, and the same refusal naming the available signatures. `Condition` drives
   this through `MethodCallConditionBuilderContext` whose `MethodSource` the property converter
   fills (`Condition` does exactly that today); the select builds the same context with the member
   `CallSource` already resolves.
3. a static call - the packs' set through `ResolveStaticMethod`, i.e. `BuildStatic` as filters
   use it: EF `DbFunctions` extensions of installed packs, ranked, argument-matched, refused the
   same way when nothing fits.

`MethodCallType` (the `GetPropInfo` half, which decides the dynamic type's property) mirrors the
same order with the resolved `MethodInfo.ReturnType`.

Because `MemberBindingConverter` is the converter of `PropertyAssignment` wherever a
`PropertyAssignment` stands, root and nested select share the whole change; the nested failure
today (`InvalidPropertyPath 'ToUpper'`) is the same branch reached with no call machinery behind
it. That is what "root and nested behave the same" means concretely here - not a rule to enforce,
one code path to reuse.

### What a call may be - and what it may not

- The set of callable names is *not* a list in HLinq: instance methods are the CLR methods of the
  member's own type (same de-facto whitelist as filters), statics are the installed packs' EF
  functions. A call that resolves but EF cannot translate fails at the database exactly like the
  same call written by hand in Linq - the documented pack policy.
- A call answering a *collection* in a value position is refused the way filters refuse it ("a
  result nothing of a query is"); `Distinct` and friends stay out (the linq-parity plan names
  them).
- Aggregates written *before* a grouping (`where[x.Count()>2]` refusal family) keep refusing;
  the first-refusal order above is what distinguishes `x.Children.Count()` (a navigation count -
  an aggregate over the collection) from `x.Name.Contains(abc)` (an instance method) at the same
  syntax.
- Written arguments keep the written-constants grammar (`Substring(0,2)`, culture of a `ToLower`
  compared later); no new argument syntax.

## Decided

- **Unnamed calls are legal and name themselves by the method** (`ToUpper`, `reverse`); a rename
  overrides. This is the behaviour the parser already produces for the shape that parses today;
  the roadmap line's "an unnamed call has no column to answer" says a *bare call standing alone*
  names itself, which is what an unnamed property already does.
- **Root and nested select behave the same** - one converter, so it is structural, and the nested
  spelling of a renamed inner call is the inner rename: `select(City=x.City.ToUpper())`, not
  `select(a.City=x.City.ToUpper())` (the latter reads as an inner rename of a *property* named
  `a.City`, which is not what a call is).
- **Statics are the packs** - a select may call what a filter may call, no wider. "the same set"
  is the rule; a select-only registry is not built.
- **The aggregate converter keeps first refusal over member calls** - the order filters already
  use; a `Count`/`Any`/`Max` written on a collection is its aggregate, not its instance method.
- **Mixed sources inside a nested select are consistent with properties**: a call may
  read the outer row (`City=x.Name.ToUpper()`), the same freedom an inner property rename has
  (`contact=x.Name`) and a nested aggregate already uses (`x.Children.Count()` in an inner
  assignment - measured answering the outer row); which row a call reads is which scope its name
  hangs on, one rule everywhere.

## Open decisions

1. **Where a call may stand beyond assignments** - a bare unnamed call among members
   (`select[x.Name.ToUpper(),x.Name]`) is covered above (named by method); a call as a *sort key*
   already works (`orderBy` today) and as a *group key* (`groupBy[x.Foo.Bar()]`) is not promised
   here.
2. **`MethodCallType` for `DynamicFactory`** - an aggregate today answers through `ReturnType`;
   instance/static must answer the resolved method's return type before the dynamic type is
   created - straight, but it is the point where a wrong type would surface as a member-binding
   error, so the first spike asserts the shapes end-to-end, not just that a call builds.

## Delivery steps

- **S1** - grammar (`MethodName` after `Assignment`) + parser (static call under
  `PropertyAssignment`), with tokenizer tests for the measured races (`R=abc` still a constant,
  `R=reverse(` a call, every existing select unchanged) and `GrammarValidator` green.
- **S2** - the builder: the Call branch order above (entity aggregate / member: aggregates then
  instance / static: packs), `MethodCallType` mirroring it; in-memory applier tests: rename wins,
  unnamed names by method, written arguments, culture, refusal messages naming the signatures.
- **S3** - the nested select: the same shapes through the inner scope (property-call, inner
  rename, static), the mixed-source shapes answering the outer row asserted beside the inner ones
  (an inner property rename and an outer aggregate already answer it today - the call must join
  them, not a new mechanism); provider packs (npgsql, sqlite,
  mysql) assert the translated SQL (`UPPER(`, `SUBSTRING`, the pack function's SQL) for
  `select[U=x.Name.ToUpper()]`-shaped queries through the pipeline, the group-aggregate tests as
  the pattern of stopping at `ToQueryString`.
- **S4** - README (Selecting gains calls with the naming rule, nested spelling with the inner
  rename), roadmap line ticked, Polish words reviewed (none expected - no new keyword), plan moved
  to done.

## Out of scope

- `property operation in select` (`FullName=x.Name+' '+x.Surname`) - the adjacent roadmap line,
  expression grammar of its own.
- Calls whose result is a collection in a value position, set operators, `Distinct` - the
  linq-parity inventory decides those.
- Filtering or paging inside a nested select's call (existing out-of-scope from the nested-selects
  plan).
- New pack functions - if `reverse` has no EF translation on an engine, that is the pack's list
  and upstream's, not this plan's.

## Definition of done (per AGENTS.md)

`dotnet build HLinq.slnx`; unit, netstandard-unit and integration suites green (integration against
postgres); `dotnet format HLinq.slnx --verify-no-changes`; the measured table is green as tests -
each today-refused query answers or refuses with its named reason; provider packs assert the
translated SQL for one call each; README documents the syntax and naming rule; branch pushed, MR
opened, pipeline watched to green, then stop and wait for review.

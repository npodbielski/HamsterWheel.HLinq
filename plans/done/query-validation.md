# Query validation - answers a query gets before it runs

Closes #2. A query is rejected root by root, at the first thing that is wrong with it, and what
it gets is a sentence. The checks exist - member resolution names what the entity offers,
`ExpressionBuilder` refuses a call whose arguments fit no overload, `FromStringConverter` refuses a
written value that is no value of the type it stands against - what is missing is a caller that
wants *all* its answers at once, and answers a machine can map back to the field of a form.

## What was measured

Every exception of a build, run through the real pipeline over an in-memory source (so the
appliers, the ranking and the converters all ran), with the exception each query answers:

| query | today |
| --- | --- |
| `where[x.Nope==1]` | `InvalidPropertyPathException` - names the path and every property of the entity |
| `where[x.Nested.Nope==1]` | same, naming the path as the query wrote it (`Nested.Nope`) and the properties of the *segment that failed* |
| `where[x.Name==x.Int]` | `ComparisonOperandTypesCannotBeComparedException` |
| `where[x.Short==x.Byte]`, `where[x.Int==x.NullableInt]`, `where[x.Decimal>x.Float]` | rejected, same exception |
| `where[x.Name==x.Name]`, `where[x.Enum==x.Enum]`, `where[x.Int==x.Int]` | accepted |
| `where[x.Name>x.Text]` | rejected - a string has no `>` |
| `where[x.DateOfBirth==1231]` | `InvalidPropertyPathException` (the member is checked before the value is) |
| `where[x.Name==12312]` | **accepted** - a number is a string's value read as `12312` |
| `take[-5]`, `skip[-1]` | `InvalidPagingValueException` |
| `take[abc]` | `InvalidConstantStringToTypeConversionException` |
| `where[x.Nope(x.AlsoNope)]` | `NotAnAggregateException` - names the aggregates a query may write |
| `where[x.Count()]` | `EntityAggregateNeedsGroupException` |
| `select[x.Name,x.Name]` | `MemberNameCollisionException`, while the select is parsed |
| `where[x.Nope==1&&x.Texto==2&&x.Into>3&&x.Namee==a]` | one error: the first condition - the other three are never reached |
| a query of 5 015 characters | tokenized and applied, no HLinq limit on length |
| `select` of 300 members (3 497 characters) | built and answered, no HLinq limit on the shape |

Two things the measurement settles for the design.

**Member against member is checked, by the expression layer.** `Expression.Equal/GreaterThan/...`
carry the operator table of the CLR, and the applier already turns their exception into a named one:
`==` between two strings builds, `>` between them does not, `==` between `Int16` and `Byte` does
not, `==` between `int` and `int?` does not (no lifted operator is inserted). So the residual is
not a new comparability check - it is that this exception, like every other, has to arrive as data
naming both sides. Widening what may be compared (nullable beside non-nullable, a narrower integral
beside a wider one) is conversion, not validation, and stays out.

**Nothing is computed twice by collecting.** Every one of the exceptions above is thrown while the
expressions the query needs are built - member ranking, conversion cost, the shape of a written
value included. A pass that recomputed them to answer "is this valid" would ask the same questions
again, so the errors are collected in the pass that already asks them.

## Design

### The bag sits where the answers are computed

`IHLinqDiagnostics` - errors and warnings in one bag, since a review said two bags for one pass
were one too many - sits on the applier's context
(`IQueryableContext`, `IBuilderContext`) and on the culture of the request, because what a name
means, whether a call's arguments fit and what a written text converts to are answered against a
CLR type and a culture - none of which the text of the query knows. The parsing context keeps
answering what the text says, as AGENTS.md rules.

The errors arrive as data:

```
HLinqError(HLinqErrorCode Code, string? RootKeyword, string Text, string Message,
           IReadOnlyList<string> Possibilities)
```

- `Code` - a closed enum, the vocabulary a caller branches on.
- `RootKeyword` - which root it stands in (`where`, `select`, `orderBy`, `groupBy`,
  `skip`, `take`, `count`), decided from the root's *token type*, so a query written in
  Polish says `where` and not `gdzie` - the kind is the contract, the keyword is a word of a
  language.
- `Text` - the offending text as the query wrote it (the path, the written value, the call, the
  two type names).
- `Message` - the sentence today's exception already says, kept for the human reading a log.
- `Possibilities` - what was possible there: the properties of the type, the signatures of the
  call, the aggregates a query may name, the operators a comparison had.

Each code comes from the exception family that throws it, and each family already *holds* the data
(`InvalidPropertyPathException` knows the path and the properties, `Method.InvalidMethodException`
knows the name, the arguments and the signatures) - so the exceptions get read-only properties for
what they already format into a message, and one place maps an exception to an error. The
vocabulary: `unknown-member`, `impossible-conversion`, `invalid-comparison`,
`constant-only-comparison`, `not-a-condition`, `invalid-method-parameters`, `not-an-aggregate`,
`aggregate-needs-group`, `invalid-paging-value`, `duplicate-member-name`, `root-not-allowed`,
`invalid-query`, `query-too-long`, `too-many-properties`, `unknown-culture` and
`refused` for the rest. Every one of them is thrown by something that exists today except
`query-too-long` and `too-many-properties`.

### One pass that reads every root, and an exception that carries the answers

The plan's two modes collapsed into one. The pass reads every root and collects what each of them
got wrong, always: a root whose expression cannot be built is **abandoned** - the error is reported
and the pass goes to the next root. An invalid root never produces a partial expression: there is
no predicate that matches nothing and no shape with a hole in it, because the expression of an
abandoned root is never built and never handed to anything. Inside `where` the same happens one
level down, at the conditions joined by `&&`/`||`, because that is where a query with several typos
actually writes them: each condition is built or not, and a `where` with one invalid condition is
rejected as a whole.

What a caller does with the answers is one decision, not a mode. A query that runs throws: it
throws the *first* exception, so a caller catching `InvalidPropertyPathException` catches the same type
it always did, and that exception carries the whole list on it. Reading every root costs a query
what it costs whether it is right or wrong, and a query that is wrong is not going to run at all - so
the caller that fails pays once for everything that can be fixed, and a query that is right pays
nothing beyond a list that stays empty. Measured over an in-memory source: a good query enumerates its
rows once and an abandoned one zero times, because the pass builds expressions and the enumeration is
what runs them.

### `Validate` builds and does not execute

`HLinqQuery<T>.Validate(bag, queryString)` is the round trip a client of any language calls with a
query text, and `Validate()` on an already-bound query answers its build alone. Both return the
errors and touch **no queryable**: what HLinq can answer about a query is answered by building
it - the tokenizer, the parser, and then the expression of every root - and the database is
asked nothing, so validating costs a build and cannot run a statement by accident. `ApplyTo`
keeps throwing for what the tree cannot build, which is what it already guarantees.

What the tokenizer and the parser throw is an answer too, so a text that never becomes a tree
(`where[1==]`, `take[10 20]`, a root no parser claims) is reported as `invalid-query`
with the text that stopped the parse, in the same list as everything else - a caller that asks to
be told what is wrong hears it once, whether it broke the grammar, the tree or an expression.
A text that does not parse leaves no tree to build roots from, so that error is the only one the
call gets, and it is the exception the caller sees today.

The expression of a root is what each root applier already builds, and an interface that asked
each applier for it again would be a second place that says what a root builds. The seam is one
step, shared by both passes:

```
public static IQueryableContext NextByCalling(this IQueryableContext context, Type currentResultType,
    Func<IQueryable?, IQueryable?> call, int? count = null, GroupingState? grouping = null)
```

Every applier already ended its step by handing a context on; it hands it through this now, and the
step makes no `Queryable` call when the context is a building one. The result type a root leaves -
which is what lets the next root be built against the right subject, a `select` after a `groupBy`
against the group and not against the row - is computed by the applier that already computed it, so
the two passes cannot drift apart on what a root builds. `skip`, `take` and `count` build no
expression; their check - a bound that is no number, a bound below zero - is made before the step
either way, and it sits beside the invoke they skip.

`count[]` is deliberately not run: over an empty `T[]` it would "succeed" while answering nothing,
which is the answer-by-emptiness the issue rejects, and there is no question in `count[]` to ask.

### The shape caps are options

`IHLinqOptions` already decides what a query may cost the host, and the paging applier reads its
cap from there, so the caps on a query's own shape belong beside it:

- `MaximumQueryLength` - default `4096`, the size Kestrel refuses by default, so HLinq refuses at
  the size a host that raised its own limit stopped answering: before the tokenizer walks it,
  because the tokenizer is a window walk over every candidate word and a long text is its cost.
- `MaxSelectMembers` - default `100`, generous beside any row a UI draws, the answer to the
  measured select of three hundred members, and the cap the wildcard plan asks for. It is read
  where the members are built - `select`, `groupBy` and a nested `select` - so it guards the
  shape both when a query is applied and when it is validated.

Both are host-wide, because the options are one host's; a cap that differs per endpoint is the
host's own check.

### What the host answers

`HLinqQueryExceptionHandler` keeps `Title` as the sentence of the first exception (nothing that reads it
today breaks) and publishes every answer under `ProblemDetails.Extensions["errors"]`. The key is
always there: when nothing of the text became a root the exception of the text is the one entry, so a
client never reads an absent list as "nothing wrong". `ProblemDetails` is the response shape, the
entry is the contract.

## Out of scope

- **Whether the engine would take the query.** Over an in-memory source every instance method
  executes, so nothing here refuses what EF refuses. The engine's verdict stays
  `ToQueryString()` at the host and what the packs' surface probe measures per engine.
- **Widening what may be compared** (`x.Int == x.NullableInt`, `x.Short == x.Byte`) - conversion,
  decided with the ranking's cost tiers, not validation.
- **Compiling or caching a query** so a repeated one costs less - a different question, and it
  would be measured first.
- Caps on nesting depth (a nested select inside a nested select) and on the number of conditions;
  nested depth is the provider's decision, already.

## Delivery steps

- **S1** - `IHLinqError`/`HLinqErrorCode` in the abstractions beside the warnings,
  carried on `IQueryableContext` and `IBuilderContext` as the warnings are; the exception
  families gain properties for the data they already format; one mapper from an exception to an
  error, with the root read from the root's token type. Tests: every row of the table
  above produces the code, the text and the possibilities it says.
- **S2** - `IRootSelectorBuilder` implemented by the appliers that build a selector, sharing the
  call they already make; the build-only pass on `IHLinqQueryApplier`;
  `HLinqQuery<T>.Validate(...)`, static over the text and instance over a bound query. Tests: a
  query with four typos in four roots answers four errors in one call, a good query answers
  none, `ApplyTo` still throws the first error and reaches no data, and a query that needs a
  grouping validates the roots after it against the group.
- **S3** - collecting inside `where` at its conditions, so `where[x.Nope==1&&x.Texto==2]` answers
  both; an invalid condition abandons the root and nothing of it is built.
- **S4** - `MaxQueryCharacters` and `MaxSelectMembers` on `IHLinqOptions`,
  `HLinqOptionsConfiguration` and the AspNet wiring; checked before the query is read; tests for
  both caps and for a host that raises them.
- **S5** - the handler emits the errors as data; README: the section for validation (what
  `Validate` answers, the codes, the caps, what it cannot answer - the engine), the roadmap line
  ticked, and the plan moved to `plans/done/`.

## Definition of done

`dotnet build HLinq.slnx`; unit, netstandard-unit and integration suites green; `dotnet format
HLinq.slnx --verify-no-changes`; `jb inspectcode` clean at warning level in the changed files;
a query with several typos answered in one call, every error naming its code, root, text and
possibilities; both caps enforced and configurable; branch pushed, MR against `alpha`, pipeline
watched green, then stop and wait for review.

## What shipped - the plan reviewed against the implementation

- **The shape of an answer held, with one field less.** `HLinqError` has no `Query`: a caller that
  asks about a text already holds the text, and neither the HTTP response nor `Validate` ever had
  an entry whose query was in doubt. `RootKeyword` is null where nothing of the text ever became a
  root - a text that does not parse, a `culture=` this runtime knows - which is exactly the
  cases where naming one would have been invented.
- **One bag, not two.** The plan's `IHLinqErrors` and the `IHLinqWarnings` it joined are one
  `IHLinqDiagnostics`: both are answered while the query is turned into expressions, both travel on
  the same context, and threading two of them made every builder signature say the same thing twice.
  A merged bag also says what a bag is - a pass that collects - so a caller that hands the builder a
  bag to hear its warnings is a caller whose exceptions are collected too, which is the honest reading
  and not a surprise.
- **No `IRootSelectorBuilder`.** A second interface answering "the expression of this root"
  would be a second place saying what a root builds, and it would have to be implemented by
  every applier that builds one. What the build-only pass needs is smaller: a step that hands on
  the shape the `Queryable` call would have left without making the call, which is
  `QueryableContextExtensions.NextByCalling` - one place, used by both passes, so they cannot
  drift.
- **Both limits reject the query before it is read, and answer alone.** `MaximumQueryLength` is
  checked in `HLinqQuery<T>.Parse` before the tokenizer is handed the text;
  `MaximumNumberOfProperties` is checked by `ValidateNumberOfProperties` before a root is built -
  the widest member list of a root, a nested `select`'s list counted as a list of its own since
  those members are columns of the answer too. Neither is an answer about a root: the error names no root and no other
  answer comes with it, because a query the host will not run at all is rejected on its size and
  nothing of it is attempted. The plan read them where the members are built, which would have
  made a width error one answer among several and applied to only one of the two paths.
- **`invalid-query` names its text as the slice that stopped the parse** (`10 20]`, not the
  whole query), which is what a client marks in an editor; where nothing was tokenized at all the
  whole text is the slice, because that is what failed.
- **Comparability came for free, as the measurement said it would.** `invalid-comparison`
  is the exception `Expression.*` already threw - `String vs Int32`, `Int16 vs Byte`, a `>` between
  two strings - arriving now with both type names in `Text`.
- **What stayed is what the plan said stays**: whether the engine translates a call, and the
  comparisons a widening would let through.

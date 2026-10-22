# Plan: Member resolution (a query that matches two members picks one, and says which)

Designs #9 (ambiguous property / method names). Follows from the root-aware grammar plan:
`plans/done/root-aware-grammar.md` made the grammar say what a character means; this makes the
reflection layer say which member a name means.

Implemented in `alpha` - the ranking as !49, the report as !52, and the client repeating what a response
warned about as !54. The plan closes with its release step undated rather than unfinished: when `master`
moves is not a design decision.

## Goal

A query like `where[x.Name==abc]` reaches a .NET member by name, case-insensitively, and a type can
have two members that one query text matches (`Name` and `name`, `Get` and `get`, several overloads of
`Sub`). Today that choice is made by whatever order reflection happens to return, silently. The goal
is a **documented, deterministic** choice that prefers the member the caller most likely meant (the
one needing the least conversions, the one declared on the entity rather than inherited), and a
**report** of the choice when no rule decided it — never an error, and never a new syntax the user has
to learn to disambiguate.

## Baseline knowledge (what the code does today)

Properties (`src/HamsterWheel.HLinq/Reflection/PropertiesCache.cs`):

```csharp
public PropertyInfo? Single(Type type, string prop) =>
    From(type).FirstOrDefault(p => p.Name.Equals(prop, StringComparison.InvariantCultureIgnoreCase));
```

one candidate, in `Type.GetProperties` order; `ExpressionBuilder.GetProperty` (property path of
`where` / `orderBy`) and `PropertyAssignment` (the rename target of `select`) both call it. Two
properties differing in case resolve to whichever reflection lists first, on every platform, forever.

Methods (`ExpressionBuilder.GetMostProbableMethods` → `FilterMethodsViaParameterValues` →
`PropertyMethodToExpressionConverter`):

- candidates: exact-name matches, then case-insensitive ones, `Distinct()` — so both spellings are in
  the list, exact name first;
- a score is computed per candidate (base 100, 0 when a `char` parameter is handed something that is
  not one character, a bonus when the value names an enum member), candidates with score 0 dropped,
  the survivors ordered **ascending** by that score;
- the converter then walks the ordered list, calling `Expression.Call` on each inside a `catch
  (ArgumentException)` — **without breaking on the first success**, so the *last* bindable candidate
  wins, not the best-scored one. With one bindable overload the order does not matter, which is why
  nobody has noticed.

`MethodsCache.GetStatic` matches the name case-sensitively and takes `FirstOrDefault` /
`SingleOrDefault(selector)`; `SingleOrDefault` throws `InvalidOperationException` on a tie, which
reaches a request as a 500.

## Design

### 1. One resolver, one answer, with the reason

`IPropertiesCache.Single` and the method candidate list are replaced by one resolution step returning
a decision, not a member:

```csharp
MemberResolution<T> { T? Member, IReadOnlyList<T> Competitors, ResolutionReason Reason }
```

The caller builds the expression from `Member` exactly as today. Nothing about the query language
changes: no quoting, no case-sensitive escape hatch, no new token. `Competitors` is what a report is
built from, and `Reason` is the rule that decided (so a report can say why).

### 2. Ranking, most significant first

1. **Bindable at all** — parameters that the given arguments cannot bind (wrong count, no conversion
   `Expression` would perform) drop out. This is what `Expression.Call` would throw for anyway; doing
   it before building means the winner is chosen, not merely the last survivor of a try/catch loop.
2. **Cheapest conversion** — the candidate whose parameters the arguments reach with the fewest and
   lightest conversions: exact type, then an implicit conversion (`int` argument to a `long`
   parameter), then the string-to-enum/Guid/DateTime parse the converters already know how to do.
   `Expression.Call` does not lift user-defined or nullable-widening conversions, so "what LINQ can
   actually call" is the bound of the ranking, not an aspiration.
3. **Name spelled exactly** — `Name` beats `name` when the query wrote `Name`.
4. **Declared on the entity type** — a member the entity declares itself beats one it inherits, and
   `System.Object` members (`Equals`, `GetHashCode`, `ToString`) sit behind that, so `x.ToString()`
   still works while a real `Name` on the entity beats an inherited one.
5. **Then, and only then, deterministically** — parameter type names and the member name compared
   with `StringComparison.Ordinal`. This is a tie-break for genuinely equivalent members; its only
   job is to make the answer independent of `GetProperties` order and of the runtime.

The existing `probability` scores collapse into rules 1–2 rather than living beside them: they score
the same thing (can this be called, at what cost) with hand-tuned constants and an inverted order, and
one ranking is easier to reason about than two.

### 3. The report

Ambiguity is a *warning*, not an error, and never changes the response body — an API server that
started returning 400 for a query that works today would break clients that never asked for it.

**Only one outcome deserves a report: the one where the rules did not decide.**

| resolution | `Reason` | reported |
|---|---|---|
| one member answers the name (`name` → `Name` on a type with one `Name`) | `OnlyCandidate`, `OnlyBindableCandidate` | no — that is what a .NET API does with the case of a name, and a caller who spells a member differently did not do anything a warning should mention |
| the member the caller spelled | `SpelledExactlyAsWritten` | no |
| a rule chose (`Sub(string)` wins over `Sub(Status)` because `5` is text) | `CheapestConversion`, `DeclaredOnType` | no — the caller's own argument made it so |
| **two candidates, every rule equal, alphabetical order won** | `OrdinalTieBreak` | **yes** — the only answer a caller cannot predict from the query they wrote |

So the trigger is one condition, `Competitors.Count > 1 && Reason == OrdinalTieBreak`, and the kind of
member picks the token the caller can act on: `ambiguous-name` (rename a member or spell it exactly)
against `ambiguous-call` (write the argument so that it binds one overload).

**Carrier: `X-HLinq-Warning` header and an `ILogger` warning.** A header leaves the body byte-identical
for every client that exists today, shows up in curl, and is ignored by anyone who does not look; a
field in the JSON envelope would change the response shape of every endpoint.

```
X-HLinq-Warning: reason="ambiguous-name", member="Name", competitors="Name,NAME", type="DemoModule.Customers.Customer", query="where[x.name==Kowalski]"
X-HLinq-Warning: reason="ambiguous-call", member="Sub(String)", competitors="Sub(String),Sub(Status)", type="DemoModule.Customers.Ordered", query="where[x.Service.Sub(5)==1]"
```

- one header per event, and the header is a list (`a, b` parses as two), so events never share a line;
- every value a quoted string (a `"` inside one escaped `\"`) — `competitors` contains commas and a
  client that splits the list naively must not read one event as two;
- `member` and `competitors` name a member the way a caller reads one: `Name` for a property,
  `Sub(String)` for an overload — `MethodInfo.Name` alone cannot tell two overloads apart, and the
  whole point of the event is which of them the query got;
- `reason` is a closed token vocabulary: a client branches on the token, never on prose.
  `conversion` and `declared-on-type` are reserved for the rules that are silent today;
- `query` is the query string off the parse context, which is enough to point at the spot without
  threading a position through the tree — a `path=` field can be added later, nothing removed;
- a name that is not ASCII goes out in RFC 8187 extended notation (`member*=UTF-8''%C5%81ukasz`) and
  only then: a header value is ASCII, and raw UTF-8 in one is how a header arrives mangled;
- at most 5 events, the last header carrying `more="true"` — Kestrel caps the whole response header
  block and a `select` over forty ambiguous members must not turn a read into a failure at the first
  byte. The log is uncapped, which is what makes a cap here tolerable;
- the header is a hint, like `Deprecation`: no status code changes because of it, and a response served
  from a cache may arrive without it. HLinq does not know whether what it sits in front of is cacheable
  — so the fact has to be in the log as well, which is the other half of the carrier.

### 4. Where it lives

- `IPropertiesCache.Single` → `Resolve(Type, string) -> MemberResolution<PropertyInfo>`; `Single`
  stays as a thin wrapper (`Resolve(...).Member`) so the property path code is untouched in this step.
- `FilterMethodsViaParameterValues` → the ranking of §2, returning the ordered candidates *with* their
  reason; `PropertyMethodToExpressionConverter` and `StaticMethodToExpressionConverter` take the first
  bindable one and **stop** (the loop that lets the last candidate win goes away).
- `MethodsCache.GetStatic` → same resolution, no `SingleOrDefault` throw.
- The report travels with the parse on `IBuilderContext`, which is built per parse and already carries
  the query string and the culture; the converters hold it, so the resolution sites can report without
  a new parameter anywhere. It cannot come from the container: `HLinqCore` registers the converters,
  the caches and the builder as singletons, so a per-request service is unreachable from them. Not the
  tokenizer either: resolution is an apply-time question, and the same tokenizer output stays valid
  with nothing listening.
- The parse then hands out what it collected: `HLinqQuery<T>` carries the warnings of its own parse, so
  whoever holds the query — a console app, a test, a background job, an endpoint writing its own
  response — reads the ties without a web layer, and the web layer writes the header from the same list.

## Steps

### S1 — Ranking, with the report collected but not emitted — merged into `alpha` (!49)

- `MemberResolution<T>` + `Resolve` on properties; methods ranked by §2; the try/catch loop stops at
  the first bindable candidate.
- Tests, all on types defined in the test project, one per ranking rule: `Name` vs `name`, a member
  shadowing an `Object` member, overloads `Sub(int)` / `Sub(long)` / `Sub(string)` against `x.Sub(5)`
  and `x.Sub(abc)`, and the enum/char cases the old score handled (they keep passing, unchanged).
- A test per carrier candidate proving the collector sees exactly the competing member names and the
  chosen one — moved to S2 with the collector: a resolution already carries what a report needs
  (`Competitors` and `Reason`), and nothing reports yet.
- What S1 found worth saying out loud: the parameters of a method call are filled from the text of a
  query by `ParametersConverter` alone (a text, a one-character `char`, an enum member), so "bindable
  at all" is a property of that converter and not of `Expression.Call` — a numeric parameter of a
  method is out of the ranking for the same reason it was out of the call.

### S2 — Report on the wire — merged into `alpha` (!52)

- A per-parse sink on `IBuilderContext` (`IHLinqWarnings?`, `null` meaning nothing is collected and the
  non-web caller pays one `if`), collected by the parse and exposed by `HLinqQuery<T>` as
  `query.Warnings.Reported` - a consumer reads the ties it hit and writes them wherever it likes instead
  of only where hlinq happens to sit. The web carrier renders the header of an event, so the
  format of §3 lives in exactly one place.
- The resolution sites report through one shared helper holding §3's condition, so the property, method,
  static-method and member-assignment sites cannot diverge on what counts as worth saying.
- `HLinq.AspNet` writes the header from the binder: the binder holds the parsed query, the options and
  `HttpContext.Response` in one method, and model binding runs before an action writes a byte — no
  filter, no trailer. Gated by `IHLinqOptions.EmitWarnings` beside `HttpDefaultMaxTakeRecords`, so an app
  that does not want a warning on the wire gets nothing on the wire; the `ILogger` warning goes out either
  way, uncapped, as the record that outlives the response. `Response.HasStarted` downgrades a report to the
  log rather than throwing. Both entry points — the controller binder and `BindAsync` — or the style of
  endpoint decides whether a caller is told anything.
- Demo project: `/demo/sidekicks`, a type with `Name` and `NAME` for a query to be ambiguous about, and
  the header it answers in the README. The pair cannot live on `Superhero`: two members of one name are
  what `System.Text.Json` refuses over a record's own constructor parameter, so the demo's ambiguous type
  is a plain class and the second member is out of the serialized answer.
- Tests: unit on what reports and what does not (§3's table), on the rendering of a member and of a
  non-ASCII name; integration on the demo app for no header when the resolution is exact, the chosen
  member and its competitor when it is not, `more="true"` past the cap, and no header with the option
  off.
- #12, the client repeating what a response warned about, is built on this producer (!54): a warning of a
  response goes to the `ILogger` the caller hands `GetWithHLinq`, and to the error stream of the program
  that hands it none, and it is said before the status code is looked at - what a server said about a name
  may be why the query failed. The name of the header lives beside the warning it names, since the package
  writing it and the package reading it do not know each other.

### S3 — Release — a date, not a step

`VersionPrefix` is already `0.13.0` and `0.12.0` was never released, so releasing what `alpha` holds is
the `alpha` → `master` MR alone: no bump commit. That release carries the resolution rule (README states
it beside the case-insensitive member lookup, since queries that were accepted before can now resolve to
the other member) and S2's `IBuilderContext` member, pre-1.0 the minor being where a breaking change
goes.

It is left out of the plan rather than done: the plan was the rule and the report, both merged, and the
release waits for the maintainer to want one. Nothing here needs revisiting when it happens — the version
it lands in is whatever `Nuget.props` says that day, and the release note owes the reader the one fact
named under risks: which spelling wins, and why.

## Risks / open questions

- A query that today resolves to `name` may resolve to `Name` after S1. That is the feature, but it is
  a real behavior change: the README and the release note have to say which spelling wins and why, and
  the version bump is minor rather than patch.
- Rule 2 wants "can `Expression` convert this" as a predicate and `Expression.Call` has none — it throws
  `ArgumentException` for what it cannot do. Settled as `ImplicitlyConverts` (assignability, plus what
  `ParametersConverter` fills from the text of a query), with the try/catch of the converters kept as
  the safety net under it.
- Warnings on every ambiguous query can be spammy on a hot endpoint. The header is capped per response
  (§3) and the log is not: if the log turns out to be the noise, the once-per-(type, member) switch
  belongs to the sink, not to the resolver.
- Static methods and EF-bound methods (`ilike`) go through `MethodsCache`, whose name matching is
  case-sensitive today. Making it case-insensitive would widen what queries accept — separate,
  explicit decision, not part of S1.

## Definition of done (per AGENTS.md)

`dotnet build HLinq.slnx` (net10.0 + netstandard2.0 + demo) and unit, netstandard-unit and integration
suites all green locally; every ranking rule pinned by a test on a purpose-built type; the ambiguity
report covered on both carriers; README documents the rule; branch pushed, MR opened with `glab`,
pipeline watched to green, then stop and wait for review — never merge, never push `master`.

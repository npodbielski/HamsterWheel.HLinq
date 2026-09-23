# Plan: Member resolution (a query that matches two members picks one, and says which)

Designs #9 (ambiguous property / method names). Follows from the root-aware grammar plan:
`plans/root-aware-grammar.md` made the grammar say what a character means; this makes the
reflection layer say which member a name means.

## Goal

A query like `where[x.Name==abc]` reaches a .NET member by name, case-insensitively, and a type can
have two members that one query text matches (`Name` and `name`, `Get` and `get`, several overloads of
`Sub`). Today that choice is made by whatever order reflection happens to return, silently. The goal
is a **documented, deterministic** choice that prefers the member the caller most likely meant (the
one needing the least conversions, the one declared on the entity rather than inherited), and a
**report** of the choice — never an error, and never a new syntax the user has to learn to disambiguate.

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

| carrier | for | against |
|---|---|---|
| response header (`X-HLinq-Warning: ambiguous member 'Name' on User, used 'name'`) | the body stays byte-identical for every current client; curl logs it; ignored by clients that do not care | invisible unless you look; header size |
| field in the JSON envelope | visible in the payload, easy to test | changes the response shape of every endpoint, needs a client contract change |
| `ILogger` from the applier | no API change, aggregatable, no cost per request when disabled | the operator sees it, the caller does not |

Recommendation: **header + `ILogger`**, behind one `IHLinqDiagnostics`-shaped collector that the
applier fills while it resolves members and the AspNet layer drains into headers. The collector is
optional (an app that passes nothing pays nothing) and the memory/db demo endpoints get the header for
free, which is how the behavior gets tested. The JSON field stays out until someone actually wants it.

### 4. Where it lives

- `IPropertiesCache.Single` → `Resolve(Type, string) -> MemberResolution<PropertyInfo>`; `Single`
  stays as a thin wrapper (`Resolve(...).Member`) so the property path code is untouched in this step.
- `FilterMethodsViaParameterValues` → the ranking of §2, returning the ordered candidates *with* their
  reason; `PropertyMethodToExpressionConverter` and `StaticMethodToExpressionConverter` take the first
  bindable one and **stop** (the loop that lets the last candidate win goes away).
- `MethodsCache.GetStatic` → same resolution, no `SingleOrDefault` throw.
- The collector is threaded through the applier context (which already carries the query and the
  options), not through the tokenizer: resolution is an apply-time question, and the same tokenizer
  output stays valid without any diagnostics sink.

## Steps

### S1 — Ranking, with the report collected but not emitted

- `MemberResolution<T>` + `Resolve` on properties; methods ranked by §2; the try/catch loop stops at
  the first bindable candidate.
- Tests, all on types defined in the test project, one per ranking rule: `Name` vs `name`, a member
  shadowing an `Object` member, overloads `Sub(int)` / `Sub(long)` / `Sub(string)` against `x.Sub(5)`
  and `x.Sub(abc)`, and the enum/char cases the old score handled (they keep passing, unchanged).
- A test per carrier candidate proving the collector sees exactly the competing member names and the
  chosen one.

### S2 — Report on the wire

- Optional diagnostics collector: applier fills it, `HLinq.AspNet` writes `X-HLinq-Warning` headers
  and logs; integration tests on the demo app assert a query that is *not* ambiguous produces no
  header, and one that is produces the chosen member name.

### S3 — Release commit

`chore: bump version to x.y.z` in `Nuget.props` (behavior of previously accepted queries can change
where two members competed — the point of the step; documented in README next to the case-insensitive
member lookup).

## Risks / open questions

- A query that today resolves to `name` may resolve to `Name` after S1. That is the feature, but it is
  a real behavior change: the README and the release note have to say which spelling wins and why, and
  the version bump is minor rather than patch.
- Rule 2 needs "can `Expression` convert this" as a predicate. `Expression.Call` throws
  `ArgumentException` for what it cannot do, so the cheap version of the predicate is to try binding
  and catch — correct by construction, slower, and the resolution is cached per (type, name, argument
  shapes). Try the predicate first (`ImplicitlyConverts` via `Type.IsAssignableTo` and the converters
  the library already has) and keep the try/catch only as the safety net.
- Warnings on every ambiguous query can be spammy on a hot endpoint. The collector is per-request and
  the header is one string; if it turns out to matter, the collector is the natural place for a
  once-per-(type, member) switch, not the resolver.
- Static methods and EF-bound methods (`ilike`) go through `MethodsCache`, whose name matching is
  case-sensitive today. Making it case-insensitive would widen what queries accept — separate,
  explicit decision, not part of S1.

## Definition of done (per AGENTS.md)

`dotnet build HLinq.slnx` (net10.0 + netstandard2.0 + demo) and unit, netstandard-unit and integration
suites all green locally; every ranking rule pinned by a test on a purpose-built type; the ambiguity
report covered on both carriers; README documents the rule; branch pushed, MR opened with `glab`,
pipeline watched to green, then stop and wait for review — never merge, never push `master`.

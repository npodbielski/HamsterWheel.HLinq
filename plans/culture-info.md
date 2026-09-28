# Plan: Culture info (what culture is a query written in?)

Addresses #5. Shares the knob it ends up with with #7 (date and time constants) and depends on
the shape of `plans/done/root-aware-grammar.md`, which made the grammar say in which *clause* a
token stands; this plan asks what *text* a value may hold.

## Goal

Decide once, in one place, what culture a query is written in - and stop every other layer from
guessing it. The tokenizer keeps its shape: one forward window walk, no backtracking, no peeking
at syntax, ambiguity resolved by the tokens' own `TokenPattern`s (AGENTS.md). What is on the table
is which characters a value may hold and which keyword separates a list, never new machinery.

## Baseline knowledge (measured on 0.11.0)

| query | what the pipeline does today |
|---|---|
| `where[x.Int>2.5]` | `NameOrValue:'2.5'` - a single dot survives inside a value |
| `where[x.Price>1,2]` | `NameOrValue:'1'`, `Comma`, `NameOrValue:'2'` - the value is cut in half |
| `where[x.Int.Between(1,2)]` | `NameOrValue:'1'`, `Comma`, `NameOrValue:'2'` - two arguments, always |
| `where[Name=="a,b"]` | `NameOrValue:'"a'`, `Comma`, `NameOrValue:'b"'`, `InvalidTokenCollectionException ... at character 12` - **quotes protect nothing** |
| `where[Name==1,1]` | same cut, `InvalidTokenCollectionException ... at character 12` |
| `select[FullName=x.Name,x.Int]` | `Comma` separates a select list too, not only method arguments |
| `decimal.TryParse("1,2", NumberStyles.Any, CultureInfo.InvariantCulture)` | **`true`, `12`** |

The three facts behind all of them:

- **A comma after a value is always a delimiter.** Delimiters are derived from adjacency, and
  `new GrammarRule<Comma>(false, [pt => pt is [.., NameOrValue or PropertyName]])` accepts a comma
  after a value in every clause and at every bracket depth. `NameOrValuePattern` holds any text but
  `..` and ends on the first delimiter it sees.
- **A quote is not a token.** *(baseline; fixed in 0.11.1, see S2)* `"` and `'` ride inside `NameOrValue` and are removed later by
  `FromStringConverter` / `PropertyAssignment` (`IsDoubleQuoted`, `UnQuote`). Nothing during the walk
  knows a quote is open, so any grammar character inside a quoted string ends the value. The comma
  is merely the character users hit first - `[`, `]`, `(`, `)`, `==` and `..` inside a quoted value
  have the same effect.
- **The conversion layer is invariant-culture throughout, and sloppier than it looks.**
  `FromStringConverter` parses numbers with `NumberStyles.Any`, which includes `AllowThousands`, and
  the invariant group separator is `,` - so `"1,2"` is twelve. It is unreachable today only because
  the tokenizer never lets a comma through to it. `IValueConverter` is public API, so it is
  reachable for anyone calling it directly.

## Why the tokenizer cannot be the one that decides

`where[x.Int.Between(1,2)]` means *two* `int` arguments to one reader and *one* `decimal` argument
`1.2` to another; both readings are grammatical, and the difference is not in the text. It is in
the method signature, which the walk does not have - the walk has a window and a set of
possibilities, and the first grammar that completes wins. Any rule that picked a reading there
would be a score, an ordering of possibilities or a look ahead at syntax: the three things the
design forbids (AGENTS.md), each of which was measured to be slower before it was rejected.

So the split is: **the tokenizer decides what a value is made of, the parser and converter decide
what it means.** Today the tokenizer decides the question by force (it commits `Comma`, and the
"one decimal" reading never reaches the layers that could judge it). The plan gives those layers the
choice back by making the two readings *written differently*.

## Design

One decision holds the whole plan up: **a query is written in exactly one culture, that culture is
an input of the request that carries it, and the default is the invariant culture it is today.**

Decided 2026-09-24, and it is the simpler of the two designs that were on the table:

- **the grammar does not change with a culture.** `,` is the list separator in every culture. Where
  the text is ambiguous - a comma between two values - the user is told to quote it, which is what
  quoting is for.
- **quoting does not change interpretation.** It says *this text, verbatim, is one value*; how that
  text is then read depends on the culture of the request and on nothing else. `where[x.Double=="1,2"]`
  and `where[x.Double=1,2]` differ in whether the comma survives tokenizing, never in which culture
  reads the number.
- **the request decides, invariant is the fallback.** A request may name a culture; a request that
  names none is read invariant-culture, exactly as today.

Keeping the grammar culture-free is what makes this cheap: the tokenizer stays a singleton with one
text rule per token, no per-culture grammar is built or cached, and `Validate()` sees the grammar it
sees today. A per-culture tokenizer would have been a cache keyed by culture, a query whose meaning
depends on who asked, and a delimiter set to re-derive per culture.

### 1. Conversion stops guessing

`NumberStyles.Any` does not mean "any number", it means "any number including group separators",
which in invariant culture is how `1,2` becomes twelve. Numbers parse with explicit styles
(sign, decimal point of the configured culture, no thousands), so a value that cannot be that
number reports `InvalidConstantStringToTypeConversionException` - a 400 - instead of becoming a
different number. This is a fix independent of everything else in this plan: the silent
reinterpretation exists in the published package, whether or not the language ever grows a comma.

### 2. A quote is a lexical fact

`NameOrValuePattern` stops ending on a delimiter while it is inside a quoted run: a value that
begins with `"` runs to the matching `"`, whatever it holds. `ValuePattern` is untouched - keyword
tails never contain quotes.

This belongs in a `TokenPattern` because it is exactly the kind of knowledge already there: the
same rule that says "a `NameOrValue` never holds `..`" says "a quoted run holds everything". It
changes no adjacency, so no delimiter set moves, `GetDelimiters` stays quote-blind and clause-blind
as AGENTS.md requires, and `Validate()` and the localized grammars are unaffected.

The tie-breakers, all three settled the way they were written here:

- `..` inside quotes: the quote wins, so `where[x.Note=="a..b"]` is a value comparison. A query that
  dies today starts filtering - that is the feature, but it is a behavior change and one corpus line.
- escapes: none in the first version. A `"` inside a value is written with the other quote
  (`'say "hi"'`). Escapes would need a second lexical rule and a story about `\` in Windows paths.
- an unterminated quote never completes, so it dies as `UnknownTokenException`. That is the honest
  message: a value that opens a quote and never closes it is not a token.

### 3. The culture of a request

A request carries its culture; nothing else does. Two ways to carry it, and both are the framework's
own rather than invented here - ASP.NET Core's localization reads `?culture=`, then a cookie, then
`X-Culture` / `X-UICulture` headers:

| carrier | verdict |
|---|---|
| `?culture=pl-PL&where[x.Double>"1,2"]` | **the one to build.** The meaning travels with the query: it survives a paste into a browser, a curl, a log line, a README example and a support ticket, and a cache key that includes the URL is already correct. It is also the key ASP.NET Core itself reads first. |
| `X-Culture: pl-PL` | later sugar, same precedence: a query string that says its culture wins over a header, so nothing that works by URL can be changed behind its back. |
| `Accept-Language` | rejected. Browsers send it unprompted, so queries would start being read in a culture nobody asked for, and every cached response would need a `Vary`. It describes the language of the *content returned*, not of the text being parsed. |
| `Content-Language` | the wrong header: it describes the audience of a representation, and a GET has none. |

Whatever carries it, the culture is scoped to **how HLinq reads its values**. It must not reach
`RequestLocalizationMiddleware` or the thread culture: that would move Entity Framework, model
binding and every `ToString()` in the application, which is a much larger change than this plan and
not what the request asked for. Cultures are validated against a configured allow-list (default:
none accepted, i.e. invariant only), so an unknown or unenabled culture is a 400 rather than a
silently different query.

What the culture is allowed to touch:

- `FromStringConverter` - the parse of query text into the target type. `FromConvertibleConverter`
  keeps `InvariantCulture`: that path converts values already in memory, never query text.
- nothing else. Not the grammar, not the delimiters, not the tokenizer, not the parser.

Two things must be true before a culture can be offered at all:

- **a quoted value must reach the type at all.** It does not today: quotes are stripped only in the
  `string` and `char` branches of `FromStringConverter`, so `where[x.Int=="5"]` answers
  `Constant value of '"5"' can not be converted to type 'System.Int32'` - measured, as are the same
  for `DateTime` and `Guid`. The escape hatch has to open for every type, which means stripping the
  quotes once, before the type-directed parse. It also gives `where[x.Date>="2020-01-01 00:00"]`.
- **step 1 must have happened.** With `NumberStyles.Any` and an invariant culture, unquoting `1,2`
  hands the parser the text that it reads as twelve. A quoted value that is not that culture's
  number must 400.

What is *not* changed, written down because it is the tempting thing to change: `where[Int.Between(1,2)]`
means two arguments in every culture, and `where[x.Price>1,2]` is not one value in any culture - the
comma is the list separator. A culture-enabled app writes the fractional value quoted
(`where[x.Price>"1,2"]`). One rule, no exceptions per clause.

## Behavior changes

| query | today | after 1 | after 2 | after 3 (`?culture=pl-PL`) |
|---|---|---|---|---|
| `where[x.Name=="a,b"]` | 400, cut at the comma | 400 | **shipped in 0.11.1** | compares `a,b` |
| `where[x.Double>"1,2"]` | 400, a quoted value is not a number | 400 | 400, quotes reach the type but `,` is not the separator | compares 1.2 |
| `where[x.Double>1,2]` | 400 | 400 | 400 | 400 - the comma is the list separator in every culture |
| `where[x.Int.Between(1,2)]` | two arguments | two arguments | two arguments | two arguments, unchanged |
| `where[x.Int>"5"]` | 400, `"5"` is not an `Int32` | 400 | works | works |
| `select[FullName=x.Name,x.Int]` | works | works | works | works |
| `where[x.Int>2.5]` | works | works | works | 400 in `pl`, not 25 |
| `?culture=pl-PL&where[..]` | 400 | filters, culture read and unused until 3 | filters | the clause read in `pl-PL` |
| `?where[..]&page=2` | 400 | filters - an endpoint's own parameter is not HLinq's and goes out in the binder | filters | filters |
| `?where[..]&take[5]` | 400 | 400 | 400 | 400 - a clause behind an `&` is refused, not dropped |
| `IValueConverter.ConvertTo("1,2", decimal)` | `12` | throws | throws | throws invariant, 1.2 in `pl` |

## Steps

### S0 - the culture parameter beside the clauses - !36

`?where[x.Int>1]&culture=pl-PL` had to stop being a 400 before `culture` could mean anything: the
binder handed the whole query string to the tokenizer, which knows HLinq and nothing else.

It is read in the binder - `HLinqQueryParameters` takes the parameters out of the request and hands the
query to the walk - because the query string is an unordered string to everything generic (RFC 3986
leaves the meaning of a query to the dereferencing algorithm, RFC 9110 leaves a cache free to compare
the whole url as a string), so the only way `culture` before the clauses and after them come to one
query is to read it before any of them is interpreted. It is also the reading S4 needs: a culture
selecting the keywords a language is written with has to be known before the first keyword is.

The tokenizer is not involved and pays nothing for the parameter: it knows none, HLinq's own included,
and `culture=pl-PL&where[..]` is not a query to it. `benchmarks/culture-parameter-2026-09-27.md` keeps
the measurements of the shape that was tried first - a keyword token the walk reads past is +1% to
+6.5% of every query, +24% once a rule is wide enough to read a foreign `name=value`.

Which segments of a request are the query's is answered by the grammar rather than guessed from text: a
keyword of a clause root, or a bracket, brackets belonging to the query language. So
`?wher[x.Int>1]&page=2` stays an error instead of becoming a parameter quietly dropped and the whole
collection handed back, `.` stays the only clause separator, and a clause behind an `&` is refused
rather than dropped.

The value is the request's own text - `pl` as readily as `pl-PL` - the first of several copies, and the
keyword is replaceable per language by an `ICultureParameterKeyword` (`format=pl-PL`).
`HLinqQuery<T>.Culture` carries it until S4 reads it.

### S1 - conversion stops guessing

- explicit `NumberStyles` in `FromStringConverter` (no `AllowThousands`), the culture taken from one
  place instead of scattered `CultureInfo.InvariantCulture` literals.
- unit tests pinning the converter directly (`"1,2"` for `decimal`, `"1 000"` for `int`, invariant
  `"1.2"` still fine), corpus unchanged - the tokenizer already refuses the comma.
- README: state that query text is invariant culture, in the section that shows a `where` with a
  number.
- ships as a patch: no query that works today changes, one that never worked keeps not working but
  now says why.

### S2 - quoted values - done in 0.11.1 (!35)

A value that starts with `"` now runs to its closing `"`, in `NameOrValuePattern` - the design
above held: no adjacency change, no delimiter moved, `Validate()` and the localized grammars
never noticed. The measurement the step asked for is in `benchmarks/quoted-values-2026-09-23.md`
(everything inside the spread of the three runs, allocation byte-identical), the corpus took a
line per behavior change including the two that stay broken, and the converters needed no
change: `UnQuote` already strips only a matched pair, it was never what decided where a value
ends. What this did *not* settle: a quoted `"1,2"` is a string, still parsed invariant-culture -
that is S3 and S4, and deliberately not part of the bug fix.

- quote rule in `NameOrValuePattern`, corpus lines for `"a,b"`, `"x == y"`, `"a..b"`, an unterminated
  quote, and a quoted value in a `select` rename.
- the converters' `IsDoubleQuoted` / `UnQuote` reduced to stripping quotes off a token known to be
  quoted, never to finding where a value ends.
- benchmark before and after in `benchmarks/` - this touches the pattern that runs on every character
  of every value.
- README: quoting section.

### S3 - quoted values reach their type

- strip the quotes once, before the type-directed parse, for every type - not in the `string` and
  `char` branches of `FromStringConverter`, which is where they are stripped now.
- corpus untouched (tokenizing is done), applier and HTTP tests for `where[x.Int>"5"]`,
  `where[x.Date>="2020-01-01 00:00"]`, a quoted GUID, and a quoted value that is not a number in the
  culture of the request - a 400 naming the text and the type.
- ships as a patch alongside S1: `where[x.Int>"5"]` answers today, so nothing that worked changes.

### S4 - the culture of a request

- read the carrier (`culture` parameter first, header later), validate it against a configured
  allow-list, and carry it as far as the conversion of values - nothing else.
- the public surface question this cannot avoid: `IValueConverter.ConvertTo(object, Type)` has no
  culture and no place to put one. Either a context argument on the interface, or a second interface
  the converter opts into, or an explicitly scoped value passed to the conversion step. Pick one -
  it is the only genuinely breaking-shaped decision in this plan, and the release notes depend on it.
- tests: the same query text read in two cultures, an unknown culture 400ing, a culture not on the
  allow-list 400ing, and the invariant behavior of a request that names nothing byte-identical to
  today.
- client: `HamsterWheel.HLinq.Client` writes query text in code. Either it emits the culture it is
  told to, or it is documented as invariant-only and a test pins that no `culture` ever leaves it.
- README: a "culture of a query" section with the table above, and the quoting rule next to it.

### S5 - not here

#7 (date and time constants) writes its formats with this knob and owns its own grammar; #9 (member
resolution) is in `plans/member-resolution.md`.

## Risks / open questions

- The `1,2` to `12` behavior is a public-API behavior change for callers of `IValueConverter`. It is
  a bug fix - a query language should not turn a value into a different value - but it belongs in the
  release notes, not in a silent patch.
- Quoting changes what `NameOrValue` can hold, so every place that compares a token's raw text
  (`PropertyAssignment`, the `null` keyword, the `x.` alias handling) needs its corpus reviewed
  rather than its tests re-run.
- Delimiter derivation must stay clause-blind *and* quote-blind: `GetDelimiters` asks adjacency
  matchers against synthetic markers, quoting is not adjacency, and if a quote state ever leaked into
  a matcher the derived sets would shrink and floats, GUIDs and dates would break the way AGENTS.md
  warns. Worth an explicit test that the sets do not move in S2.
- A quoted value is read by a culture, so the same URL means two things on two servers with
  different allow-lists. That is why the allow-list defaults to empty: a culture is something an
  endpoint turns on, not something a client discovers works.
- `where[x.Price>1,2]` staying an error in a `pl` deployment is the cost of one comma with one
  meaning. It will read as an unforgiving choice to whoever writes it; the error message should name
  the quote as the way out.
- Does the client library need culture at all, or is "clients write invariant, servers may read a
  culture" enough? Open, and it decides how much of S3 is a client release.

## Definition of done (per AGENTS.md)

`dotnet build HLinq.slnx` (net10.0 + netstandard2.0 + demo), unit, netstandard-unit and integration
suites green locally, `dotnet format HLinq.slnx --verify-no-changes`, `Validate()` green for the
default grammar and the demo localizations, one corpus line per behavior change in the table, a
benchmark recorded for anything that touches a `TokenPattern`, one MR per step, pipeline watched to
green - then stop and wait for review.

# Plan: Culture info (what culture is a query written in?)

Addresses #5. Shares the knob it ends up with with #7 (date and time constants) and depends on
the shape of `plans/done/root-aware-grammar.md`, which made the grammar say in which *clause* a
token stands; this plan asks what *text* a value may hold and in what *words* the clauses are written.

## Goal

Decide once, in one place, what culture a query is written in - and stop every other layer from
guessing it. The tokenizer keeps its shape: one forward window walk, no backtracking, no peeking
at syntax, ambiguity resolved by the tokens' own `TokenPattern`s (AGENTS.md). What is on the table is
which characters a value may hold, which keyword separates a list, and - the one piece of machinery this
plan adds - which words a culture spells its clauses with.

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

- **the shape of the grammar does not change with a culture.** `,` is the list separator in every
  culture, `.` is the clause separator in every culture, and what a value may hold is one text rule in
  every culture. Where the text is ambiguous - a comma between two values - the user is told to quote it,
  which is what quoting is for.
- **quoting does not change interpretation.** It says *this text, verbatim, is one value*; how that
  text is then read depends on the culture of the request and on nothing else. `where[x.Double=="1,2"]`
  and `where[x.Double=1,2]` differ in whether the comma survives tokenizing, never in which culture
  reads the number.
- **the request decides, invariant is the fallback.** A request may name a culture; a request that
  names none is read invariant-culture, exactly as today.
- **which words the language is written with belongs to the culture.** Decided 2026-09-28: the culture of
  a request selects the keywords its clauses are spelled with, so an endpoint can be asked in the
  language it answers in (S5). This is the other axis from the one above - which words stand next to
  which, and what a value is made of, stay exactly as written here.

What stays culture-free is what makes the language cheap to reason about: one adjacency per token, so
one derived delimiter set, one text rule per token, one quote rule, and a `Validate()` checking one
structure. A culture moves none of it.

The keywords therefore cost what this plan used to rule out, and they buy a language: a keyword table per
enabled culture, the grammar and the walk built per culture and cached under it - bounded by how many
cultures have a table, not by how many requests arrive - and `Validate()` run per table when it is filled.
That is not a query whose meaning depends on who asked: the culture rides in the URL, so a cache key, a
log line, a curl and a paste carry the language next to the text, which is the property that made
`?culture=` the carrier in §3. What it does mean is that the same *clause* written in two vocabularies is
two texts, and S5 decides whether an endpoint reads both.

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
not what the request asked for. A culture is accepted when the runtime
knows it (`pl`, `pl-PL`, `de`) and refused with a 400 when it does not (`test` is a name, not a culture), so
a query is never read in a culture nobody named. There is no set of enabled cultures to configure: the
cultures the deployment has data for are the set, and a request naming one of them is answered in it.

What the culture is allowed to touch:

- `FromStringConverter` - the parse of query text into the target type. `FromConvertibleConverter`
  keeps `InvariantCulture`: that path converts values already in memory, never query text.
- which word a keyword token is (S5) - the vocabulary of the language, chosen once per request when the
  grammar of its culture is picked.
- nothing else. Not the adjacency, so no delimiter set moves; not a text rule, so no character starts or
  stops being part of a value; not `RequestLocalizationMiddleware` or the thread culture.

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
| `where[x.Decimal>2.5]` | works | works | works | 400 in `pl`, not 25 |
| `where[x.Int>2.5]` | 400 | 400 | 400 | 400 - no integer holds a fraction, in any culture |
| `?culture=pl-PL&where[..]` | 400 | filters, culture read and unused until 3 | filters | the clause read in `pl-PL` |
| `?where[..]&page=2` | 400 | filters - an endpoint's own parameter is not HLinq's and goes out in the binder | filters | filters |
| `?where[..]&take[5]` | 400 | 400 | 400 | 400 - a clause behind an `&` is refused, not dropped |
| `DefaultConverter.ConvertTo(typeof(decimal), "1,2")` | `12` | throws | throws | throws invariant, 1.2 in `pl` |
| `?culture=pl&gdzie[x.Int>1]` | 400 | 400 | 400 | 400 until 5; a clause in `pl` after it |
| `?culture=pl&where[x.Int>1]` | 400 | filters | filters | filters; after 5, whether it still does is the replace-or-extend decision of S5 |

## Steps

S0 through S5 are merged into `alpha`, which is the whole of what this plan designed. The release they
ride on is not a step of it - `0.13.0` carries them, the member-resolution rule and everything else
`alpha` holds whenever `master` is moved, and the version a step reached is named beside it below.

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

- the grammar of a number written out in `FromStringConverter` instead of inherited from
  `NumberStyles.Any`: white space around it, a sign on either side, the culture's decimal point and
  group separator, an exponent for the floating point types. What `Any` added on top - a parenthesised
  negative, a currency symbol - stopped being a number.
- `AllowThousands` stays, and that is a decision rather than an oversight: which character groups
  thousands and which one separates the fraction is the culture's to say, and S4 is the step that hands
  the converter a culture. Under the invariant culture `1,2` is twelve, under `pl-PL` it is one and a
  fraction; the converter must not pre-empt that.
- one culture for the whole converter instead of a literal per type, which also took the last culture
  that was nobody's decision: `byte` parsed with the culture of the machine the service ran on.
- unit tests pinning the converter directly, corpus unchanged (the tokenizer already refuses a comma
  outside quotes), and the README paragraph that shows a `where` with a number says what a number is.
- rides the patch release of S3: no query that answers today changes its answer; `(5)` and a currency
  symbol answer 400 instead of quietly being -5 and 5.

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

### S3 - quoted values reach their type - merged into `alpha` (!40)

- strip the quotes once, before the type-directed parse, for every type - not in the `string` and
  `char` branches of `FromStringConverter`, which is where they are stripped now.
- corpus untouched (tokenizing is done), applier and HTTP tests for `where[x.Int>"5"]`,
  `where[x.Date>="2020-01-01 00:00"]`, a quoted GUID, and a quoted value that is not a number in the
  culture of the request - a 400 naming the text and the type.
- ships as a patch alongside S1: `where[x.Int>"5"]` answers today, so nothing that worked changes.

### S4 - the culture of a request - in `alpha`, 0.13.0 (!44)

Shipped as decided, with the seams named as they now are:
`IConfigurableValueConverter.TryConvert(object?, Type, CultureInfo?)` answering `(bool success, object?
result)` carries the culture to every converter - one method per conversion, its culture **optional**, `null`
being the invariant culture, which is how `take` and `skip` call it; the applier resolves the name the request
gave once - the invariant culture when the request named none, `UnknownCultureException` when the runtime
knows no such culture, which is what makes `culture=test` an error and leaves every real culture open with
nothing to configure - and `IBuilderContext.Culture` carries the result to the three places that convert.
`where`, `select` and the `orderBy` family thread the culture; `take`, `skip` and `count` do not need it. The
client is documented as asking for no culture, with a test pinning that none leaves it.

An allow-list of enabled cultures was designed in and taken out again before the merge: it would have been a
limitation with no reason behind it. The runtime supports its cultures, a name it does not know is refused
anyway, and asking a library user to enumerate cultures for a feature the runtime already has is work nobody
should have to do to use that feature.

- read the carrier (`culture` parameter first, header later), accept only a culture the runtime knows, and
  carry it as far as the conversion of values - nothing else.
- the public surface question, decided: **the culture is a parameter of the conversion call**, and the
  conversion is one method rather than a probe and a call.
  `IConfigurableValueConverter` answers with one
  `(bool success, object? result) TryConvert(object? value, Type to, CultureInfo? culture = null)`:
  `(true, value)`, `(false, null)` for "not mine, ask the next converter", and a thrown
  `InvalidConstantStringToTypeConversionException` for "mine, and this value cannot be read into that type".
  Optional, and on the method that already converted: a second overload that only forwarded the invariant
  culture would double the seam for no second behaviour, and `null` saying "invariant" says everything that
  overload said. A static or ambient value is out - it hides
  which query a value was read for, and it is the thing this plan keeps calling a guess. A context object
  (`HLinqQueryContext` and friends) does not avoid the choice either: it still has to be passed, so it
  buys a type the user has to learn on top of the parameter it carries. A plain `CultureInfo` says what it
  needs and nothing else.
- what that costs, in the open: the interface is the extension seam a user's converter is written against,
  so every custom converter is recompiled against a changed signature - a minor bump (`0.13.0`), release
  notes naming the one line to change. Merging the pair into `TryConvert` was not only tidiness: the two
  methods of `FromFormattableConverter` disagreed about the condition (one asked `IConvertible`, the other
  `IFormattable`), the dispatcher believed the first, and so `DateTimeOffset` to `string` was silently the
  serializer's to write rather than this converter's - a contradiction only one method cannot keep. No default interface member to soften it: the libraries ship `netstandard2.0` too.
  The implementors inside this repo move together: `FromStringConverter`, `FromConvertibleConverter`,
  `FromFormattableConverter`, `ToInterfaceConverter`, `ViaSerializationConverter`,
  `CustomTypeCombinationsConverter`, `EnumValueConverter`, `BaseConfigurableValueConverter`, and
  `DefaultConverter` - which hands the culture to whichever converter it picked.
- where the value comes from: the query's carrier (S0) gives a name, one place turns it into a
  `CultureInfo` - the invariant one when the request named nothing, so a request that says nothing today
  keeps answering byte-identically - and `IBuilderContext` carries the resolved culture to the three
  places that convert: `ComparisonConst`, `PropertyAssignment`, `InitializerConstantValue`. Converters
  hold no culture: `FromStringConverter.QueryCulture` from S1 moves to the place that resolves the name,
  so a culture is always an argument and never something a converter reads.
- tests: the same query text read in two cultures, a name that is not a culture 400ing, and the invariant
  behavior of a request that names nothing byte-identical to today.
- client: `HamsterWheel.HLinq.Client` writes query text in code. Either it emits the culture it is
  told to, or it is documented as invariant-only and a test pins that no `culture` ever leaves it.
- README: a "culture of a query" section with the table above, and the quoting rule next to it.

### S5 - the keywords of the language - in `alpha`, 0.13.0 (!47)

A culture says two things: how a value is read, and what language the clauses are written in. The words
are a small closed set - `where`, `select`, `take`, `skip`, `count`, `orderBy`, `orderByDescending`,
`thenBy`, `thenByDescending` - and the plan's rule holds over them: they are *vocabulary*, chosen per
culture, while adjacency, delimiters and text rules stay as they are. A `pl` endpoint is then asked in
`pl`, with the words its own dictionary picks: `?culture=pl&gdzie[x.Int>1]`.

Two words are not in that set and each has its answer. The culture parameter's keyword cannot be
translated: the parameter has to be read before the culture is known, so one word per endpoint names it
and `ICultureParameterKeyword` is exactly the seam that keeps it culture-free - which is also why it is
not a keyword of the grammar (S0). The entity alias `x` stays `x` in every culture: one endpoint serves
customers and e-mails at once, and a query language that renamed `x` to `klient` for Polish would make
querying e-mails in the same API confusing. A descriptive alias is worth having - every character used
consistently inside one query being the alias of that query's root, so
`/addresses?where[a.primary].select[p.contacts].orderBy[c.name]` reads as what it filters - but it is a
feature of the query, chosen per query, not a word of a culture: it belongs in its own plan and does not
wait for S5.

What the step has to settle, worst cost first:

- **replace or extend the default keywords - extend.** A named culture adds its words to the invariant
  ones, and the invariant ones stay viable always: `?culture=pl&gdzie[x.Int>1]` and
  `?culture=pl&where[x.Int>1]` are both queries. Turning a vocabulary on breaks nothing, and an endpoint
  keeps answering every query it always answered. The price is the measured one - two vocabularies in one
  window, the walk paying per possibility, +1% to +6.5% of a query
  (`benchmarks/culture-parameter-2026-09-27.md`) - and one endpoint answering the same question in two
  languages, which is what a migrating client needs and a documentation sentence for everyone else.
  "Prefer the translated word" is what two tables do by themselves wherever their words differ, and the
  words always differ where it would matter: the walk scores and orders nothing, so a preference between two
  spellings of one clause could only ever mean the two never competed to begin with.
- **where the tables live.** One table per culture next to the tokens, with a possibility reading its
  keyword from the table of the request's culture instead of from a literal - one table per culture, not
  one token class per culture. `INullKeyword`, today a DI swap for one word that is not even a keyword
  token (`null` is matched inside `NameOrValue` and read by the converters), either becomes an entry of
  the same table or is written down as deliberately outside it.
- **per-culture instances, cached by culture.** `IGrammar`, the possibilities and the tokenizer are
  singletons today; they become built once per culture, filled lazily, bounded by how many cultures have a
table and not by traffic. `Validate()` runs per culture at fill - over the **merged** set of that culture's
words and the invariant ones, because under extend both are viable in the same window and a translated word
swallowing an invariant one is precisely the bug the check exists for - keeping its rules: no keyword
swallows a longer one, and no two differ only in case, keywords being matched case-insensitively
(`ORDERBY[X.Name]` works today).
- **the request filter has to speak every language the endpoint answers in.**
  `HLinqQueryParameters` decides which segments of a request are the query's from the keywords the
  grammar registers; with several tables it asks the union of the enabled ones, or a bare clause root in
  another language (`count` and friends, which carry no bracket) becomes a parameter quietly dropped and
  the whole collection handed back. The bracket rule catches the bracketed ones today, which is exactly
  the kind of half-protection this step must not lean on.
- **measure both movers:** the setup - a grammar and a `Validate()` per culture, once - and the walk per
  query, which should not move at all, the possibility count being the same with other words in it.
  Recorded in `benchmarks/` per AGENTS.md.
- **tests:** one clause in two cultures and neither reading the other's words, the corpus for the default
  table untouched (the corpus speaks the default culture), `Validate()` green per table, the filter
  keeping a clause in a foreign language behind an `&`, and a README section per enabled language next to
  the culture one.

#### How it is built (as it was built)

- **a word is data, a word with a text rule of its own is a class.**
  `AddTranslatedKeyword<Where>("pl", "gdzie")` registers a word; the possibility that reads it is
  `KeywordPossibility<Where>("gdzie", culture)`, which is the token's own possibility with the word and its
  culture passed in. A word needing a text rule of its own is a possibility class of the API's own, spelling
  its word and naming its culture, registered by `AddTokenPossibility<T>()` like any other possibility. A
  possibility naming no culture belongs to the invariant language, which is how a word is added for every
  request at once.
- **a possibility is per word, a language is a grammar, its rules and a walk.** A possibility says which
  token it spells, with which word, of which culture - and nothing about a language: adjacency, clause
  scope, whether a token may open a query and which characters end a free text token are answers about the
  words of a language, so they are asked of its grammar (`GetRuleFor(type)`), and the one thing a
  possibility builds per language is its own text rule. That is also what lets the same possibility stand
  in two vocabularies: `where[x.Int>1]` and `gdzie[x.Int>1 oraz x.Int<2]` need different answers about the
  character `o`, and the two grammars give them. The walk takes the culture as a parameter of a request
  because it walks one language, chosen before it starts.
- **the language is assembled while the API is composed.** `HLinqTokenizer` builds a grammar, a validated
  rule set and a walk per culture with words, over the possibilities the container built;
  `Tokenize(query, culture)` walks the nearest one. Nothing of it happens during a request, and the
  tokenizer is resolved as the API starts, so a vocabulary that does not make a valid grammar stops the API
  instead of answering 500 to the first query written in it. A culture with no words gets no language and no
  validation of its own - a request naming it is walked in the invariant language.
- **the check runs where the words are.** The question was whether `ConfigureHLinq` should validate the
  grammar of every registered culture; it does not, because there it has no container and no culture - only
  a possibility says which culture its word belongs to, and the languages are the possibilities' to declare.
  The AspNet package resolves the tokenizer from an `IStartupFilter`, which builds and checks every language
  as the API starts.
- **nearest language wins, nothing merges.** A request naming `pl-PL` is walked in the `pl-PL` language
  when there is one and in the `pl` language otherwise; `pl-PL` never inherits `pl`'s words on top of its
  own, because "the words of this API's language" has to be one list an author can read, and
  inheritance would make the answer depend on which other languages happen to be registered.
- **the delimiter rule went plural.** `GetDelimiters` derives the characters that end a free text token
  from the keywords of the tokens adjacency allows to follow, and under *extend* a token can have two of
  them (`&&` and `i`): both first characters are delimiters, or the value swallows the translated word.
  The same plural sets feed the character checks that compare a keyword against `==` and `[`, so the rule
  matchers of the grammar stayed `static` lambdas over data.
- **`INullKeyword` stays outside the words.** It is not a token: `null` is matched inside `NameOrValue`
  and read by a converter, so it has no grammar rule, no adjacency and no delimiter set, and putting it in
  a table of words would make the table a list of two unrelated things.
- **the invariant language costs nothing extra.** With no words registered - every API that does not opt
  in - the possibilities are the ones the container built, and the invariant language is one grammar, one
  validation and one walk over them, built while the API is composed: the same one build as before S5, plus
  the self-check the grammar already ran.
- **tokens are built by the walk, not by the possibility.** A token is its `Range` and nothing else, so the
  walk of a language makes the tokens of its query (`TokenBase.Create<T>(range)`, resolved once per token
  type) and a possibility never carries a way to instantiate one - which is one more question that was
  asked of thirty objects per language when there was one answer.

- **the words of a language travel as a package.** `HamsterWheel.HLinq.Lang.Pl` is the Polish table in one call,
  `c.AddPolishToHLinq()`, and the demo endpoint that had nine `AddTranslatedKeyword` lines is its first host.
  Two things the pack showed without the core having to change: a word is registered by a generic
  (`AddTranslatedKeyword<Where>`), so a language is a list of statements rather than data something could
  enumerate, count or copy - a second language is another package, not another row - and the extension
  configuration it registers into is an AspNet type, so a language speaks to an endpoint and not to
  `ExecuteHLinq`. Both are worth a mechanism only when a second language makes them a pattern.

### S6 - not here

#7 (date and time constants) writes its formats with this knob and owns its own grammar; #9 (member
resolution) is in `plans/done/member-resolution.md`. Two more left with this plan rather than in it:

- **the alias of the root entity** - a descriptive letter for the root, used consistently inside one query
  (`where[a.primary].select[p.contacts]`) - is a feature of the query, chosen per query and written in it,
  and waits for its own plan. Registering a word of a culture for the entity token (`towar`) is a different
  thing and works today.
- **an error that reads itself out of the grammar.** `InvalidTokenCollectionException` prints the tokens it
  expected from a hand-kept table of examples (`ExampleOfTokenValue`), so `where[x.Decimal>1,2]` is refused
  with "was expecting for example: ']'" where the grammar also knows the author could have gone `&&`, `||` -
  or quoted the value. Deriving that list from the grammar touches every message and every test naming one,
  so it is its own plan too.

## Risks / open questions

- The `1,2` to `12` behavior is a public-API behavior change for callers of `IValueConverter`. It is
  a bug fix - a query language should not turn a value into a different value - but it belongs in the
  release notes, not in a silent patch.
- Quoting changed what `NameOrValue` can hold, and the places comparing a token's raw text
  (`PropertyAssignment`, the `null` keyword, the `x.` alias handling) were reviewed against their corpus
  instead of having their tests re-run: that was S3.
- Delimiter derivation must stay clause-blind *and* quote-blind: `GetDelimiters` asks adjacency
  matchers against synthetic markers, quoting is not adjacency, and if a quote state ever leaked into
  a matcher the derived sets would shrink and floats, GUIDs and dates would break the way AGENTS.md
  warns. Worth an explicit test that the sets do not move in S2.
- A quoted value is read by a culture, so the same URL means two things to two requests: the one naming
  `pl-PL` and the one naming nothing. That is what naming a culture in the URL buys, and it is why a
  response cache key, a log line and a pasted link have to carry the parameter next to the query text.
- `where[x.Price>1,2]` staying an error in a `pl` deployment is the cost of one comma with one
  meaning. It will read as an unforgiving choice to whoever writes it; the error message should name
  the quote as the way out.
- The client library writes **invariant** values and names no culture - which is what it does today:
  `WhereRootBuilder` renders a constant through `DefaultConverter`, and that renders an `IConvertible` with
  `CultureInfo.InvariantCulture` and a `DateTime` with `"O"`. A culture given to the client - globally, in
  its settings, or per query - replaces the invariant one in a single move: the values are written in it
  *and* the request names it. It has to be one move, because the two agree or the query means something
  else: the text `1,2` is twelve where no culture is named and 1.2 in `pl`.
  What the client does *not* take from the machine it runs on: `CurrentUICulture` is the culture of
  resource strings, and `CurrentCulture` would make one builder answer differently on two desks.
  It does leave with the rest of S5 one question about its own: a client that writes query text in code
  writes it in a *language* too, so either the invariant keywords are pinned to it by test or the client
  carries the words it wrote its query with.
- S5 decided to **extend**, not replace: turning a culture on adds words and takes none away, so no query
  that answered stops answering. What it moves is the count of words the walk asks per query (measured in
  `benchmarks/translated-keywords-2026-10-06.md`) and the release notes, which have to say that a `pl`
  endpoint reads `gdzie` *and* `where`.
- The entity alias `x` is a fixed letter of the syntax, not a word of a language: one endpoint serves
  customers and e-mails at once, and an alias renamed per culture would make the two read differently. A
  descriptive alias is a feature of the *query*, chosen per query and written in the query, and it waits in
  its own plan. Registering `towar` as a word of the entity token is a different thing - a word of the
  language, which an API may do, and not the alias of the root.

## Definition of done (per AGENTS.md)

`dotnet build HLinq.slnx` (net10.0 + netstandard2.0 + demo), unit, netstandard-unit and integration
suites green locally, `dotnet format HLinq.slnx --verify-no-changes`, `Validate()` green for the
default grammar and the demo localizations, one corpus line per behavior change in the table, a
benchmark recorded for anything that touches a `TokenPattern`, one MR per step, pipeline watched to
green - then stop and wait for review.

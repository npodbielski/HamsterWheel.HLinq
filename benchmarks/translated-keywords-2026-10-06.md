# The words of a culture in the walk (2026-10-06)

A language is the words viable in one culture: the invariant words and the words an API registered for that
culture. The two vocabularies stand in one window, so a query of a language carries the candidates of both.
This is what that costs, and what it does not cost.

`ShortRun`, `dotnet run -c Release --project test/HamsterWheel.HLinq.Benchmarks -- --filter '*Language*'`,
one session, `test/HamsterWheel.HLinq.Benchmarks/LanguageBenchmarks.cs`. The words are the demo's Polish
ones: nine clause roots, `gdzie`, `wybierz`, `wez`, `pomin`, `zlicz`, `sortujPo`, `sortujPoMalejaco`,
`potemPo`, `potemPoMalejaco`. "Invariant words" is the same API with nothing registered, which is every API
that does not opt in.

## The walk, per query

| query | invariant words | invariant words, in a language with words of its own | same query written in those words |
|---|---|---|---|
| `where[x.Int==5]` | 1.59 µs | 1.94 µs (+23%) | 1.96 µs (+24%) |
| `where[x.Date>2023-10-12]` | 1.76 µs | 2.11 µs (+20%) | 2.07 µs (+18%) |
| `take[10..20]` | 1.03 µs | 1.26 µs (+22%) | 1.26 µs (+22%) |
| `select[27 properties]` | 17.39 µs | 20.51 µs (+18%) | 20.34 µs (+17%) |
| full chain | 8.10 µs | 9.68 µs (+20%) | 10.28 µs (+27%) |

Three things the table says:

- **the tax is the number of words, not the words.** The same query costs the same whether it is spelled
  with the invariant words or with the registered ones, in the same language. Nothing about a translated
  word is slower.
- **an API that registers no words pays nothing, and neither does a request that names no culture**: both
  are the invariant walk, the grammar and the possibilities every API builds. Every number above is paid
  only by a request that asked to be read in a language of its own.
- **the walk pays it at every window rebuild, not at every character**: the extra words are asked whether
  their adjacency fits the tokens committed, once per window. The size of the vocabulary is the size of
  the tax, which is the API's own decision, word by word.

Allocations move by the two scratch arrays the walk sizes by word count: +144 B on `where[x.Int==5]`, and
nothing visible on a wide `select` where the token list dominates.

Both tables were measured at this head, one session; the columns of the last table come from two sessions,
which between them differ by a few percent on the same code.

## The language, per culture, once

| what | time | allocated |
|---|---|---|
| an API with no words of any culture (the invariant language) | 183 µs | 389 KB |
| the same API with a language of nine words | 327 µs | 721 KB |

About 145 µs and 330 KB for a language: a grammar over the words of that culture and the invariant ones,
the self-check over them, the text rules of the walk and its clause partitions. The possibilities are not
built again - one possibility stands in every language it is a word of, and a language differs by the
grammar it asks and the rules its words produce. All of it is paid while the API is composed, once per
culture with words, never per request, and a language whose words do not make a valid grammar stops the API
rather than answering 500 to the first query written in it. For scale, one query tokenizes in 1-10 µs: a
language pays for itself in the first hundred or so queries written in it.

## Why the walk got faster while the possibilities got slimmer

Measured before (`0.13.0` + the first S5 head, possibilities answering for their own adjacency, clause
scope and token creation) and after (this head, the grammar answering, one possibility standing in every
language): `--filter '*Tokenizer*'`, same queries, invariant words only, so the two columns are the same
work.

| query | possibilities answering for their grammar | grammar answering for them |
|---|---|---|
| `where[x.Int==5]` | 2.37 µs | 1.59 µs (-33%) |
| `where[x.Date>2023-10-12]` | 2.48 µs | 1.69 µs (-32%) |
| `take[10..20]` | 1.53 µs | 1.01 µs (-34%) |
| `select[27 properties]` | 23.29 µs | 17.39 µs (-25%) |
| full chain | 12.19 µs | 8.08 µs (-34%) |
| `orderByDescending[x.Name]` | 3.48 µs | 1.55 µs (-55%) |

The walk now holds, per word of its language, the finished text rule and the rule of the token: the window
loop asks a `sealed` pattern directly instead of an interface call into a possibility that forwarded to a
pattern, and a keyword no longer walks a chain of delegates and a lazily fetched delimiter property to learn
what the language allows after it. A possibility is consulted once per language and never per character.

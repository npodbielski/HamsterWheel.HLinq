# Plans

Open plans in this repo (`plans/`); completed plans move to `plans/done/`.

| Plan | Status |
|---|---|
| [`take[10..20]` range paging syntax](plans/done/take-range-syntax.md) | ✅ done - shipped in 0.7.0 (!17, !18, !19, closes #8) - note: S1 grammar sketch superseded by !20 (RangeOperator matches `10..` with its number) |
| [Grammar rules & span-matching tokenizer](plans/done/grammar-rules.md) | ✅ done - closed #4 (+ #11) in 0.9.0 (!24) and 0.10.0 (!25); S5 follow-ups (#7, #5, #1) are separate plans |
| [Root-aware grammar](plans/done/root-aware-grammar.md) | ✅ done - shipped in 0.11.0 (!32, closes #1): tokens are scoped to their clause, the `=` next-character workaround is gone, reverse comparison and property-vs-property comparison work - note: the revision where a single `=` also compared outside `select` was reverted before merge, see §3b |
| [Culture info](plans/culture-info.md) | 🚧 in progress - quoting shipped in 0.11.1 (!35) and parameters beside clauses in !36; decided that the grammar stays culture-free (`,` is always the list separator, an ambiguous value is quoted) and a request carries its own culture - S1 styles, S3 quoted values reaching their type, S4 the switch still to build (#5, shared with #7) |
| [Member resolution](plans/member-resolution.md) | 📝 designed - a name matching two members picks the better candidate and reports it (#9) |

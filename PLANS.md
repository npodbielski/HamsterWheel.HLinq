# Plans

Open plans in this repo (`plans/`); completed plans move to `plans/done/`.

| Plan | Status |
|---|---|
| [`take[10..20]` range paging syntax](plans/done/take-range-syntax.md) | ✅ done - shipped in 0.7.0 (!17, !18, !19, closes #8) - note: S1 grammar sketch superseded by !20 (RangeOperator matches `10..` with its number) |
| [Grammar rules & span-matching tokenizer](plans/done/grammar-rules.md) | ✅ done - closed #4 (+ #11) in 0.9.0 (!24) and 0.10.0 (!25); S5 follow-ups (#7, #5, #1) are separate plans |
| [Root-aware grammar](plans/root-aware-grammar.md) | 🚧 in progress - S1/S2 retire the `=` next-character workaround, close #1 and compare two property paths (!32) |
| [Member resolution](plans/member-resolution.md) | 📝 designed - a name matching two members picks the better candidate and reports it (#9) |

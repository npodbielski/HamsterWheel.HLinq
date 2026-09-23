using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Filtering;
using HamsterWheel.HLinq.Tokens.Ordering;
using HamsterWheel.HLinq.Tokens.Paging;
using HamsterWheel.HLinq.Tokens.Selecting;

namespace HamsterWheel.HLinq.Pipeline.Tokenizer;

public class Grammar(GrammarDependencies dependencies) : IGrammar
{
    public static char LastCharOfLeftSquareBracketKeyword;
    private static char _lastCharOfEqualityKeyword;

    /// <summary>
    ///     The whole grammar, one rule per token, adjacency declared only as matchers over the
    ///     tokens committed so far: <c>After(typeof(NameOrValue))</c> means "directly after a
    ///     value", a pattern like <c>[.., RightSquareBracket, Dot]</c> means "after the closing
    ///     bracket of one clause and the chain dot". Which characters end a free text token are
    ///     the first characters of the keywords of the tokens these same matchers allow to
    ///     follow, see <see cref="GetDelimiters" />.
    /// </summary>
    internal IGrammarRule[] Rules => field ??=
    [
        //roots
        new GrammarRootRule<Select>(),
        new GrammarRootRule<Where>(),
        new GrammarRootRule<OrderBy>(),
        new GrammarRootRule<OrderByDescending>(),
        new GrammarRootRule<Count>(),
        new GrammarRootRule<Skip>(),
        new GrammarRootRule<Take>(),
        new GrammarRootRule<ThenBy>(),
        new GrammarRootRule<ThenByDescending>(),
        new GrammarRule<PropertyName>(false,
            [pt => pt is [.., Entity, Dot] or [.., PropertyName, Dot]]),
        new GrammarRule<NameOrValue>(false,
        [
            pt => pt is [.., _, IComparisonToken or LeftCircleBracket or LeftSquareBracket or Comma or Assignment],
            // end side of take[start..end] / take[..end] range
            pt => pt is [.., RangeOperator]
        ]),
        // the range operator matches the number in front of it (`10..`), so it is only ever
        // possible directly after the `[` of take/skip
        new GrammarRule<RangeOperator>(false, [static pt => pt is [.., LeftSquareBracket]]),
        new GrammarRule<And>(false, [static pt => pt is [.., NameOrValue or PropertyName or RightCircleBracket]]),
        new GrammarRule<Or>(false, [static pt => pt is [.., NameOrValue or PropertyName or RightCircleBracket]]),
        new GrammarRule<Entity>(false,
        [
            static pt => pt is
                [.., LeftSquareBracket or And or Or or Comma or LeftCircleBracket or Assignment or IComparisonToken]
        ]),
        new GrammarRule<Comma>(false, [static pt => pt is [.., NameOrValue or PropertyName]]),
        new GrammarRule<Dot>(false, [static pt => pt is [.., Entity or PropertyName or RightSquareBracket]]),
        // `=` renames only inside a select clause; in every other clause the same character is no
        // token at all. The clause is what keeps the rename apart from the comparison tokens, so
        // neither has to look at the next character to avoid swallowing the other
        new GrammarRule<Assignment>(false,
            [pt => pt.Count > 0 && pt is [.., NameOrValue]],
            insideClauseOf: typeof(Select)),
        // comparisons compare in every clause but select, which has no condition to compare in -
        // this is what keeps `=` unambiguous: inside a select clause only the rename claims it
        new GrammarRule<Equality>(false, [static pt => pt is [.., NameOrValue or PropertyName or Entity]],
            outsideClauseOf: typeof(Select)),
        new GrammarRule<Inequality>(false, [static pt => pt is [.., NameOrValue or PropertyName or Entity]],
            outsideClauseOf: typeof(Select)),
        new GrammarRule<LessOrEqualThan>(false, [static pt => pt is [.., NameOrValue or PropertyName or Entity]],
            outsideClauseOf: typeof(Select)),
        new GrammarRule<LessThan>(false, [static pt => pt is [.., NameOrValue or PropertyName or Entity]],
            nextCharMatcher: static c => c != _lastCharOfEqualityKeyword,
            outsideClauseOf: typeof(Select)),
        new GrammarRule<GreaterOrEqualThan>(false, [static pt => pt is [.., NameOrValue or PropertyName or Entity]],
            outsideClauseOf: typeof(Select)),
        new GrammarRule<GreaterThan>(false, [static pt => pt is [.., NameOrValue or PropertyName or Entity]],
            nextCharMatcher: static c => c != _lastCharOfEqualityKeyword,
            outsideClauseOf: typeof(Select)),
        new GrammarRule<LeftCircleBracket>(false,
        [
            static pt => pt is [.., LeftSquareBracket or And or Or or MethodName or LeftCircleBracket]
        ]),
        new GrammarRule<RightCircleBracket>(false,
        [
            static pt => pt is [.., NameOrValue or PropertyName or RightCircleBracket]
        ]),
        new GrammarRule<LeftSquareBracket>(false,
        [
            static pt => pt is
            [
                ..,
                Where or Skip or Take or OrderBy or OrderByDescending or ThenBy or ThenByDescending or Select or Count
            ]
        ]),
        new GrammarRule<RightSquareBracket>(false,
            [
                static pt => pt is
                    [.., PropertyName or NameOrValue or RightCircleBracket or LeftSquareBracket or RangeOperator]
            ]
        ),
        new GrammarRule<MethodName>(false,
        [
            static pt => pt is [.., Dot or LeftSquareBracket]
        ])
    ];

    /// <summary>
    ///     The characters that may directly follow <paramref name="type" />: the first characters
    ///     of the keywords of every token whose adjacency rule accepts a query ending in it.
    /// </summary>
    public char[] GetDelimiters(Type type)
    {
        IToken[] marker = [GrammarValidator.MarkerOf(type)];
        return
        [
            .. Rules
                .Where(r => r.PreviousTokensMatchers.Any(m => m(marker)))
                .Select(r => KeywordOf(r.ForType))
                .Where(k => k is not null)
                .Cast<string>()
                .Select(k => k[0])
                .Distinct()
        ];
    }

    public IGrammarRule GetRuleFor<TToken>()
    {
        LastCharOfLeftSquareBracketKeyword =
            KeywordOf(typeof(LeftSquareBracket))?[^1] ??
            throw new GrammarValidator.UnknownTokenTypeException<LeftSquareBracket>();
        _lastCharOfEqualityKeyword =
            KeywordOf(typeof(Equality))?[^1] ??
            throw new GrammarValidator.UnknownTokenTypeException<Equality>();

        return Rules.FirstOrDefault(t => t.IsFor<TToken>()) ??
               throw new GrammarValidator.UnknownTokenTypeException<TToken>();
    }

    private string? KeywordOf(Type tokenType) =>
        dependencies.TokenPossibilities.FirstOrDefault(p => p.ForType == tokenType)?.Keyword;
}
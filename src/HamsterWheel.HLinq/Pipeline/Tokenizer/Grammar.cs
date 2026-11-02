using System.Collections.Concurrent;
using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Filtering;
using HamsterWheel.HLinq.Tokens.Grouping;
using HamsterWheel.HLinq.Tokens.Ordering;
using HamsterWheel.HLinq.Tokens.Paging;
using HamsterWheel.HLinq.Tokens.Selecting;

namespace HamsterWheel.HLinq.Pipeline.Tokenizer;

public class Grammar(IEnumerable<IHLinqWord> tokenWords) : IGrammar
{
    private readonly IHLinqWord[] _words = [.. tokenWords];

    private readonly ConcurrentDictionary<string, char[]> _continuations =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// The whole grammar, one rule per token, adjacency declared only as matchers over the
    /// tokens committed so far: <c>After(typeof(NameOrValue))</c> means "directly after a
    /// value", a pattern like <c>[.., RightSquareBracket, Dot]</c> means "after the closing
    /// bracket of one clause and the chain dot". Which characters end a free text token are
    /// the first characters of the keywords the tokens these same matchers allow to follow are
    /// spelled with, see <see cref="GetDelimiters" />.
    /// </summary>
    public IGrammarRule[] Rules => field ??= BuildRules();

    internal IHLinqWord[] Words => [.. _words];

    private IGrammarRule[] BuildRules()
    {
        var rootDelimiters = LastCharsOfKeywordsOf(typeof(LeftSquareBracket));
        var endsComparison = LastCharsOfKeywordsOf(typeof(Equality));

        return
        [
            //roots
            new GrammarRootRule<Select>(rootDelimiters),
            new GrammarRootRule<Where>(rootDelimiters),
            new GrammarRootRule<GroupBy>(rootDelimiters),
            new GrammarRootRule<OrderBy>(rootDelimiters),
            new GrammarRootRule<OrderByDescending>(rootDelimiters),
            new GrammarRootRule<Count>(rootDelimiters),
            new GrammarRootRule<Skip>(rootDelimiters),
            new GrammarRootRule<Take>(rootDelimiters),
            new GrammarRootRule<ThenBy>(rootDelimiters),
            new GrammarRootRule<ThenByDescending>(rootDelimiters),
            new GrammarRule<PropertyName>(false,
            [
                static pt => pt is [.., Entity, Dot] or [.., PropertyName, Dot] or [.., SubEntity, Dot]
            ]),
            new GrammarRule<NameOrValue>(false,
            [
                static pt => pt is
                [
                    .., _, IComparisonToken or LeftCircleBracket or LeftSquareBracket or Comma or Assignment
                ],
                static pt => pt is [.., RangeOperator]
            ]),
            new GrammarRule<RangeOperator>(false, [static pt => pt is [.., Take or Skip, LeftSquareBracket]]),
            new GrammarRule<And>(false,
                [static pt => pt is [.., NameOrValue or PropertyName or RightCircleBracket]]),
            new GrammarRule<Or>(false,
                [static pt => pt is [.., NameOrValue or PropertyName or RightCircleBracket]]),
            new GrammarRule<Entity>(false,
            [
                static pt => pt is
                    [.., LeftSquareBracket or And or Or or Comma or LeftCircleBracket or Assignment or
                        IComparisonToken]
            ]),
            new GrammarRule<Comma>(false,
                [static pt => pt is [.., NameOrValue or PropertyName or RightCircleBracket]]),
            new GrammarRule<Dot>(false,
            [
                static pt => pt is [.., Entity or PropertyName or RightSquareBracket or SubEntity]
            ]),
            new GrammarRule<Assignment>(false,
                [static pt => pt.Count > 0 && pt is [.., NameOrValue]],
                insideClauseOf: typeof(Select)),
            new GrammarRule<Equality>(false,
                [static pt => pt is [.., NameOrValue or PropertyName or Entity or RightCircleBracket]],
                outsideClauseOf: typeof(Select)),
            new GrammarRule<Inequality>(false,
                [static pt => pt is [.., NameOrValue or PropertyName or Entity or RightCircleBracket]],
                outsideClauseOf: typeof(Select)),
            new GrammarRule<LessOrEqualThan>(false,
                [static pt => pt is [.., NameOrValue or PropertyName or Entity or RightCircleBracket]],
                outsideClauseOf: typeof(Select)),
            new GrammarRule<LessThan>(false,
                [static pt => pt is [.., NameOrValue or PropertyName or Entity or RightCircleBracket]],
                nextCharsRefused: endsComparison,
                outsideClauseOf: typeof(Select)),
            new GrammarRule<GreaterOrEqualThan>(false,
                [static pt => pt is [.., NameOrValue or PropertyName or Entity or RightCircleBracket]],
                outsideClauseOf: typeof(Select)),
            new GrammarRule<GreaterThan>(false,
                [static pt => pt is [.., NameOrValue or PropertyName or Entity or RightCircleBracket]],
                nextCharsRefused: endsComparison,
                outsideClauseOf: typeof(Select)),
            new GrammarRule<LeftCircleBracket>(false,
            [
                static pt => pt is [.., LeftSquareBracket or And or Or or MethodName or LeftCircleBracket]
            ]),
            new GrammarRule<RightCircleBracket>(false,
            [
                static pt => pt is [.., NameOrValue or PropertyName or RightCircleBracket],
                static pt => pt is [.., LeftCircleBracket]
            ]),
            new GrammarRule<LeftSquareBracket>(false,
            [
                static pt => pt is
                [
                    .., Where or Skip or Take or OrderBy or OrderByDescending or ThenBy or ThenByDescending or
                        Select or Count or GroupBy
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
                static pt => pt is [.., Dot or LeftSquareBracket or IComparisonToken or Assignment]
            ]),
            new GrammarRule<SubEntity>(false,
            [
                static pt => pt is [.., LeftCircleBracket or Comma or Assignment] && InsideCircleBrackets(pt)
            ], insideClauseOf: typeof(Select))
        ];
    }

    private static bool InsideCircleBrackets(IReadOnlyList<IToken> previousTokens)
    {
        var open = 0;
        foreach (var token in previousTokens)
        {
            switch (token)
            {
                case LeftCircleBracket:
                    open++;
                    break;
                case RightCircleBracket:
                    open--;
                    break;
            }
        }

        return open > 0;
    }

    /// <summary>
    /// Value tokens (i.e. "John" in "x.Name=John") do not have keywords because they essentially free text.
    /// Instead, we rely on delimiters, characters that must end such values i.e. "]" always ends value in "where"
    /// </summary>
    public char[] GetDelimiters(Type type)
    {
        IToken[] marker = [MarkerOf(type)];
        return
        [
            .. Rules
                .Where(r => r.PreviousTokensMatchers.Any(m => m(marker)))
                .SelectMany(r => KeywordsOf(r.ForType))
                .Select(k => k[0])
                .Distinct()
        ];
    }

    /// <summary>
    /// OrderBy is contained in OrderByDescending. This makes sure that the tokenizer does not produce only the shorter token
    /// </summary>
    public bool MatchesLongerKeyword(string keyword, char? next) =>
        next is not null && ContinuationsOf(keyword).Contains(next.Value);

    /// <summary>
    /// Returns grammar rules for a specified token type
    /// </summary>
    public IGrammarRule GetRuleFor(Type tokenType) =>
        RuleByType.TryGetValue(tokenType, out var rule)
            ? rule
            : throw new HLinqTokenizer.UnknownTokenTypeException(tokenType);

    private Dictionary<Type, IGrammarRule> RuleByType => field ??= Rules.ToDictionary(r => r.ForType);

    /// <summary>
    /// A token of this type, standing for no text at all, to ask an adjacency rule what follows it.
    /// </summary>
    internal static IToken MarkerOf(Type tokenType) =>
        Markers.GetOrAdd(tokenType, t => (IToken)Activator.CreateInstance(t)!);

    private static readonly ConcurrentDictionary<Type, IToken> Markers = new();

    private char[] ContinuationsOf(string keyword) =>
        _continuations.GetOrAdd(keyword, k =>
            [
                .. _words
                    .Select(p => p.Keyword)
                    .Where(x => x is not null && x.Length > k.Length
                                          && x.StartsWith(k, StringComparison.OrdinalIgnoreCase))
                    .Select(x => x![k.Length])
                    .Distinct()
            ]);

    public string[] KeywordsOf(Type tokenType) =>
        [
            .. _words
                .Where(p => p.ForType == tokenType && p.Keyword is not null)
                .Select(p => p.Keyword!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
        ];

    private char[] LastCharsOfKeywordsOf(Type tokenType) =>
        [
            .. KeywordsOf(tokenType)
                .Select(k => k[^1])
                .Distinct()
        ];
}

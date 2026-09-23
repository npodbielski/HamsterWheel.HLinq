using HamsterWheel.HLinq.Pipeline.Tokenizer;
using HamsterWheel.HLinq.Tokens.Patterns;

namespace HamsterWheel.HLinq.Tokens;

public abstract class TokenPossibility(IGrammar grammar, string? keyword = null) : IHLinqTokenPossibility
{
    public MatchAt MatchAt(ReadOnlySpan<char> window, char? next) => Pattern.Match(window, next);

    private bool _delimitersFetched;

    /// <summary>
    ///     The characters the grammar allows directly after this token, derived from its adjacency
    ///     rule. A free text token ends at them, and a keyword which is also a word
    ///     (<see cref="Entity" />) completes only at one of them.
    /// </summary>
    protected char[]? Delimiters
    {
        get
        {
            if (_delimitersFetched)
            {
                return field;
            }

            _delimitersFetched = true;
            return field = Grammar.GetDelimiters(ForType);
        }
    }

    public abstract bool CanBeFirst { get; }
    public string? Keyword { get; } = keyword;
    protected IGrammar Grammar => grammar;
    public abstract Type ForType { get; }

    /// <summary>
    ///     The text rule of this token, see <see cref="TokenPattern" />; its adjacency rule is
    ///     <see cref="MatchesPreviousTokens" />. Keywords get a <see cref="KeywordPattern" />,
    ///     everything else a <see cref="ValuePattern" /> - override <see cref="BuildPattern" />
    ///     for a custom text rule (i.e. <see cref="Patterns.RangeOperatorPattern" />).
    /// </summary>
    protected TokenPattern Pattern => field ??= BuildPattern();

    protected virtual TokenPattern BuildPattern() =>
        Keyword is not null
            ? new KeywordPattern(Keyword, NextIsAllowedWhenKeywordMatch)
            : new ValuePattern(() => Delimiters);

    public abstract IToken Build(Range range);

    public abstract bool MatchesPreviousTokens(List<IToken> previousTokens);

    public abstract bool MatchesClauseRoot(Type? currentClauseRoot);

    protected virtual bool NextIsAllowedWhenKeywordMatch(char? next) => true;
}

public abstract class TokenPossibility<T>(IGrammar grammar, string? tokenString = null)
    : TokenPossibility(grammar, tokenString) where T : TokenBase, new()
{
    public override Type ForType => typeof(T);
    public sealed override bool CanBeFirst => Rule.CanBeFirst;

    /// <summary>
    ///     Adjacency comes from the token's single grammar rule, so a localization replacing this
    ///     possibility can never drift from (or carry its own copy of) the grammar.
    /// </summary>
    public sealed override bool MatchesPreviousTokens(List<IToken> previousTokens) =>
        Rule.PreviousTokensMatch(previousTokens);

    /// <summary>
    ///     Clause scope comes from the same single rule as adjacency, for the same reason: a
    ///     localization replacing this possibility must not be able to claim a clause the grammar
    ///     did not give the token.
    /// </summary>
    public sealed override bool MatchesClauseRoot(Type? currentClauseRoot) =>
        Rule.MatchesClauseRoot(currentClauseRoot);

    public sealed override IToken Build(Range range) => BuildImpl(range);
    protected IGrammarRule Rule => field ??= Grammar.GetRuleFor<T>();

    protected virtual T BuildImpl(Range range)
    {
        var token = new T
        {
            Range = range
        };
        return token;
    }
}
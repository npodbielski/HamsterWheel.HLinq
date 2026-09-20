using HamsterWheel.HLinq.Pipeline.Tokenizer;
using HamsterWheel.HLinq.Tokens.Patterns;

namespace HamsterWheel.HLinq.Tokens;

public abstract class TokenPossibility(IGrammar grammar, string? keyword = null) : IHLinqTokenPossibility
{
    public MatchAt MatchAt(ReadOnlySpan<char> window, char? next) => Pattern.Match(window, next);

    private TokenPattern? _pattern;
    private bool _delimitersFetched;
    private char[]? _delimiters;

    protected char[]? Delimiters
    {
        get
        {
            if (Keyword is not null)
            {
                return null;
            }

            if (_delimitersFetched)
            {
                return _delimiters;
            }

            _delimitersFetched = true;
            return _delimiters = Grammar.GetDelimiters(ForType);
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
    protected TokenPattern Pattern => _pattern ??= BuildPattern();

    protected virtual TokenPattern BuildPattern() =>
        Keyword is not null
            ? new KeywordPattern(Keyword, NextIsAllowedWhenKeywordMatch)
            : new ValuePattern(() => Delimiters);

    public abstract IToken Build(Range range);

    public abstract bool MatchesPreviousTokens(List<IToken> previousTokens);

    protected virtual bool NextIsAllowedWhenKeywordMatch(char? next) => true;
}

public abstract class TokenPossibility<T>(IGrammar grammar, string? tokenString = null)
    : TokenPossibility(grammar, tokenString) where T : TokenBase, new()
{
    private IGrammarRule? _rule;
    public override Type ForType => typeof(T);
    public sealed override bool CanBeFirst => Rule.CanBeFirst;

    /// <summary>
    ///     Adjacency comes from the token's single grammar rule, so a localization replacing this
    ///     possibility can never drift from (or carry its own copy of) the grammar.
    /// </summary>
    public sealed override bool MatchesPreviousTokens(List<IToken> previousTokens) =>
        Rule.PreviousTokensMatch(previousTokens);

    public sealed override IToken Build(Range range) => BuildImpl(range);
    protected IGrammarRule Rule => _rule ??= Grammar.GetRuleFor<T>();

    protected virtual T BuildImpl(Range range)
    {
        var token = new T
        {
            Range = range
        };
        return token;
    }
}
namespace HamsterWheel.HLinq.Tokens;

public abstract class TokenPossibility(IGrammar grammar, string? keyword = null, bool haveDelimiters = false)
    : IHLinqTokenPossibility
{
    private bool _delimitersFetched;
    private char[]? _delimiters;

    protected char[]? Delimiters
    {
        get
        {
            if (!haveDelimiters)
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

    public virtual int CanBeAt(int index, ReadOnlySpan<char> subset, char? next, List<IToken> previousToken)
    {
        var possibility = 0;
        if (!PreviousTokensMatch(previousToken))
        {
            return possibility;
        }

        possibility += 50;
        if (Keyword is not null)
        {
            if (subset.Length < Keyword.Length)
            {
                if (Keyword.AsSpan()[..subset.Length].Equals(subset, StringComparison.OrdinalIgnoreCase))
                {
                    possibility += 50 * subset.Length / Keyword.Length;
                    //check next char -> i.e., if we have subset 'orderBy' and the next char is '['
                    //...then the possibility of 'orderByDescending' is zero at this point
                    if (Keyword.Length > subset.Length && next != Keyword[subset.Length])
                    {
                        possibility = 0;
                    }
                }
                else
                {
                    return 0;
                }
            }
            else
            {
                if (Keyword.AsSpan().Equals(subset, StringComparison.OrdinalIgnoreCase) &&
                    NextIsAllowedWhenKeywordMatch(next))
                {
                    possibility += 50;
                }
                else
                {
                    return 0;
                }
            }
        }
        else
        {
            if (next is not null && Delimiters?.Contains(next.Value) == true)
            {
                possibility += 50;
            }
        }

        return possibility;
    }

    public abstract IToken Build(Range range);

    protected virtual bool PreviousTokensMatch(List<IToken> previousTokens) =>
        previousTokens.Count != 0 && PreviousTokenMatchImpl(previousTokens[^1]);

    protected virtual bool PreviousTokenMatchImpl(IToken previousToken) => false;

    protected virtual bool NextIsAllowedWhenKeywordMatch(char? next) => true;
}

public abstract class TokenPossibility<T>(IGrammar grammar, string? tokenString = null, bool haveDelimiters = false)
    : TokenPossibility(grammar, tokenString, haveDelimiters) where T : TokenBase, new()
{
    private IGrammarRule? _rule;
    public override Type ForType => typeof(T);
    public sealed override bool CanBeFirst => Rule.CanBeFirst;
    public sealed override TokenBase Build(Range range) => BuildImpl(range);
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
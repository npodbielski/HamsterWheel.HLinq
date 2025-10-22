namespace HamsterWheel.HLinq.Tokens;

public abstract class TokenPossibility(string? keyword = null, char[]? delimiters = null) : IHLinqTokenPossibility
{
    protected char[]? Delimiters { get; } = delimiters;

    public virtual int CanBeAt(int index, ReadOnlySpan<char> subset, char? next, List<IToken> previousToken)
    {
        var possibility = 0;
        if (!PreviousTokensMatch(previousToken))
        {
            return possibility;
        }

        possibility += 50;
        if (keyword is not null)
        {
            if (subset.Length < keyword.Length)
            {
                if (keyword.AsSpan()[..subset.Length].Equals(subset, StringComparison.OrdinalIgnoreCase))
                {
                    possibility += 50 * subset.Length / keyword.Length;
                    //check next char -> i.e., if we have subset 'orderBy' and the next char is '['
                    //...then the possibility of 'orderByDescending' is zero at this point
                    if (keyword.Length > subset.Length && next != keyword[subset.Length]) possibility = 0;
                }
                else
                {
                    return 0;
                }
            }
            else
            {
                if (keyword.AsSpan().Equals(subset, StringComparison.OrdinalIgnoreCase) &&
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

public abstract class TokenPossibility<T>(string? tokenString = null, char[]? delimiters = null)
    : TokenPossibility(tokenString, delimiters) where T : TokenBase
{
    protected abstract T BuildImpl(Range range);

    public override TokenBase Build(Range range) => BuildImpl(range);
}
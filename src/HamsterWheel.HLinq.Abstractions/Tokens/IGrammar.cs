namespace HamsterWheel.HLinq.Tokens;

public interface IGrammar
{
    bool CanBeFirst<TToken>();
    char[] GetDelimiters(Type type);
    bool PreviousTokensMatch<TToken>(List<IToken> previousTokens);
    bool PreviousTokenMatch<TToken>(IToken previousToken);
    bool NextCharIsAllowed<TToken>(char? next);
}
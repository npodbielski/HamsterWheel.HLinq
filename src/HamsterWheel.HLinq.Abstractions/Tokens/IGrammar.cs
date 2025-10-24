namespace HamsterWheel.HLinq.Tokens;

public interface IGrammar
{
    char[] GetDelimiters(Type type);
    IGrammarRule GetRuleFor<TToken>();
}
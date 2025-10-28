namespace HamsterWheel.HLinq.Pipeline.Tokenizer;

public interface IGrammar
{
    char[] GetDelimiters(Type type);
    IGrammarRule GetRuleFor<TToken>();
}
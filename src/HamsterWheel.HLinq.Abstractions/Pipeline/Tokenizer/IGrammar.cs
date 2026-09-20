using HamsterWheel.HLinq.Tokens;

namespace HamsterWheel.HLinq.Pipeline.Tokenizer;

public interface IGrammar
{
    char[] GetDelimiters(Type type);
    IGrammarRule GetRuleFor<TToken>();

    /// <summary>
    ///     Validates the effective grammar - the rules together with the registered
    ///     possibilities, so a localization replacing a token is validated as it will run.
    ///     Throws <see cref="Exceptions.HLinqQueryException" /> listing every problem found:
    ///     a token without exactly one rule, a token unreachable from the start of a query,
    ///     or a keyword that swallows a longer keyword it can be confused with.
    /// </summary>
    void Validate();
}

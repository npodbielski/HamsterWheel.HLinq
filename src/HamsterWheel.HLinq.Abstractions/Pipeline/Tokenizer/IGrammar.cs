namespace HamsterWheel.HLinq.Pipeline.Tokenizer;

public interface IGrammar
{
    /// <summary>
    /// The whole grammar, one rule per token - what the grammar is made of, so that reading it does
    /// not need the concrete grammar or a hand in building it.
    /// </summary>
    IGrammarRule[] Rules { get; }

    char[] GetDelimiters(Type type);

    /// <summary>
    /// Returns this grammar rule for specified token.
    /// </summary>
    IGrammarRule GetRuleFor(Type tokenType);

    /// <summary>
    /// Returns true if multiple keywords shares prefix
    /// </summary>
    bool MatchesLongerKeyword(string keyword, char? next);
}

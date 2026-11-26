using System.Globalization;

namespace HamsterWheel.HLinq.Pipeline.Tokenizer;

/// <summary>
/// A token the tokenizer may produce: which token, spelled by which word, of which culture.
/// </summary>
public interface IHLinqWord
{
    Type ForType { get; }
    string? Keyword { get; }

    /// <summary>
    /// The culture this word belongs to, null for the invariant language every request is read with.
    /// </summary>
    CultureInfo? Culture { get; }

    /// <summary>
    /// The token's text rule as the words of one language allow it.
    /// </summary>
    TokenPattern PatternFor(IGrammar grammar);
}

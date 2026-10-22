using System.Globalization;

namespace HamsterWheel.HLinq.Pipeline.Tokenizer;

/// <summary>
/// A token the tokenizer may produce, told by three things: which token it is, which word spells it,
/// and which culture that word belongs to. Everything else the tokenizer asks about a token is a
/// question about a language, not about this word: which tokens may stand next to which
/// (<see cref="IGrammarRule.PreviousTokensMatch" />), which clause a token belongs to
/// (<see cref="IGrammarRule.MatchesClauseRoot" />) and which characters end a free text token are
/// answered by the <see cref="IGrammar" /> of the language being tokenized with, so one word stands in
/// every language it is registered in.
/// What does belong here is the token's own text rule, which is why a word hands the grammar of
/// a language to <see cref="PatternFor" /> and gets back the rule the tokenizer matches.
/// </summary>
public interface IHLinqWord
{
    Type ForType { get; }
    string? Keyword { get; }

    /// <summary>
    /// The culture this word belongs to, null for the invariant language every request is read with.
    /// A word of a culture stands next to the words the language always had, and tokenizing a request
    /// is chosen by it: the characters that end a token are derived from the words that may follow it,
    /// so which words are viable together is the definition of a language.
    /// </summary>
    CultureInfo? Culture { get; }

    /// <summary>
    /// The text rule of this token, read against the words of one language: the tokenizer asks once per
    /// word and language, never per request, and matches it against its window
    /// (<see cref="TokenPattern.Match" />).
    /// </summary>
    TokenPattern PatternFor(IGrammar grammar);
}

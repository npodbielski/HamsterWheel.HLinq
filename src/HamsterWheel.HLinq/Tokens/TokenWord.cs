using System.Globalization;
using HamsterWheel.HLinq.Pipeline.Tokenizer;
using HamsterWheel.HLinq.Tokens.Patterns;

namespace HamsterWheel.HLinq.Tokens;

/// <summary>
/// A token of the grammar, spelled by a word: the word, the culture it belongs to, and the text rule
/// of <typeparamref name="T" />. Adjacency, clause scope and the characters a token ends at are the
/// grammar's answers about a language, so they are asked of the grammar being tokenized with - which is
/// what lets one word stand in every language it is registered in.
/// </summary>
public abstract class TokenWord(string? keyword = null, CultureInfo? culture = null)
    : IHLinqWord
{
    public string? Keyword { get; } = keyword;
    public CultureInfo? Culture { get; } = culture;
    public abstract Type ForType { get; }

    public TokenPattern PatternFor(IGrammar grammar) => BuildPattern(grammar);

    /// <summary>
    /// The text rule of this token for one language, see <see cref="TokenPattern" />: keywords get a
    /// <see cref="KeywordPattern" />, free text a <see cref="ValuePattern" /> ending at the characters
    /// this language allows after this token - override for a text rule that is neither (i.e.
    /// <see cref="Patterns.RangeOperatorPattern" />).
    /// </summary>
    protected virtual TokenPattern BuildPattern(IGrammar grammar) =>
        Keyword is not null
            ? new KeywordPattern(Keyword, next => NextIsAllowedWhenKeywordMatch(grammar, next))
            : new ValuePattern(grammar.GetDelimiters(ForType));

    /// <summary>
    /// Whether the character after a full keyword leaves this token the one the window is: a keyword
    /// that completes where a longer word of the same language continues would take that word's window
    /// from it, and a keyword that is also a word (<see cref="Entity" />) completes only where the
    /// language allows something to follow it.
    /// </summary>
    protected virtual bool NextIsAllowedWhenKeywordMatch(IGrammar grammar, char? next) => true;
}

public abstract class TokenWord<T>(string? tokenString = null, CultureInfo? culture = null)
    : TokenWord(tokenString, culture) where T : TokenBase, new()
{
    public override Type ForType => typeof(T);
}

using HamsterWheel.HLinq.Tokens;

namespace HamsterWheel.HLinq.Pipeline.Tokenizer;

/// <summary>
///     A token the tokenizer may produce. Both halves of the token's grammar live here:
///     <see cref="MatchesPreviousTokens" /> (adjacency: which tokens may precede it) and
///     <see cref="MatchAt" /> (its text rule: which characters the token is made of, e.g.
///     <c>name = ALPHA *( ALPHA | DIGIT | "_" )</c> or a fixed keyword like <c>orderBy</c>).
///     The tokenizer evaluates adjacency once per window (it cannot change while the window
///     grows) and the text rule on every window growth; there is no backtracking - a window
///     dies (<see cref="MatchAt.Discarded" />) instead of being rewound.
/// </summary>
public interface IHLinqTokenPossibility
{
    bool CanBeFirst { get; }
    Type ForType { get; }
    string? Keyword { get; }
    bool MatchesPreviousTokens(List<IToken> previousTokens);
    MatchAt MatchAt(ReadOnlySpan<char> window, char? next);
    IToken Build(Range range);
}

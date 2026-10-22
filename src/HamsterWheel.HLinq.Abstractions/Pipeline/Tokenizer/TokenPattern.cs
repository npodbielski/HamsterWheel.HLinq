namespace HamsterWheel.HLinq.Pipeline.Tokenizer;

/// <summary>
/// The text rule of a token: which characters the token consists of, i.e.
/// <c>name = ALPHA *( ALPHA | DIGIT | "_" )</c> for a name or property reference, a fixed
/// keyword like <c>orderBy</c>, or the digits and dots of a range operator. This is part of
/// the HLinq grammar - the other part is adjacency (<see cref="IGrammarRule" />: which
/// tokens may stand next to which). <see cref="Match" /> receives the current tokenizer
/// window and the character after it and returns the <see cref="MatchAt" /> for that text,
/// assuming adjacency already matched: a pattern is a pure function of its text, it never
/// looks at the surrounding tokens or at syntax.
/// </summary>
public abstract class TokenPattern
{
    public abstract MatchAt Match(ReadOnlySpan<char> window, char? next);
}

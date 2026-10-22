using HamsterWheel.HLinq.Pipeline.Tokenizer;

namespace HamsterWheel.HLinq.Tokens;

public sealed class Entity : TokenBase
{
    // ReSharper disable once UnusedType.Global -> Used by tokenizer
    public sealed class Word() : TokenWord<Entity>("x")
    {
        //`x` is a single letter that can start a value (`where[x.Name==xander]`), so it has to end
        //where the grammar says a token may follow it instead of after its own character
        protected override TokenPattern BuildPattern(IGrammar grammar) =>
            new EntityPattern(Keyword!, grammar.GetDelimiters(ForType));

        private sealed class EntityPattern(string keyword, char[]? delimiters) : TokenPattern
        {
            public override MatchAt Match(ReadOnlySpan<char> window, char? next)
            {
                if (window.Length > keyword.Length ||
                    !keyword.AsSpan(0, window.Length).Equals(window, StringComparison.OrdinalIgnoreCase))
                {
                    return MatchAt.Discarded;
                }

                if (window.Length < keyword.Length)
                {
                    return next == keyword[window.Length]
                        ? MatchAt.Possible
                        : MatchAt.Discarded;
                }

                if (next is null)
                {
                    return MatchAt.CompleteAtEnd(window.Length);
                }

                return delimiters?.Contains(next.Value) == true
                    ? MatchAt.CompleteAtEnd(window.Length)
                    : MatchAt.Discarded;
            }
        }
    }

    public static Entity Build(Range range) => new() { Range = range };
    public static Entity Empty { get; } = new() { Range = default };
}
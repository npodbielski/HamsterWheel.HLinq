using HamsterWheel.HLinq.Pipeline.Tokenizer;
using HamsterWheel.HLinq.Tokens.Patterns;

namespace HamsterWheel.HLinq.Tokens.Selecting;

/// <summary>
/// Token representing nested entity in nested select projection.
/// </summary>
public sealed class SubEntity : TokenBase
{
    // ReSharper disable once UnusedType.Global -> used by tokenizer
    public sealed class Word : TokenWord<SubEntity>
    {
        protected override TokenPattern BuildPattern(IGrammar grammar) =>
            new Pattern(new Entity.Word().Keyword!, grammar.GetDelimiters(ForType));

        private sealed class Pattern(string entityAlias, char[]? delimiters) : ValuePattern(delimiters)
        {
            public override MatchAt Match(ReadOnlySpan<char> window, char? next) =>
                !char.IsLetter(window[0]) ||
                window.Equals(entityAlias.AsSpan(), StringComparison.OrdinalIgnoreCase)
                    ? MatchAt.Discarded
                    : base.Match(window, next);
        }
    }

    public static SubEntity Build(Range range) => new() { Range = range };
    public static SubEntity Empty { get; } = new() { Range = default };
}

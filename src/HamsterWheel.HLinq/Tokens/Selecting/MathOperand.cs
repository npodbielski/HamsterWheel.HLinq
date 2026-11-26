using HamsterWheel.HLinq.Pipeline.Tokenizer;

namespace HamsterWheel.HLinq.Tokens.Selecting;

/// <summary>
/// A mathematical operand of a math operation: <c>" "</c> in <c>x.Name+" "+x.Surname</c>,
/// <c>2</c> in <c>x.Total+2</c>.
/// </summary>
public sealed class MathOperand : TokenBase
{
    // ReSharper disable once UnusedType.Global -> used by the Grammar
    public sealed class Word : TokenWord<MathOperand>
    {
        protected override TokenPattern BuildPattern(IGrammar grammar) =>
            new MathOperandPattern(grammar.GetDelimiters(ForType));

        private sealed class MathOperandPattern(char[]? delimiters) : TokenPattern
        {
            public override MatchAt Match(ReadOnlySpan<char> window, char? next) =>
                window[0] == '"' ? MatchQuotedText(window) : MatchNumber(window, next);

            //a text opens with a quote and only its matching quote closes it: everything between
            //them - a bracket, an operator, a comma - is the text of the value
            private static MatchAt MatchQuotedText(ReadOnlySpan<char> window)
            {
                if (window.Length == 1)
                {
                    return MatchAt.Possible;
                }

                return window[^1] == '"' ? MatchAt.CompleteAtEnd(window.Length) : MatchAt.Possible;
            }

            //a number ends at the first character it cannot be made of: an operator closes it
            //however the pack spells it - `2-1` chains operator after operator with no space -
            //and a dash that is no operator of no pack ends no number, so a date outside a group
            //is read whole by the free value grammar of the clause
            private MatchAt MatchNumber(ReadOnlySpan<char> window, char? next)
            {
                var separators = 0;
                for (var index = 0; index < window.Length; index++)
                {
                    var current = window[index];
                    if (char.IsDigit(current))
                    {
                        continue;
                    }

                    //a separator of the culture belongs to the number while it is being read
                    if (current is '.' or ',' && index > 0 && separators++ == 0)
                    {
                        continue;
                    }

                    //a sign, a letter, a dash of a date: the window opened too early or is no
                    //number at all - a sign is the window of an operator, a letter of a name
                    return MatchAt.Discarded;
                }

                return next is not null && delimiters?.Contains(next.Value) == true
                    ? MatchAt.CompleteAtEnd(window.Length)
                    : MatchAt.Possible;
            }
        }
    }

    public static MathOperand Build(Range range) => new() { Range = range };
    public static MathOperand Empty { get; } = new() { Range = default };
}
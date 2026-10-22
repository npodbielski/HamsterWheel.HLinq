using HamsterWheel.HLinq.Pipeline.Tokenizer;

namespace HamsterWheel.HLinq.Tokens;

public sealed class NameOrValue : TokenBase
{
    // ReSharper disable once UnusedType.Global -> used by tokenizer
    public sealed class Word : TokenWord<NameOrValue>
    {
        protected override TokenPattern BuildPattern(IGrammar grammar) =>
            new NameOrValuePattern(grammar.GetDelimiters(ForType));

        /// <summary>
        /// Free text pattern that can be only recognized by its delimiters. I.e. 'John' in 'where[x.name == John]'
        /// </summary>
        private sealed class NameOrValuePattern(char[]? delimiters) : TokenPattern
        {
            private const char Quote = '"';

            public override MatchAt Match(ReadOnlySpan<char> window, char? next)
            {
                var insideQuote = false;
                for (var index = 0; index < window.Length; index++)
                {
                    var current = window[index];
                    if (current == Quote)
                    {
                        //a quote opens only at the first character, everywhere else it is text or the close
                        if (index == 0)
                        {
                            insideQuote = true;
                        }
                        else if (insideQuote)
                        {
                            insideQuote = false;
                        }

                        continue;
                    }

                    if (insideQuote)
                    {
                        //an open quote holds every delimiter, the ones that would end the clause too
                        continue;
                    }

                    if (current == '.' && index + 1 < window.Length && window[index + 1] == '.')
                    {
                        return MatchAt.Discarded;
                    }
                }

                if (insideQuote)
                {
                    return MatchAt.Possible;
                }

                return next is not null && delimiters?.Contains(next.Value) == true
                    ? MatchAt.CompleteAtEnd(window.Length)
                    : MatchAt.Possible;
            }
        }
    }

    public static NameOrValue Build(Range range) => new() { Range = range };
    public static NameOrValue Empty { get; } = new() { Range = default };
}
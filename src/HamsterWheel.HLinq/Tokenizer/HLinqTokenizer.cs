using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Parsers;
using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Filter;
using HamsterWheel.HLinq.Tokens.Order;
using HamsterWheel.HLinq.Tokens.Paging;
using HamsterWheel.HLinq.Tokens.Select;

namespace HamsterWheel.HLinq.Tokenizer;

public sealed class HLinqTokenizer(IHLinqParsersCollection services) : IHLinqTokenizer
{
    public IToken[] Tokenize(string hLinqQuery)
    {
        List<IToken> tokens = [];

        (IHLinqTokenPossibility token, int possibility)[] allTokens =
            services.Tokens.Select(t => (t, 0)).ToArray();
        List<(IHLinqTokenPossibility token, int possibility)> nextPossibleTokens = new();
        var span = hLinqQuery.AsSpan();
        var previousTokenIndex = 0;
        for (var index = 0; index < span.Length; index++)
        {
            var range = previousTokenIndex..(index + 1);
            var currentSubset = span[range];

            if (currentSubset.IsWhiteSpace())
            {
                previousTokenIndex++;
                continue;
            }

            var currentPossibleTokens = (nextPossibleTokens.Count > 0 ? [..nextPossibleTokens] : allTokens).ToArray();
            nextPossibleTokens.Clear();
            foreach (var pt in currentPossibleTokens)
            {
                var nextCharIndex = index;
                char? next;
                do
                {
                    next = nextCharIndex + 1 < span.Length ? span[nextCharIndex + 1] : null;
                    nextCharIndex++;
                } while (next is not null && char.IsWhiteSpace(next.Value));

                var possibility = pt.token.CanBeAt(index, currentSubset, next, tokens);
                if (possibility > 0) nextPossibleTokens.Add(pt with { possibility = possibility });
            }

            nextPossibleTokens = nextPossibleTokens.OrderBy(t => t.possibility).ToList();

            if (nextPossibleTokens.Any(t => t.possibility == 100))
            {
                var first = nextPossibleTokens.First(t => t.possibility == 100);
                if (first.possibility >= 100)
                {
                    previousTokenIndex = index + 1;
                    nextPossibleTokens.Clear();
                    tokens.Add(first.token.Build(range));
                    continue;
                }
            }

            if (currentPossibleTokens.Length == 0)
            {
                tokens.Add(new Unknown(range));
            }

            if (tokens.Count != 0 && tokens[^1] is Unknown)
                throw new UnknownTokenException(range,
                [
                    new Where(default), new Select(default), new Skip(default), new Take(default), new OrderBy(default),
                    new OrderByDescending(default)
                ]);
        }

        return tokens.ToArray();
    }
}
using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Tokens.Filtering;
using HamsterWheel.HLinq.Tokens.Ordering;
using HamsterWheel.HLinq.Tokens.Paging;
using HamsterWheel.HLinq.Tokens.Selecting;

namespace HamsterWheel.HLinq.Tokens;

using CurrentTokenPossibility = (IHLinqTokenPossibility token, int possibility);

public sealed class HLinqTokenizer(IEnumerable<IHLinqTokenPossibility> tokenPossibilities) : IHLinqTokenizer
{
    public IToken[] Tokenize(string hLinqQuery)
    {
        List<IToken> tokens = [];

        CurrentTokenPossibility[] allTokens = tokenPossibilities.Select(t => (t, 0)).ToArray();
        List<CurrentTokenPossibility> nextPossibleTokens = [];
        var span = hLinqQuery.AsSpan();
        var previousTokenIndex = 0;

        ReadOnlySpan<char> currentSubset = null;
        for (var index = 0; index < span.Length; index++)
        {
            var range = previousTokenIndex..(index + 1);
            currentSubset = span[range];

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
                if (possibility > 0)
                {
                    nextPossibleTokens.Add(pt with { possibility = possibility });
                }
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
                    currentSubset = [];
                    continue;
                }
            }

            if (currentPossibleTokens.Length == 0)
            {
                tokens.Add(new Unknown(range));
            }

            if (tokens.Count != 0 && tokens[^1] is Unknown)
            {
                throw new UnknownTokenException(hLinqQuery,
                [
                    new Where(default), new Select(default), new Skip(default), new Take(default), new OrderBy(default),
                    new OrderByDescending(default)
                ]);
            }
        }

        if (hLinqQuery.Length > 0 && currentSubset.Length > 0 && !currentSubset.IsWhiteSpace())
        {
            throw new UnknownTokenException(hLinqQuery,
                nextPossibleTokens.Select(npt => npt.token.Build(default)).ToArray());
        }

        return tokens.ToArray();
    }

    public sealed class UnknownTokenException(string queryString, IToken[] expectedTokens)
        : InvalidTokenCollectionException(queryString, [], expectedTokens.Take(1).ToArray(),
            expectedTokens.Skip(1).Select(t => new[] { t }).ToArray());
}
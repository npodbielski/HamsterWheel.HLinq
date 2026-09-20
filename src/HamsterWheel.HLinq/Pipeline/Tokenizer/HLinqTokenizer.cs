using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Tokens;

namespace HamsterWheel.HLinq.Pipeline.Tokenizer;

/// <summary>
///     Single left-to-right walk over the query, no backtracking and no syntax peeking.
///     For each window (the text after the last committed token) the adjacency rule of every
///     possibility (<see cref="IHLinqTokenPossibility.MatchesPreviousTokens" />) is evaluated
///     once - it cannot change while the window grows - and every window growth then asks only
///     its text rule (<see cref="IHLinqTokenPossibility.MatchAt" />). The first possibility
///     (in registration order) that completes at an offset commits its token and the walk jumps
///     to that offset; windows that match nothing throw. Ambiguity is resolved by the token
///     grammars themselves, never by the tokenizer looking at surrounding syntax.
/// </summary>
public sealed partial class HLinqTokenizer : IHLinqTokenizer
{
    private readonly IHLinqTokenPossibility[] _allPossibilities;

    private readonly IHLinqTokenPossibility[] _startingPossibilities;

    public HLinqTokenizer(IEnumerable<IHLinqTokenPossibility> tokenPossibilities)
    {
        _allPossibilities = [.. tokenPossibilities];
        _startingPossibilities = [.. _allPossibilities.Where(t => t.CanBeFirst)];
    }

    public IToken[] Tokenize(string hLinqQuery)
    {
        var span = hLinqQuery.AsSpan();
        List<IToken> tokens = [];
        var candidates = new IHLinqTokenPossibility[_allPossibilities.Length];
        var candidateCount = 0;
        var possible = new IHLinqTokenPossibility[_allPossibilities.Length];
        var possibleCount = 0;
        var previousTokenIndex = 0;
        var candidatesValid = false;
        var windowHasText = false;

        for (var index = 0; index < span.Length; index++)
        {
            var window = span.Slice(previousTokenIndex, index - previousTokenIndex + 1);
            if (window.IsWhiteSpace())
            {
                //whitespace never starts a window, slide the window start over it
                previousTokenIndex++;
                candidatesValid = false;
                continue;
            }

            windowHasText = true;

            if (!candidatesValid)
            {
                candidateCount = 0;
                foreach (var possibility in tokens.Count == 0 ? _startingPossibilities : _allPossibilities)
                {
                    if (possibility.MatchesPreviousTokens(tokens))
                    {
                        candidates[candidateCount++] = possibility;
                    }
                }

                candidatesValid = true;
            }

            var next = NextNonWhiteSpace(span, index);
            possibleCount = 0;
            IHLinqTokenPossibility? winner = null;
            var completeAt = 0;
            for (var candidate = 0; candidate < candidateCount; candidate++)
            {
                var match = candidates[candidate].MatchAt(window, next);
                if (match.IsDiscarded)
                {
                    continue;
                }

                possible[possibleCount++] = candidates[candidate];
                if (match.IsCompleted && winner is null)
                {
                    winner = candidates[candidate];
                    completeAt = match.CompleteAt;
                }
            }

            if (winner is null)
            {
                continue;
            }

            if (completeAt == 0)
            {
                throw new ZeroLengthCompletionException(winner.ForType.Name);
            }

            // commit [previousTokenIndex, previousTokenIndex + completeAt) and continue the walk
            // right behind the completed match - characters it did not cover open the next window
            var tokenEnd = previousTokenIndex + completeAt;
            tokens.Add(winner.Build(GetRange(previousTokenIndex..tokenEnd)));
            previousTokenIndex = tokenEnd;
            index = tokenEnd - 1;
            candidatesValid = false;
            windowHasText = false;
            possibleCount = 0;
        }

        if (windowHasText)
        {
            throw new UnknownTokenException(hLinqQuery,
                possible.Take(possibleCount).Select(npt => npt.Build(default)).ToArray());
        }

        return [.. tokens];
    }

    private static char? NextNonWhiteSpace(ReadOnlySpan<char> span, int windowEnd)
    {
        var nextCharIndex = windowEnd;
        char? next;
        do
        {
            next = nextCharIndex + 1 < span.Length ? span[nextCharIndex + 1] : null;
            nextCharIndex++;
        } while (next is not null && char.IsWhiteSpace(next.Value));

        return next;
    }

}

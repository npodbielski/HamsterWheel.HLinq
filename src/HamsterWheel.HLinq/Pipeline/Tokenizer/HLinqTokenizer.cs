using HamsterWheel.HLinq.Tokens;

namespace HamsterWheel.HLinq.Pipeline.Tokenizer;

/// <summary>
///     Single left-to-right walk over the query, no backtracking and no syntax peeking.
///     The walk knows which clause it is in: possibilities are split per clause once, at construction,
///     and committing a clause root (<see cref="IClauseRootToken" />) switches the walk to the
///     possibilities of that clause - a token of another clause is never even asked. Adjacency is
///     then the only question left for the possibilities of the current clause.
///     For each window (the text after the last committed token) the adjacency rule of every
///     remaining possibility (<see cref="IHLinqTokenPossibility.MatchesPreviousTokens" />) is
///     evaluated once - it cannot change while the window grows - and every window growth then asks
///     only its text rule (<see cref="IHLinqTokenPossibility.MatchAt" />). The first possibility
///     (in registration order) that completes at an offset commits its token and the walk jumps
///     to that offset; windows that match nothing throw. Ambiguity is resolved by the token
///     grammars themselves, never by the tokenizer looking at surrounding syntax.
/// </summary>
public sealed partial class HLinqTokenizer : IHLinqTokenizer
{
    private readonly IHLinqTokenPossibility[] _allPossibilities;

    // possibilities that may stand in a clause opened by a given root token, built once: which clause
    // a token belongs to cannot change while a query is tokenized, so the walk switches this array
    // instead of asking every possibility where it is allowed - the fewer possibilities it asks, the
    // less work the walk does
    private readonly Dictionary<Type, IHLinqTokenPossibility[]> _possibilitiesByClauseRoot;

    // possibilities allowed before any clause root was committed (the empty-clause context, which is
    // also what the grammar self-check evaluates rules against)
    private readonly IHLinqTokenPossibility[] _outsideOfAnyClause;

    private readonly IHLinqTokenPossibility[] _startingPossibilities;

    public HLinqTokenizer(IEnumerable<IHLinqTokenPossibility> tokenPossibilities)
    {
        _allPossibilities = [.. tokenPossibilities];

        var clauseRoots = _allPossibilities
            .Select(p => p.ForType)
            .Where(t => typeof(IClauseRootToken).IsAssignableFrom(t))
            .Distinct()
            .ToArray();

        _possibilitiesByClauseRoot = clauseRoots.ToDictionary(
            root => root,
            root => _allPossibilities.Where(p => p.MatchesClauseRoot(root)).ToArray());

        _outsideOfAnyClause = [.. _allPossibilities.Where(p => p.MatchesClauseRoot(null))];
        _startingPossibilities = [.. _outsideOfAnyClause.Where(t => t.CanBeFirst)];
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
        var clausePossibilities = _outsideOfAnyClause;

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
                foreach (var possibility in tokens.Count == 0 ? _startingPossibilities : clausePossibilities)
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
                if (!match.IsCompleted || winner is not null)
                {
                    continue;
                }

                winner = candidates[candidate];
                completeAt = match.CompleteAt;
            }

            if (winner is null)
            {
                continue;
            }

            if (completeAt == 0)
            {
                throw new ZeroLengthCompletionException(winner.ForType.Name);
            }

            var tokenEnd = previousTokenIndex + completeAt;
            var token = winner.Build(GetRange(previousTokenIndex..tokenEnd));
            tokens.Add(token);
            if (token is IClauseRootToken clauseRoot)
            {
                if (!_possibilitiesByClauseRoot.TryGetValue(clauseRoot.GetType(), out var clause))
                {
                    clause = _allPossibilities;
                }

                clausePossibilities = clause;
            }

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

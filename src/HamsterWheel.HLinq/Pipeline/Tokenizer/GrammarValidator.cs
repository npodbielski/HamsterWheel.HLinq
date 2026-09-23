using System.Collections.Concurrent;
using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Tokens;

namespace HamsterWheel.HLinq.Pipeline.Tokenizer;

internal class GrammarValidator(IGrammar grammar, GrammarDependencies dependencies)
{
    private static readonly ConcurrentDictionary<Type, IToken> Markers = new();

    private Grammar Grammar => (Grammar)grammar;

    private void ValidateReachability(IHLinqTokenPossibility[] possibilities, List<string> problems)
    {
        var reachable = new HashSet<Type>(possibilities
            .Where(p => Grammar.Rules.First(r => r.ForType == p.ForType).CanBeFirst)
            .Select(p => p.ForType));

        var grew = true;
        while (grew)
        {
            grew = false;
            foreach (var possibility in possibilities.Where(p => !reachable.Contains(p.ForType)).ToArray())
            {
                if (!Contexts(reachable).Any(c => possibility.MatchesPreviousTokens(c)))
                {
                    continue;
                }

                reachable.Add(possibility.ForType);
                grew = true;
            }
        }

        foreach (var unreachable in possibilities.Select(p => p.ForType).Where(t => !reachable.Contains(t)))
        {
            problems.Add($"token '{unreachable.Name}' is unreachable from the start of a query");
        }
    }

    private void ValidateKeywordCollisions(IHLinqTokenPossibility[] possibilities, List<string> problems)
    {
        var keywords = possibilities.Where(p => p.Keyword is not null).ToArray();
        var allContexts = Contexts([.. Grammar.Rules.Select(r => r.ForType)]).ToList();

        foreach (var shorter in keywords)
        {
            foreach (var longer in keywords)
            {
                if (shorter == longer || longer.Keyword!.Length <= shorter.Keyword!.Length
                                      || !longer.Keyword.StartsWith(shorter.Keyword,
                                          StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var swallowed = shorter.MatchAt(shorter.Keyword.AsSpan(), longer.Keyword[shorter.Keyword.Length]);
                //the shorter keyword does not finish before the longer one would continue
                if (!swallowed.IsCompleted)
                {
                    continue;
                }

                //they never compete in the same place...
                if (allContexts.All(c => !(shorter.MatchesPreviousTokens(c) && longer.MatchesPreviousTokens(c))))
                {
                    continue;
                }

                //...nor in the same clause, which is how `=` (rename, select only) and `==` (compare,
                //everywhere else) can both be keywords without either carrying a next-character rule
                if (!GrammarValidator.ClauseRootsOf(possibilities)
                        .Any(root => shorter.MatchesClauseRoot(root) && longer.MatchesClauseRoot(root)))
                {
                    continue;
                }

                problems.Add(
                    $"keyword '{shorter.Keyword}' completes where '{longer.Keyword}' could still continue and both " +
                    $"are allowed in the same place and clause - add a next-character rule to " +
                    $"'{shorter.ForType.Name}' or give the two different clauses");
            }
        }
    }

    /// <summary>
    ///     Every token list of up to two markers built from the given token types - enough to
    ///     evaluate any adjacency matcher against a synthetic query ending in those types.
    /// </summary>
    private static IEnumerable<List<IToken>> Contexts(HashSet<Type> tokenTypes)
    {
        yield return [];
        var markers = tokenTypes.Select(MarkerOf).ToArray();
        foreach (var one in markers)
        {
            yield return [one];
        }

        foreach (var first in markers)
        {
            foreach (var second in markers)
            {
                yield return [second, first];
            }
        }
    }

    /// <summary>
    ///     Validates the effective grammar - the rules together with the registered
    ///     possibilities, so a localization replacing a token is validated as it will run.
    ///     Throws <see cref="Exceptions.HLinqQueryException" /> listing every problem found:
    ///     a token without exactly one rule, a token unreachable from the start of a query,
    ///     or a keyword that swallows a longer keyword it can be confused with.
    /// </summary>
    public void Validate()
    {
        var problems = new List<string>();
        var possibilities = dependencies.TokenPossibilities.ToArray();

        foreach (var duplicate in Grammar.Rules.GroupBy(r => r.ForType).Where(g => g.Count() > 1))
        {
            problems.Add(
                $"token '{duplicate.Key.Name}' has {duplicate.Count()} grammar rules, exactly one is expected");
        }

        foreach (var possibility in possibilities)
        {
            if (Grammar.Rules.All(r => r.ForType != possibility.ForType))
            {
                problems.Add($"token '{possibility.ForType.Name}' has no grammar rule");
            }
        }

        foreach (var rule in Grammar.Rules)
        {
            if (possibilities.All(p => p.ForType != rule.ForType))
            {
                problems.Add($"grammar rule for '{rule.ForType.Name}' has no registered token possibility");
            }
        }

        if (problems.Count > 0)
        {
            throw new GrammarValidationException(problems);
        }

        ValidateReachability(possibilities, problems);
        ValidateKeywordCollisions(possibilities, problems);

        if (problems.Count > 0)
        {
            throw new GrammarValidationException(problems);
        }
    }

    /// <summary>
    ///     The clauses a keyword can be asked about: no clause at all, and every clause root the
    ///     registered tokens know. Two keywords that are never legal in one clause cannot compete
    ///     over the same characters.
    /// </summary>
    private static IEnumerable<Type?> ClauseRootsOf(IHLinqTokenPossibility[] possibilities) =>
        new[] { (Type?)null }
            .Concat(possibilities.Select(p => p.ForType)
                .Where(t => typeof(IClauseRootToken).IsAssignableFrom(t)))
            .Distinct();

    public static IToken MarkerOf(Type tokenType) =>
        Markers.GetOrAdd(tokenType, t => (IToken)Activator.CreateInstance(t)!);

    public sealed class UnknownTokenTypeException<T>()
        : HLinqQueryException($"Unknown token type: '{typeof(T).Name}'. This token is not supported by HLinq grammar");
}
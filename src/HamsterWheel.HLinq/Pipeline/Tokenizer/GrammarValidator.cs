using HamsterWheel.HLinq.Tokens;

namespace HamsterWheel.HLinq.Pipeline.Tokenizer;

/// <summary>
/// Validates grammar for correctness: if token is possible to be produced, if tokens
/// </summary>
internal class GrammarValidator(IGrammar grammar, IHLinqWord[] allWords)
{
    private IGrammarRule RuleOf(IHLinqWord word) => grammar.GetRuleFor(word.ForType);

    private void ValidateReachability(IHLinqWord[] words, List<string> problems)
    {
        var reachable = new HashSet<Type>(words
            .Where(p => grammar.Rules.First(r => r.ForType == p.ForType).CanBeFirst)
            .Select(p => p.ForType));

        var grew = true;
        while (grew)
        {
            grew = false;
            foreach (var word in words.Where(p => !reachable.Contains(p.ForType)).ToArray())
            {
                if (!Contexts(reachable).Any(c => RuleOf(word).PreviousTokensMatch(c)))
                {
                    continue;
                }

                reachable.Add(word.ForType);
                grew = true;
            }
        }

        foreach (var unreachable in words.Select(p => p.ForType).Where(t => !reachable.Contains(t)))
        {
            problems.Add($"token '{unreachable.Name}' is unreachable from the start of a query");
        }
    }

    private void ValidateKeywordCollisions(IHLinqWord[] words, List<string> problems)
    {
        var keywords = words.Where(p => p.Keyword is not null).ToArray();
        var allContexts = Contexts([.. grammar.Rules.Select(r => r.ForType)]).ToList();

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

                var swallowed = shorter.PatternFor(grammar)
                    .Match(shorter.Keyword.AsSpan(), longer.Keyword[shorter.Keyword.Length]);
                //the shorter keyword does not finish before the longer one would continue
                if (!swallowed.IsCompleted)
                {
                    continue;
                }

                //they never compete in the same place...
                if (allContexts.All(c =>
                        !(RuleOf(shorter).PreviousTokensMatch(c) && RuleOf(longer).PreviousTokensMatch(c))))
                {
                    continue;
                }

                //...nor in the same clause, which is how `=` (rename, select only) and `==` (compare,
                //everywhere else) can both be keywords without either carrying a next-character rule
                if (!ClauseRootsOf(words)
                        .Any(root => RuleOf(shorter).MatchesClauseRoot(root) && RuleOf(longer).MatchesClauseRoot(root)))
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
    /// Every token list of up to two markers built from the given token types - enough to
    /// evaluate any adjacency matcher against a synthetic query ending in those types.
    /// </summary>
    private static IEnumerable<List<IToken>> Contexts(HashSet<Type> tokenTypes)
    {
        yield return [];
        var markers = tokenTypes.Select(Grammar.MarkerOf).ToArray();
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
    /// Every way this language is wrong: one rule per token, every token reachable from the start of
    /// a query, no keyword swallowing a longer one it competes with in a place and clause they share.
    /// Empty means the language can tokenize queries.
    /// </summary>
    private List<string> Problems()
    {
        var problems = new List<string>();

        foreach (var duplicate in grammar.Rules.GroupBy(r => r.ForType).Where(g => g.Count() > 1))
        {
            problems.Add(
                $"token '{duplicate.Key.Name}' has {duplicate.Count()} grammar rules, exactly one is expected");
        }

        foreach (var word in allWords)
        {
            if (grammar.Rules.All(r => r.ForType != word.ForType))
            {
                problems.Add($"token '{word.ForType.Name}' has no grammar rule");
            }
        }

        foreach (var rule in grammar.Rules)
        {
            if (allWords.All(p => p.ForType != rule.ForType))
            {
                problems.Add($"grammar rule for '{rule.ForType.Name}' has no registered token word");
            }
        }

        //reachability and collisions ask each word's rule, which only makes sense once every word
        //has exactly one
        if (problems.Count > 0)
        {
            return problems;
        }

        ValidateReachability(allWords, problems);
        ValidateKeywordCollisions(allWords, problems);

        return problems;
    }

    /// <summary>
    /// The grammar of every language of this tokenizer - the invariant one and one per culture its
    /// words name - checked in order, the problems of all of them collected into one answer.
    /// </summary>
    public static void Validate(IHLinqTokenizer tokenizer)
    {
        var validators = ((HLinqTokenizer)tokenizer).GetLanguages()
            .Select(l => new GrammarValidator(l.Grammar, l.Grammar.Words));
        var problems = new List<string>();
        foreach (var validator in validators)
        {
            problems.AddRange(validator.Problems());
        }

        if (problems.Count > 0)
        {
            throw new GrammarValidationException(problems);
        }
    }

    private static IEnumerable<Type?> ClauseRootsOf(IHLinqWord[] words) =>
        new[] { (Type?)null }
            .Concat(words.Select(p => p.ForType)
                .Where(t => typeof(IClauseRootToken).IsAssignableFrom(t)))
            .Distinct();
}
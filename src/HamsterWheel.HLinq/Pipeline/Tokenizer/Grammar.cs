using System.Collections.Concurrent;
using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Filtering;
using HamsterWheel.HLinq.Tokens.Ordering;
using HamsterWheel.HLinq.Tokens.Paging;
using HamsterWheel.HLinq.Tokens.Selecting;
using Microsoft.Extensions.DependencyInjection;

namespace HamsterWheel.HLinq.Pipeline.Tokenizer;

public class Grammar(IServiceProvider serviceProvider) : IGrammar
{
    private static readonly ConcurrentDictionary<Type, IToken> Markers = new();

    private IEnumerable<IHLinqTokenPossibility>? _tokenPossibilities;
    private IGrammarRule[]? _rules;

    private IEnumerable<IHLinqTokenPossibility> TokenPossibilities =>
        _tokenPossibilities ??= serviceProvider.GetServices<IHLinqTokenPossibility>();

    private string EqualityTokenKeyword =>
        KeywordOf(typeof(Equality)) ?? throw new UnknownTokenTypeException<Equality>();

    private string LeftSquareBracketTokenKeyword =>
        KeywordOf(typeof(LeftSquareBracket)) ?? throw new UnknownTokenTypeException<LeftSquareBracket>();

    /// <summary>
    ///     The whole grammar, one rule per token, adjacency declared only as matchers over the
    ///     tokens committed so far: <c>After(typeof(NameOrValue))</c> means "directly after a
    ///     value", a pattern like <c>[.., RightSquareBracket, Dot]</c> means "after the closing
    ///     bracket of one clause and the chain dot". Which characters end a free text token are
    ///     the first characters of the keywords of the tokens these same matchers allow to
    ///     follow, see <see cref="GetDelimiters" />.
    /// </summary>
    private IGrammarRule[] Rules => _rules ??=
    [
        //roots
        new GrammarRule<Select>(true, [AfterChain.RightSquareBracketThenDot],
            nextCharMatcher: c => c == LeftSquareBracketTokenKeyword[0]),
        new GrammarRule<Where>(true, [AfterChain.RightSquareBracketThenDot],
            nextCharMatcher: c => c == LeftSquareBracketTokenKeyword[0]),
        new GrammarRule<OrderBy>(true, [AfterChain.RightSquareBracketThenDot],
            nextCharMatcher: c => c == LeftSquareBracketTokenKeyword[0]),
        new GrammarRule<OrderByDescending>(true, [AfterChain.RightSquareBracketThenDot],
            nextCharMatcher: c => c == LeftSquareBracketTokenKeyword[0]),
        new GrammarRule<Count>(true, [AfterChain.RightSquareBracketThenDot],
            nextCharMatcher: c => c == LeftSquareBracketTokenKeyword[0]),
        new GrammarRule<Skip>(true, [AfterChain.RightSquareBracketThenDot],
            nextCharMatcher: c => c == LeftSquareBracketTokenKeyword[0]),
        new GrammarRule<Take>(true, [AfterChain.RightSquareBracketThenDot],
            nextCharMatcher: c => c == LeftSquareBracketTokenKeyword[0]),
        new GrammarRule<ThenBy>(false, [AfterChain.RightSquareBracketThenDot],
            nextCharMatcher: c => c == LeftSquareBracketTokenKeyword[0]),
        new GrammarRule<ThenByDescending>(false, [AfterChain.RightSquareBracketThenDot],
            nextCharMatcher: c => c == LeftSquareBracketTokenKeyword[0]),

        new GrammarRule<PropertyName>(false,
            [pt => pt is [.., Entity, Dot] or [.., PropertyName, Dot]]),
        new GrammarRule<NameOrValue>(false,
            [
                pt => pt is [.., _, IComparisonToken or LeftCircleBracket or LeftSquareBracket or Comma or Assignment],
                // end side of take[start..end] / take[..end] range
                pt => pt is [.., RangeOperator]
            ]),
        // the range operator matches the number in front of it (`10..`), so it is only ever
        // possible directly after the `[` of take/skip
        new GrammarRule<RangeOperator>(false, [pt => pt is [.., LeftSquareBracket]]),
        // until the grammar tells the select root (`=` renames) from the where root (`==` compares),
        // a second dot-free `=` must not let an Assignment swallow the start of an Equality
        new GrammarRule<Assignment>(false, [After(typeof(NameOrValue))],
            nextCharMatcher: c => c != EqualityTokenKeyword[0]),
        new GrammarRule<And>(false, [After(typeof(NameOrValue), typeof(PropertyName), typeof(RightCircleBracket))]),
        new GrammarRule<Or>(false, [After(typeof(NameOrValue), typeof(PropertyName), typeof(RightCircleBracket))]),
        new GrammarRule<Entity>(false,
            [
                After(typeof(LeftSquareBracket), typeof(And), typeof(Or), typeof(Comma), typeof(LeftCircleBracket),
                    typeof(Assignment))
            ]),
        new GrammarRule<Comma>(false, [After(typeof(NameOrValue), typeof(PropertyName))]),
        new GrammarRule<Dot>(false, [After(typeof(Entity), typeof(PropertyName), typeof(RightSquareBracket))]),
        new GrammarRule<Equality>(false, [After(typeof(NameOrValue), typeof(PropertyName), typeof(Entity))]),
        new GrammarRule<Inequality>(false, [After(typeof(NameOrValue), typeof(PropertyName), typeof(Entity))]),
        new GrammarRule<LessOrEqualThan>(false, [After(typeof(NameOrValue), typeof(PropertyName), typeof(Entity))]),
        new GrammarRule<LessThan>(false, [After(typeof(NameOrValue), typeof(PropertyName), typeof(Entity))],
            nextCharMatcher: c => c != EqualityTokenKeyword[0]),
        new GrammarRule<GreaterOrEqualThan>(false, [After(typeof(NameOrValue), typeof(PropertyName), typeof(Entity))]),
        new GrammarRule<GreaterThan>(false, [After(typeof(NameOrValue), typeof(PropertyName), typeof(Entity))],
            nextCharMatcher: c => c != EqualityTokenKeyword[0]),
        new GrammarRule<LeftCircleBracket>(false,
            [After(typeof(LeftSquareBracket), typeof(And), typeof(Or), typeof(MethodName), typeof(LeftCircleBracket))]),
        new GrammarRule<RightCircleBracket>(false,
            [After(typeof(NameOrValue), typeof(PropertyName), typeof(RightCircleBracket))]),
        new GrammarRule<LeftSquareBracket>(false,
            [
                After(typeof(Where), typeof(Skip), typeof(Take), typeof(OrderBy), typeof(OrderByDescending),
                    typeof(ThenBy), typeof(ThenByDescending), typeof(Select), typeof(Count))
            ]),
        new GrammarRule<RightSquareBracket>(false,
            [
                After(typeof(PropertyName), typeof(NameOrValue), typeof(RightCircleBracket),
                    typeof(LeftSquareBracket), typeof(RangeOperator))
            ]),
        new GrammarRule<MethodName>(false, [After(typeof(Dot), typeof(LeftSquareBracket))]),
    ];

    /// <summary>
    ///     The characters that may directly follow <paramref name="type" />: the first characters
    ///     of the keywords of every token whose adjacency rule accepts a query ending in it.
    /// </summary>
    public char[] GetDelimiters(Type type)
    {
        IToken[] marker = [MarkerOf(type)];
        return Rules
            .Where(r => r.PreviousTokensMatchers.Any(m => m(marker)))
            .Select(r => KeywordOf(r.ForType))
            .Where(k => k is not null)
            .Cast<string>()
            .Select(k => k[0])
            .Distinct()
            .ToArray();
    }

    public IGrammarRule GetRuleFor<TToken>() => Rules.FirstOrDefault(t => t.IsFor<TToken>()) ??
                                                throw new UnknownTokenTypeException<TToken>();

    public void Validate()
    {
        var problems = new List<string>();
        var possibilities = TokenPossibilities.ToArray();

        foreach (var duplicate in Rules.GroupBy(r => r.ForType).Where(g => g.Count() > 1))
        {
            problems.Add($"token '{duplicate.Key.Name}' has {duplicate.Count()} grammar rules, exactly one is expected");
        }

        foreach (var possibility in possibilities)
        {
            if (Rules.All(r => r.ForType != possibility.ForType))
            {
                problems.Add($"token '{possibility.ForType.Name}' has no grammar rule");
            }
        }

        foreach (var rule in Rules)
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

    private void ValidateReachability(IHLinqTokenPossibility[] possibilities, List<string> problems)
    {
        var reachable = new HashSet<Type>(possibilities
            .Where(p => Rules.First(r => r.ForType == p.ForType).CanBeFirst)
            .Select(p => p.ForType));

        var grew = true;
        while (grew)
        {
            grew = false;
            foreach (var possibility in possibilities.Where(p => !reachable.Contains(p.ForType)).ToArray())
            {
                if (Contexts(reachable).Any(c => possibility.MatchesPreviousTokens(c)))
                {
                    reachable.Add(possibility.ForType);
                    grew = true;
                }
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
        var allContexts = Contexts(new HashSet<Type>(Rules.Select(r => r.ForType))).ToList();

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
                //they never compete in the same place
                if (allContexts.All(c => !(shorter.MatchesPreviousTokens(c) && longer.MatchesPreviousTokens(c))))
                {
                    continue;
                }

                problems.Add(
                    $"keyword '{shorter.Keyword}' completes where '{longer.Keyword}' could still continue and both " +
                    $"are allowed in the same place - add a next-character rule to '{shorter.ForType.Name}'");
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
            yield return new List<IToken> { one };
        }

        foreach (var first in markers)
        {
            foreach (var second in markers)
            {
                yield return new List<IToken> { second, first };
            }
        }
    }

    private string? KeywordOf(Type tokenType) =>
        TokenPossibilities.FirstOrDefault(p => p.ForType == tokenType)?.Keyword;

    private static IToken MarkerOf(Type tokenType) =>
        Markers.GetOrAdd(tokenType, t => (IToken)Activator.CreateInstance(t)!);

    private static Func<IReadOnlyList<IToken>, bool> After(params Type[] tokenTypes) =>
        tokens => tokens.Count > 0 && Array.IndexOf(tokenTypes, tokens[^1].GetType()) >= 0;

    private static class AfterChain
    {
        public static bool RightSquareBracketThenDot(IReadOnlyList<IToken> tokens) =>
            tokens is [.., RightSquareBracket, Dot];
    }

    private sealed class UnknownTokenTypeException<T>()
        : HLinqQueryException($"Unknown token type: '{typeof(T).Name}'. This token is not supported by HLinq grammar");
}

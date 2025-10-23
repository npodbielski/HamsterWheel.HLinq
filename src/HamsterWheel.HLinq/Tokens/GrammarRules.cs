using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Tokens.Filtering;
using HamsterWheel.HLinq.Tokens.Ordering;
using HamsterWheel.HLinq.Tokens.Paging;
using HamsterWheel.HLinq.Tokens.Selecting;

namespace HamsterWheel.HLinq.Tokens;

public static class GrammarRules
{
    private static readonly List<IGrammarRule> Rules =
    [
        new GrammarRule<Select>(true, previousTokensMatchers: [pt => pt is [.., RightSquareBracket, Dot]]),
        new GrammarRule<Where>(true, previousTokensMatchers: [pt => pt is [.., RightSquareBracket, Dot]]),
        new GrammarRule<OrderBy>(true, previousTokensMatchers: [pt => pt is [.., RightSquareBracket, Dot]]),
        new GrammarRule<OrderByDescending>(true, previousTokensMatchers: [pt => pt is [.., RightSquareBracket, Dot]]),
        new GrammarRule<Count>(true, previousTokensMatchers: [pt => pt is [.., RightSquareBracket, Dot]]),
        new GrammarRule<Skip>(true, previousTokensMatchers: [pt => pt is [.., RightSquareBracket, Dot]]),
        new GrammarRule<Take>(true, previousTokensMatchers: [pt => pt is [.., RightSquareBracket, Dot]]),
        new GrammarRule<ThenBy>(false, previousTokensMatchers: [pt => pt is [.., RightSquareBracket, Dot]]),
        new GrammarRule<ThenByDescending>(false, previousTokensMatchers: [pt => pt is [.., RightSquareBracket, Dot]]),
        new GrammarRule<PropertyAccess>(false,
            previousTokensMatchers: [pt => pt is [.., Entity, Dot] or [.., PropertyAccess, Dot]]),
        new GrammarRule<NameOrValue>(false,
            previousTokensMatchers:
            [
                pt => pt is [.., _, IComparisonToken or LeftCircleBracket or LeftSquareBracket or Comma or Assignment]
            ]),
        new GrammarRule<Assignment>(false, [typeof(NameOrValue)]),
        new GrammarRule<And>(false, [typeof(NameOrValue), typeof(PropertyAccess), typeof(RightCircleBracket)]),
        new GrammarRule<Or>(false, [typeof(NameOrValue), typeof(PropertyAccess), typeof(RightCircleBracket)]),
        new GrammarRule<Entity>(false,
        [
            typeof(LeftSquareBracket), typeof(And), typeof(Or), typeof(Comma), typeof(LeftCircleBracket),
            typeof(Assignment)
        ]),
        new GrammarRule<Comma>(false, [typeof(NameOrValue), typeof(PropertyAccess)]),
        new GrammarRule<Dot>(false, [typeof(Entity), typeof(PropertyAccess), typeof(RightSquareBracket)]),
        new GrammarRule<Equality>(false, [typeof(NameOrValue), typeof(PropertyAccess)]),
        new GrammarRule<Inequality>(false, [typeof(NameOrValue), typeof(PropertyAccess)]),
        new GrammarRule<LessOrEqualThan>(false, [typeof(NameOrValue), typeof(PropertyAccess)]),
        new GrammarRule<LessThan>(false, [typeof(NameOrValue), typeof(PropertyAccess)]),
        new GrammarRule<GreaterOrEqualThan>(false, [typeof(NameOrValue), typeof(PropertyAccess)]),
        new GrammarRule<GreaterThan>(false, [typeof(NameOrValue), typeof(PropertyAccess)]),
        new GrammarRule<LeftCircleBracket>(false,
            [typeof(LeftSquareBracket), typeof(And), typeof(Or), typeof(MethodCall), typeof(LeftCircleBracket)]),
        new GrammarRule<Or>(false, [typeof(NameOrValue), typeof(PropertyAccess), typeof(RightCircleBracket)]),
        new GrammarRule<RightCircleBracket>(false,
            [typeof(NameOrValue), typeof(PropertyAccess), typeof(RightCircleBracket)]),
        new GrammarRule<Dot>(false, [typeof(Entity), typeof(PropertyAccess), typeof(RightSquareBracket)]),
        new GrammarRule<LeftSquareBracket>(false, [
            typeof(Where), typeof(Skip), typeof(Take), typeof(OrderBy), typeof(OrderByDescending), typeof(ThenBy),
            typeof(ThenByDescending), typeof(Select), typeof(Count)
        ]),
        new GrammarRule<RightSquareBracket>(false,
            [typeof(PropertyAccess), typeof(NameOrValue), typeof(RightCircleBracket), typeof(LeftSquareBracket)]),
        new GrammarRule<MethodCall>(false, [typeof(Dot), typeof(LeftSquareBracket)]),
    ];

    public static bool CanBeFirst<TToken>() => GetRuleFor<TToken>().CanBeFirst;

    public static bool PreviousTokensMatch<TToken>(List<IToken> previousTokens)
    {
        var rule = GetRuleFor<TToken>();
        return previousTokens.Count == 0 && rule.CanBeFirst
               || rule.PreviousTokensMatchers.Length > 0 && rule.PreviousTokensMatchers.Any(m => m(previousTokens));
    }

    public static bool PreviousTokenMatch<TToken>(IToken previousToken) => GetRuleFor<TToken>().CanBeAfter.Contains(previousToken.GetType());

    private static IGrammarRule GetRuleFor<TToken>() => Rules.FirstOrDefault(t => t.IsFor<TToken>()) ?? throw new UnknownTokenTypeException<TToken>();

    public static bool NextCharIsAllowed<TToken>(char? next)
    {
        if (typeof(TToken) == typeof(GreaterThan))
        {
            return next switch
            {
                '=' => false,
                _ => true
            };
        }

        if (typeof(TToken) == typeof(ThenBy)
            || typeof(TToken) == typeof(ThenByDescending))
        {
            return next == LeftSquareBracket.Possibility.TokenValue.AsSpan()[0];
        }

        return true;
    }

    private sealed class UnknownTokenTypeException<T>()
        : HLinqQueryException($"Unknown token type: '{typeof(T).Name}'. This token is not supported by HLinq grammar");
}

public class GrammarRule<T>(
    bool canBeFirst,
    Type[]? canBeAfter = null,
    Func<List<IToken>, bool>[]? previousTokensMatchers = null)
    : IGrammarRule
{
    public bool IsFor<TToken>() => typeof(TToken) == typeof(T);
    public bool CanBeFirst => canBeFirst;

    public Func<List<IToken>, bool>[] PreviousTokensMatchers { get; } = previousTokensMatchers ?? [];
    public Type[] CanBeAfter { get; } = canBeAfter ?? [];
}

public interface IGrammarRule
{
    bool IsFor<TToken>();
    bool CanBeFirst { get; }

    Func<List<IToken>, bool>[] PreviousTokensMatchers { get; }
    Type[] CanBeAfter { get; }
}
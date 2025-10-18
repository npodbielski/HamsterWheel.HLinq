using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Tokens.Filtering;
using HamsterWheel.HLinq.Tokens.Ordering;
using HamsterWheel.HLinq.Tokens.Paging;
using HamsterWheel.HLinq.Tokens.Selecting;

namespace HamsterWheel.HLinq.Tokens;

public static class GrammarRules
{
    public static bool SelectPreviousTokensMatch<TToken>(List<IToken> previousTokens)
    {
        if (typeof(TToken) == typeof(Select)
            || typeof(TToken) == typeof(Where)
            || typeof(TToken) == typeof(OrderBy)
            || typeof(TToken) == typeof(OrderByDescending)
            || typeof(TToken) == typeof(ThenBy)
            || typeof(TToken) == typeof(ThenByDescending)
            || typeof(TToken) == typeof(Count)
            || typeof(TToken) == typeof(Skip)
            || typeof(TToken) == typeof(Take)
           )
        {
            return previousTokens.Count == 0 || previousTokens is [.., RightSquareBracket, Dot];
        }

        if (typeof(TToken) == typeof(PropertyAccess))
        {
            return previousTokens.Count >= 4 &&
                   previousTokens is [.., Entity, Dot] or [.., PropertyAccess, Dot];
        }

        if (typeof(TToken) == typeof(LeftSquareBracket))
        {
            return previousTokens is
                [.., _, IComparisonToken or LeftCircleBracket or LeftSquareBracket or Comma or Assignment];
        }

        throw new UnknownTokenTypeException<TToken>();
    }

    public static bool SelectPreviousTokenMatch<TToken>(IToken previousToken)
    {
        if (typeof(TToken) == typeof(Assignment))
        {
            return previousToken is NameOrValue;
        }

        if (typeof(TToken) == typeof(And))
        {
            return previousToken is NameOrValue or PropertyAccess or RightCircleBracket;
        }

        if (typeof(TToken) == typeof(Comma))
        {
            return previousToken is NameOrValue or PropertyAccess;
        }

        if (typeof(TToken) == typeof(Equality)
            || typeof(TToken) == typeof(GreaterOrEqualThan)
            || typeof(TToken) == typeof(GreaterThan)
            || typeof(TToken) == typeof(Inequality))
        {
            return previousToken is PropertyAccess or NameOrValue;
        }

        if (typeof(TToken) == typeof(LeftCircleBracket))
        {
            return previousToken is LeftSquareBracket or And or Or or MethodCall or LeftCircleBracket;
        }

        if (typeof(TToken) == typeof(LessOrEqualThan)
            || typeof(TToken) == typeof(LessThan))
        {
            return previousToken is PropertyAccess or RightSquareBracket;
        }

        if (typeof(TToken) == typeof(LeftCircleBracket))
        {
            return previousToken is Dot or LeftSquareBracket;
        }

        if (typeof(TToken) == typeof(Or))
        {
            return previousToken is NameOrValue or PropertyAccess or RightCircleBracket;
        }

        if (typeof(TToken) == typeof(RightCircleBracket))
        {
            return previousToken is PropertyAccess or NameOrValue or RightCircleBracket;
        }

        if (typeof(TToken) == typeof(Dot))
        {
            return previousToken is Entity or PropertyAccess or RightSquareBracket;
        }

        if (typeof(TToken) == typeof(LeftSquareBracket))
        {
            return previousToken is Where or Skip or Take or OrderBy or OrderByDescending or ThenBy or ThenByDescending
                or Select or Count;
        }

        if (typeof(TToken) == typeof(RightSquareBracket))
        {
            return previousToken is PropertyAccess or NameOrValue or RightCircleBracket
                or LeftSquareBracket;
        }

        if (typeof(TToken) == typeof(MethodCall))
        {
            return previousToken is Dot or LeftSquareBracket;
        }

        throw new UnknownTokenTypeException<TToken>();
    }

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

    public sealed class UnknownTokenTypeException<T>()
        : HLinqQueryException($"Unknown token type: '{typeof(T).Name}'. This token is not supported by HLinq grammar");
}
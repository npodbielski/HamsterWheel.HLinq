using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Filtering;
using HamsterWheel.HLinq.Tokens.Grouping;
using HamsterWheel.HLinq.Tokens.Ordering;
using HamsterWheel.HLinq.Tokens.Paging;
using HamsterWheel.HLinq.Tokens.Selecting;

namespace HamsterWheel.HLinq.UnitTests.TestUtils.Assertions;

public class ExpectedToken(Type type, string value)
{
    public Type Type { get; set; } = type;
    public string TokenValue { get; set; } = value;

    public static ExpectedToken Select => new(typeof(Select), "select");
    public static ExpectedToken Where => new(typeof(Where), "where");
    public static ExpectedToken GroupBy => new(typeof(GroupBy), "groupBy");
    public static ExpectedToken LeftSquareBracket => new(typeof(LeftSquareBracket), "[");
    public static ExpectedToken RightSquareBracket => new(typeof(RightSquareBracket), "]");
    public static ExpectedToken LeftCircleBracket => new(typeof(LeftCircleBracket), "(");
    public static ExpectedToken RightCircleBracket => new(typeof(RightCircleBracket), ")");
    public static ExpectedToken Dot => new(typeof(Dot), ".");

    public static ExpectedToken Prop(string prop) => new(typeof(PropertyName), prop);

    public static ExpectedToken Equality => new(typeof(Equality), "==");
    public static ExpectedToken GreaterOrEqualThan => new(typeof(GreaterOrEqualThan), ">=");
    public static ExpectedToken LessOrEqualThan => new(typeof(LessOrEqualThan), "<=");
    public static ExpectedToken LessThan => new(typeof(LessThan), "<");
    public static ExpectedToken GreaterThan => new(typeof(GreaterThan), ">");
    public static ExpectedToken Assignment => new(typeof(Assignment), "=");

    public static ExpectedToken NameOrValue(string value) => new(typeof(NameOrValue), value);

    public static ExpectedToken Or => new(typeof(Or), "||");
    public static ExpectedToken And => new(typeof(And), "&&");
    public static ExpectedToken Comma => new(typeof(Comma), ",");

    public static ExpectedToken Entity(string name = "x") => new(typeof(Entity), name);

    public static ExpectedToken MethodName(string name) => new(typeof(MethodName), name);

    public static ExpectedToken Take => new(typeof(Take), "take");
    public static ExpectedToken Skip => new(typeof(Skip), "skip");
    public static ExpectedToken OrderBy => new(typeof(OrderBy), "orderBy");
    public static ExpectedToken ThenBy => new(typeof(ThenBy), "thenBy");
    public static ExpectedToken ThenByDescending => new(typeof(ThenByDescending), "thenByDescending");
    public static ExpectedToken OrderByDescending => new(typeof(OrderByDescending), "orderByDescending");
}
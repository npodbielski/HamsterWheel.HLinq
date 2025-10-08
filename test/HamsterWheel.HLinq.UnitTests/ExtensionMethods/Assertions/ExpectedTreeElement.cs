using HamsterWheel.HLinq.Tree;
using HamsterWheel.HLinq.Tree.Filter;
using HamsterWheel.HLinq.Tree.Filtering;
using HamsterWheel.HLinq.Tree.Select;
using HamsterWheel.HLinq.Tree.Selecting;

namespace HamsterWheel.HLinq.UnitTests.Assertions;

public class ExpectedTreeElement(
    Type type,
    IEnumerable<ExpectedTreeElement>? children = null,
    IEnumerable<ExpectedToken>? tokens = null)
{
    public Type Type { get; set; } = type;
    public ExpectedTreeElement[]? Children { get; set; } = children?.ToArray();
    public ExpectedToken[]? Tokens { get; set; } = tokens?.ToArray();

    //select
    public static ExpectedTreeElement SelectRoot(params ExpectedTreeElement[] children) =>
        new(typeof(SelectRoot), children);

    public static ExpectedTreeElement PropertyAssignment(params ExpectedTreeElement[] children) =>
        new(typeof(PropertyAssignment), children);

    public static ExpectedTreeElement Property(params ExpectedTreeElement[] children) =>
        new(typeof(Property), children);

    public static ExpectedTreeElement InitializerPropertyName(string name) =>
        new(typeof(InitializerPropertyName), tokens: [ExpectedToken.NameOrValue(name)]);

    public static ExpectedTreeElement InitializerConstantValue(string value) =>
        new(typeof(InitializerConstantValue), tokens: [ExpectedToken.Assignment, ExpectedToken.NameOrValue(value)]);

    //where
    public static ExpectedTreeElement WhereRoot(params ExpectedTreeElement[] children) =>
        new(typeof(WhereRoot), children);

    public static ExpectedTreeElement ConditionElement(params ExpectedTreeElement[] children) =>
        new(typeof(Condition), children);

    public static ExpectedTreeElement ConditionGroup(params ExpectedTreeElement[] children) =>
        new(typeof(ConditionGroup), children);

    public static ExpectedTreeElement Property(params ExpectedToken[] tokens) => new(typeof(Property), tokens: tokens);
    public static ExpectedTreeElement ComparisonOperation(ExpectedToken token) => new(typeof(ComparisonOperation), tokens: [token]);
    public static ExpectedTreeElement ComparisonConstant(ExpectedToken token) => new(typeof(ComparisonConstant), tokens: [token]);

    public static ExpectedTreeElement MethodElement(params ExpectedTreeElement[] children) =>
        new(typeof(Method), children);

    public static ExpectedTreeElement MethodConstParam(params ExpectedToken[] tokens) =>
        new(typeof(MethodConstParam), tokens: tokens);
}
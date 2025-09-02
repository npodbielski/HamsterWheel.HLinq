using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;
using HamsterWheel.HLinq.Parsers;
using HamsterWheel.HLinq.Tree;
using HamsterWheel.HLinq.Tree.Filter;
using HamsterWheel.HLinq.Tree.Select;

namespace HamsterWheel.HLinq.UnitTests.Assertions;

public class HLinqITreeBranchAssertions(ITreeBranch instance) :
    ReferenceTypeAssertions<ITreeBranch, HLinqITreeBranchAssertions>(instance, AssertionChain.GetOrCreate())
{
    protected override string Identifier => "hLinqITreeBranch";

    public AndConstraint<HLinqITreeBranchAssertions> HaveStructureOf(string query,
        IEnumerable<ExpectedTreeElement> expected,
        string because = "",
        params object[] becauseArgs)
    {
        var expectedChildren = expected as ExpectedTreeElement[] ?? expected.ToArray();
        Subject.Should().NotBeNull();
        Subject.Children.Should().HaveCount(expectedChildren.Length);
        for (var index = 0; index < Subject.Children.Length; index++)
        {
            var child = Subject.Children[index];
            var expectedChild = expectedChildren.ElementAt(index);
            var expectedChildrenOfChild = expectedChild.Children;
            var expectedTokensOfChild = expectedChild.Tokens;
            child.Should().BeOfType(expectedChild.Type);
            if (expectedChildrenOfChild is { Length: > 0 })
            {
                (child as ITreeBranch)!.Should().HaveStructureOf(query, expectedChildrenOfChild ?? []);
            }
            else
            {
                child.Tokens.Should().HaveSequenceOf(query, expectedTokensOfChild ?? []);
            }
        }

        return new AndConstraint<HLinqITreeBranchAssertions>(this);
    }
}

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

    public static ExpectedTreeElement MethodElement(params ExpectedTreeElement[] children) =>
        new(typeof(Method), children);

    public static ExpectedTreeElement MethodConstParam(params ExpectedToken[] tokens) =>
        new(typeof(MethodConstParam), tokens: tokens);
}
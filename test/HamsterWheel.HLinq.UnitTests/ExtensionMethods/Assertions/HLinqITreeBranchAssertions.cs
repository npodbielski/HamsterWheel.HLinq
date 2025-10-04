using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;
using HamsterWheel.HLinq.Parsers;

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
                (child as ITreeBranch)!.Should().HaveStructureOf(query, expectedChildrenOfChild);
            }
            else
            {
                child.Tokens.Should().HaveSequenceOf(query, expectedTokensOfChild ?? []);
            }
        }

        return new AndConstraint<HLinqITreeBranchAssertions>(this);
    }
}
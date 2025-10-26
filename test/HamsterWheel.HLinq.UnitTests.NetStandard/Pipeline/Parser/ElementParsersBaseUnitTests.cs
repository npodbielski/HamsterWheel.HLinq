using FluentAssertions;
using HamsterWheel.HLinq.Pipeline.Parser;
using HamsterWheel.HLinq.Tokens;
using NSubstitute;

namespace HamsterWheel.HLinq.UnitTests.Pipeline.Parser;

// ReSharper disable once InconsistentNaming
public class ElementParserBaseUnitTests_NetStandard
{
    [Fact]
    public void ChildOf_WhenCalled_ThenReturnsFalse()
    {
        //arrange
        var sut = new TestElementParser();

        //act
        var actual = sut.ChildOf(new DummyOrderRoot());

        //assert
        actual.Should().BeFalse();
    }

    [Fact]
    public void ForElement_WhenCalled_ThenReturnsTypeParameter()
    {
        //arrange
        var sut = new TestElementParser();

        //act
        var actual = sut.ForElement();

        //assert
        actual.Should().Be<DummySelectRoot>();
    }

    [Fact]
    public void IsRoot_WhenCalled_ThenReturnsTrue()
    {
        //arrange
        var sut = new TestElementParser();

        //act
        var actual = sut.IsRoot;

        //assert
        actual.Should().BeTrue();
    }

    [Fact]
    public void Finish_WhenCalled_ThenCallsCorrectMethod()
    {
        //arrange
        var context = Substitute.For<IParsingContext>();
        var branch = Substitute.For<ITreeBranch>();
        context.CurrentBranch.Returns(branch);
        var sut = new TestElementParser();

        //act
        sut.Finish(context);

        //assert
        branch.Received(1).Finish(Arg.Any<IParsingContext>(), Arg.Any<IToken[]>());
    }

    [Fact]
    public void TryBuildElement_WhenBuildsBranchReturnsNull_ThenReturnsFalse()
    {
        //arrange
        var context = Substitute.For<IParsingContext>();
        var sut = new TestElementParser();

        //act && assert
        sut.TryBuildElement(context).Should().BeFalse();
    }

    [Fact]
    public void TryBuildElement_WhenBuildsElement_ThenPushesToContext()
    {
        //arrange
        var context = Substitute.For<IParsingContext>();
        var root = new DummySelectRoot();
        var sut = new TestElementParser(root);

        //act
        sut.TryBuildElement(context);

        //assert
        context.Received(1).Push(Arg.Any<ITreeElement>());
        context.Received(1).Push(Arg.Is(root));
        context.Received(1).RemoveStartTokens(Arg.Any<int>());
        context.Received(1).RemoveStartTokens(0);
    }
}

public class TestElementParser(DummySelectRoot? root = null) : ElementParserBase<DummySelectRoot>
{
    public override IToken[] ExampleTokens { get; } = [];
    protected override DummySelectRoot? BuildBranch(IParsingContext context) => root;
}

public class DummyOrderRoot : ITreeBranch
{
    public IToken[] Tokens { get; } = [];
    public bool NoChildren { get; } = true;
    public ITreeElement[] Children { get; } = [];
    public void Finish(IParsingContext context, IToken[] endingTokens)
    {
        
    }
}

public class DummySelectRoot : ITreeBranch
{
    public IToken[] Tokens { get; } = [];
    public bool NoChildren { get; } = true;
    public ITreeElement[] Children { get; } = [];
    public void Finish(IParsingContext context, IToken[] endingTokens)
    {
        
    }   
}
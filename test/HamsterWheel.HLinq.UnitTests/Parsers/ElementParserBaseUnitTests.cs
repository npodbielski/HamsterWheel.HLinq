using FluentAssertions;
using HamsterWheel.HLinq.Parsers;
using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tree.Ordering;
using HamsterWheel.HLinq.Tree.Select;
using HamsterWheel.HLinq.Tree.Selecting;
using NSubstitute;

namespace HamsterWheel.HLinq.UnitTests.Parsers;

public class ElementParserBaseUnitTests
{
    [Fact]
    public void ChildOf_WhenCalled_ThenReturnsFalse()
    {
        //arrange
        var sut = new TestElementParser();

        //act
        var actual = sut.ChildOf(new OrderByRoot([]));

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
        actual.Should().Be<SelectRoot>();
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
        var currentElement = Substitute.For<ITreeBranch>();
        var context = new ParsingContext(currentElement);
        var sut = new TestElementParser();

        //act
        sut.Finish(context);

        //assert
        currentElement.Received(1).Finish(Arg.Any<ParsingContext>(), Arg.Any<IToken[]>());
        currentElement.Received(1).Finish(context, Arg.Is<IToken[]>(d => d.Length == 0));
    }

    [Fact]
    public void TryBuildElement_WhenBuildsElement_ThenReturnsTrue()
    {
        //arrange
        var context = Substitute.For<IParsingContext>();
        var root = new SelectRoot([]);
        var sut = new TestElementParser(root);

        //act && assert
        sut.TryBuildElement(context).Should().BeTrue();
    }

    [Fact]
    public void TryBuildElement_WhenBuildsElement_ThenPushesToContext()
    {
        //arrange
        var context = Substitute.For<IParsingContext>();
        var root = new SelectRoot([]);
        var sut = new TestElementParser(root);

        //act
        sut.TryBuildElement(context);

        //assert
        context.Received(1).Push(Arg.Any<ITreeElement>());
        context.Received(1).Push(Arg.Is(root));
    }

    [Fact]
    public void TryBuildElement_WhenBuildsElement_ThenRemovesTokens()
    {
        //arrange
        var context = Substitute.For<IParsingContext>();
        var root = new SelectRoot([null!, null!]);
        var sut = new TestElementParser(root);

        //act
        sut.TryBuildElement(context);

        //assert
        context.Received(1).RemoveTokensFromStart(Arg.Any<int>());
        context.Received(1).RemoveTokensFromStart(Arg.Is(2));
    }
}

public class TestElementParser(SelectRoot? root = null) : ElementParserBase<SelectRoot>
{
    public override IToken[] ExampleTokens { get; } = [];
    protected override SelectRoot BuildBranch(IParsingContext context) => root ?? new SelectRoot([]);
}
using FluentAssertions;
using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Pipeline.Applier;
using HamsterWheel.HLinq.Pipeline.Parser;
using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tree;
using HamsterWheel.HLinq.Tree.Selecting;

namespace HamsterWheel.HLinq.UnitTests.Pipeline.Applier;

// ReSharper disable once InconsistentNaming
public class RootApplierBaseUnitTests_NetStandard
{
    [Fact]
    public void CanApply_WhenCalledWithCorrectType_ThenReturnTrue()
    {
        //arrange
        var sut = new TestRootApplier();

        //act
        var actual = sut.CanApply(new TestDummySelectRoot());

        //assert
        actual.Should().BeTrue();
    }

    [Fact]
    public void CanApply_WhenCalledWithIncorrectType_ThenReturnFalse()
    {
        //arrange
        var sut = new TestRootApplier();

        //act
        var actual = sut.CanApply(new TestDummyOrderRoot());

        //assert
        actual.Should().BeFalse();
    }

    [Fact]
    public void Apply_WhenCalledWithInvalidType_ThenThrowException()
    {
        //arrange
        var sut = new TestRootApplier();
        var action = () => sut.Apply(new TestDummyQueryableContext(), new TestDummyOrderRoot(),
            "select[x.name]");

        //act
        var actual = action.Should().Throw<InvalidTypeOfTreeRoot<TestDummySelectRoot>>();

        //assert
        actual.WithMessage("* but type of * was expected.");
    }

    [Fact]
    public void Apply_WhenCalledWithCancellation_ThenThrowException()
    {
        //arrange
        var cancellationTokenSource = new CancellationTokenSource();
        var sut = new TestRootApplier();
        var action = () => sut.Apply(new TestDummyQueryableContext(), new TestDummySelectRoot(),
            "select[x.name]", cancellationTokenSource.Token);
        cancellationTokenSource.Cancel();

        //act and assert
        action.Should().Throw<OperationCanceledException>();
    }

    [Fact]
    public void Apply_WhenCalledWithCorrectData_ThenCallsImplementation()
    {
        //arrange
        var cancellationTokenSource = new CancellationTokenSource();
        var expectedContext =
            new TestDummyQueryableContext();
        var sut = new TestRootApplier(expectedContext);

        //act
        var actual = sut.Apply(new TestDummyQueryableContext(), new TestDummySelectRoot(), "select[x.name]",
            cancellationTokenSource.Token);

        //assert
        actual.Should().Be(expectedContext);
    }
}

public class TestRootApplier(IQueryableContext? expectedContext = null) : RootApplierBase<TestDummySelectRoot>
{
    protected override IQueryableContext ApplyImpl(IQueryableContext context, TestDummySelectRoot selectRoot,
        string hLinqQuery) =>
        expectedContext ?? throw new NotImplementedException();
}

public class TestDummySelectRoot : ISelectRoot
{
    public IToken[] Tokens { get; } = [];
    public bool NoChildren => true;
    public ITreeElement[] Children { get; } = [];

    public void Finish(IParsingContext context, IToken[] endingTokens)
    {
    }
}

public class TestDummyOrderRoot : ITreeRoot
{
    public IToken[] Tokens { get; } = [];
    public bool NoChildren => true;
    public ITreeElement[] Children { get; } = [];

    public void Finish(IParsingContext context, IToken[] endingTokens)
    {
    }
}

public class TestDummyQueryableContext : IQueryableContext
{
    public IQueryable? Queryable { get; init; } = Enumerable.Empty<Person>().AsQueryable();
    public Type CurrentResultType { get; init; } = typeof(Person);
    public int? Count { get; init; } = 0;
}

public class Person
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
}
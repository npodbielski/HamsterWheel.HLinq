using FluentAssertions;
using HamsterWheel.HLinq.Appliers;
using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Tree.Ordering;
using HamsterWheel.HLinq.Tree.Select;
using HamsterWheel.HLinq.Tree.Selecting;
using HamsterWheel.HLinq.UnitTests.Dummies;

namespace HamsterWheel.HLinq.UnitTests.Appliers;

public class RootApplierBaseUnitTests
{
    [Fact]
    public void CanApply_WhenCalledWithCorrectType_ThenReturnTrue()
    {
        //arrange
        var sut = new TestRootApplier();

        //act
        var actual = sut.CanApply(new SelectRoot([]));

        //assert
        actual.Should().BeTrue();
    }

    [Fact]
    public void CanApply_WhenCalledWithIncorrectType_ThenReturnFalse()
    {
        //arrange
        var sut = new TestRootApplier();

        //act
        var actual = sut.CanApply(new OrderByRoot([]));

        //assert
        actual.Should().BeFalse();
    }

    [Fact]
    public void Apply_WhenCalledWithInvalidType_ThenThrowException()
    {
        //arrange
        var sut = new TestRootApplier();
        var action = () => sut.Apply(new QueryableContext(null, typeof(DummyEntity)), new OrderByRoot([]),
            "select[x.name]");

        //act
        var actual = action.Should().Throw<InvalidTypeOfTreeRoot<SelectRoot>>();

        //assert
        actual.WithMessage("* but type of * was expected.");
    }

    [Fact]
    public void Apply_WhenCalledWithCancellation_ThenThrowException()
    {
        //arrange
        var cancellationTokenSource = new CancellationTokenSource();
        var sut = new TestRootApplier();
        var action = () => sut.Apply(new QueryableContext(null, typeof(DummyEntity)), new SelectRoot([]),
            "select[x.name]", cancellationTokenSource.Token);
        cancellationTokenSource.Cancel();

        //act & assert
        action.Should().Throw<OperationCanceledException>();
    }

    [Fact]
    public void Apply_WhenCalledWithCorrectData_ThenCallsImplementation()
    {
        //arrange
        var cancellationTokenSource = new CancellationTokenSource();
        var expectedContext =
            new QueryableContext(new[] { new DummyEntity("test") }.AsQueryable(), typeof(DummyEntity));
        var sut = new TestRootApplier(expectedContext);

        //act
        var actual = sut.Apply(new QueryableContext(null, typeof(DummyEntity)), new SelectRoot([]),
            "select[x.name]", cancellationTokenSource.Token);

        //assert
        actual.Should().Be(expectedContext);
    }
}

public class TestRootApplier(IQueryableContext? expectedContext = null) : RootApplierBase<SelectRoot>
{
    protected override IQueryableContext ApplyImpl(IQueryableContext context, SelectRoot root, string hLinqQuery) =>
        expectedContext ?? throw new NotImplementedException();
}
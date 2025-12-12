using FluentAssertions;
using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Pipeline.Parser;
using HamsterWheel.HLinq.Request;
using HamsterWheel.HLinq.Tokens.Filtering;
using HamsterWheel.HLinq.Tree.Filtering;
using HamsterWheel.HLinq.UnitTests.TestUtils;
using HamsterWheel.HLinq.UnitTests.TestUtils.Dummies;
using Microsoft.AspNetCore.Http;
using NSubstitute;

namespace HamsterWheel.HLinq.UnitTests.Requests;

public class HLinqQueryUnitTests
{
    [Fact]
    public void ApplyTo_WhenCalledWithIQueryable_ThenCallsIQueryApplier()
    {
        //arrange
        var expected = (string[])["test"];
        var dependenciesBag = new DummyHLinqServiceProviderFactory().BinderDependenciesBag;
        var sut = HLinqQuery<DummyEntity>.Parse(dependenciesBag, "select[x.Name]");
        dependenciesBag.QueryApplier.Apply(Arg.Any<IQueryable<DummyEntity>>(), Arg.Any<IHLinqQuery>(),
            Arg.Any<CancellationToken>()).Returns(expected);

        //act
        var actual = sut.ApplyTo(new List<DummyEntity> { new("test") }.AsQueryable());

        //assert
        actual.Should().BeOfType<string[]>().Which.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void ApplyTo_WhenCalledAdnQueryApplierNull_ThenThrows()
    {
        //arrange
        var sut = new HLinqQuery<DummyEntity>
        {
            SourceQueryString = "select[x.Name]"
        };
        var action = () => sut.ApplyTo(new List<DummyEntity> { new("test") }.AsQueryable());

        //act
        var exception = action.Should().Throw<HLinqQueryQueryApplierNullException>();

        //assert
        exception.WithMessage("*use IHLinqQueryApplier.Apply method instead*");
    }

    [Fact]
    public void Tokens_WhenUsesFromInterface_ThenCanBeCalled()
    {
        //arrange
        ITreeElement sut = new HLinqQuery<DummyEntity>();

        //act
        var tokens = sut.Tokens;

        //assert
        AssertionExtensions.Should(tokens).BeEmpty();
    }

    [Fact]
    public void Finish_WhenCalledWithNotEmptyTokens_ThenThrows()
    {
        //arrange
        var hLinqQuery = new HLinqQuery<DummyEntity> { SourceQueryString = "select[x.Name]" };
        ITreeBranch sut = hLinqQuery;
        var action = () => sut.Finish(new ParsingContext(hLinqQuery)
        {
            Tokens = [new And()]
        }, []);

        //act
        var exception = action.Should().Throw<NonParsableTokenSequenceException>();

        //assert
        exception.WithMessage("*");
    }

    [Fact]
    public void NoChildren_WhenCalledOnEmpty_ThenTrue()
    {
        //arrange
        ITreeElement sut = new HLinqQuery<DummyEntity>();

        //act
        var tokens = sut.NoChildren;

        //assert
        tokens.Should().BeTrue();
    }

    [Fact]
    public void NoChildren_WhenCalledNotEmpty_ThenFalse()
    {
        //arrange
        var hLinqQuery = new HLinqQuery<DummyEntity>();
        ITreeElement sut = hLinqQuery;
        ((ITreeBranch)hLinqQuery).Finish(new ParsingContext(hLinqQuery)
        {
            Tokens = [],
            Children = { new WhereRoot([]) }
        }, []);

        //act
        var tokens = sut.NoChildren;

        //assert
        tokens.Should().BeFalse();
    }

    [Fact]
    public async Task BindAsync_WhenEmptyQueryString_ThenEmptyHLinqQueryReturned()
    {
        //arrange
        //act
        var actual = await HLinqQuery<DummyEntity>.BindAsync(new DefaultHttpContext
        {
            RequestServices = new DummyHLinqServiceProviderFactory().CreateServiceProvider()
        }, null!);

        //assert
        actual.Children.Should().BeEmpty();
    }
}
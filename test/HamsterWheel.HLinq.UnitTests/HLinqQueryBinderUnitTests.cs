using System.Reflection;
using FluentAssertions;
using HamsterWheel.HLinq.Request;
using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.UnitTests.TestUtils;
using HamsterWheel.HLinq.UnitTests.TestUtils.Dummies;
using NSubstitute;

namespace HamsterWheel.HLinq.UnitTests;

public class HLinqQueryBinderUnitTests
{
    [Fact]
    public void BindQuery_WhenCalled_Then()
    {
        //arrange
        var expected = new HLinqQuery<DummyEntity>();
        var dummyServices = new DummyHLinqServiceProviderFactory();
        dummyServices.Parser.Parse(Arg.Any<HLinqQuery<DummyEntity>>(), Arg.Any<IToken[]>()).Returns(expected);
        var hlinqQueryMethod = typeof(HLinqQuery<DummyEntity>).GetMethods().First(m => m.Name == "Parse");
        dummyServices.MethodsCache
            .GetStaticOrThrow(Arg.Any<Type>(), Arg.Any<string>(), Arg.Any<Func<ParameterInfo[], bool>>())
            .Returns(hlinqQueryMethod);
        var sut = new HLinqQueryBinder(dummyServices.BinderDependenciesBag);
        var queryString = "where[x.name==test]";

        //act
        var actual = sut.BindQuery(queryString, typeof(HLinqQuery<DummyEntity>));

        //assert
        actual.Should().BeOfType<HLinqQuery<DummyEntity>>();
        actual.Children.Should().BeEmpty();
        actual.NoChildren.Should().BeTrue();
        actual.SourceQueryString.Should().Be(queryString);
    }
}
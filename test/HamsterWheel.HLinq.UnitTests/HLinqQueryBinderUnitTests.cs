using System.Reflection;
using FluentAssertions;
using HamsterWheel.HLinq.Appliers;
using HamsterWheel.HLinq.Parsers;
using HamsterWheel.HLinq.Reflection;
using HamsterWheel.HLinq.Request;
using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.UnitTests.Dummies;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace HamsterWheel.HLinq.UnitTests;

public class HLinqQueryBinderUnitTests
{
    [Fact]
    public void BindQuery_WhenCalled_Then()
    {
        //arrange
        var expected = new HLinqQuery<DummyEntity>();
        var serviceProviderFactory = new DummyHLinqServiceProviderFactory();
        serviceProviderFactory.Parser.Parse<DummyEntity>(Arg.Any<IToken[]>(), Arg.Any<string>()).Returns(expected);
        var parserMethod = typeof(IHLinqParser).GetMethods().First(m => m.Name == "Parse")
            .MakeGenericMethod(typeof(DummyEntity));
        serviceProviderFactory.MethodsCache
            .GetInstanceGeneric(Arg.Any<Type>(), Arg.Any<string>(), Arg.Any<Func<ParameterInfo[], bool>>(),
                Arg.Any<Type[]>()).Returns(parserMethod);
        var hlinqQueryMethod = typeof(HLinqQuery<DummyEntity>).GetMethods().First(m => m.Name == "Parse");
        serviceProviderFactory.MethodsCache.GetStatic(Arg.Any<Type>(), Arg.Any<string>(), Arg.Any<Func<ParameterInfo[], bool>>())
            .Returns(hlinqQueryMethod);
        var sut = new HLinqQueryBinder(new HLinqCore(serviceProviderFactory.CreateServiceProvider()));

        //act
        var actual = sut.BindQuery("where[x.name==test]", typeof(DummyEntity));

        //assert
        actual.Should().Be(expected);
    }
}
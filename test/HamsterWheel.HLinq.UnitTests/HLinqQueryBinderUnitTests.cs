using System.Reflection;
using FluentAssertions;
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
        var parser = Substitute.For<IHLinqParser>();
        parser.Parse<DummyEntity>(Arg.Any<IToken[]>(), Arg.Any<string>()).Returns(expected);
        var tokenizer = Substitute.For<IHLinqTokenizer>();
        var methodsCache = Substitute.For<IMethodsCache>();
        var services = new ServiceCollection();
        services.AddSingleton(parser);
        services.AddSingleton(methodsCache);
        services.AddSingleton(tokenizer);
        var parserMethod = typeof(IHLinqParser).GetMethods().First(m => m.Name == "Parse")
            .MakeGenericMethod(typeof(DummyEntity));
        methodsCache
            .GetInstanceGeneric(Arg.Any<Type>(), Arg.Any<string>(), Arg.Any<Func<ParameterInfo[], bool>>(),
                Arg.Any<Type[]>()).Returns(parserMethod);
        var hlinqQueryMethod = typeof(HLinqQuery<DummyEntity>).GetMethods().First(m => m.Name == "Parse");
        methodsCache.GetStatic(Arg.Any<Type>(), Arg.Any<string>(), Arg.Any<Func<ParameterInfo[], bool>>())
            .Returns(hlinqQueryMethod);
        var sut = new HLinqQueryBinder(new HLinqCore(services.BuildServiceProvider()));

        //act
        var actual = sut.BindQuery("where[x.name==test]", typeof(DummyEntity));

        //assert
        actual.Should().Be(expected);
    }
}
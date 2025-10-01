using System.Reflection;
using FluentAssertions;
using HamsterWheel.HLinq.AspNet.Binder;
using HamsterWheel.HLinq.Parsers;
using HamsterWheel.HLinq.Reflection;
using HamsterWheel.HLinq.Request;
using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.UnitTests.Dummies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace HamsterWheel.HLinq.UnitTests.AspNet.Binder;

public class WebHLinqQueryBinderUnitTests
{
    [Fact]
    public void BindModelAsync_WhenCalledWithNull_ThenThrows()
    {
        //arrange
        var expected = "";
        var parser = Substitute.For<IHLinqParser>();
        var tokenizer = Substitute.For<IHLinqTokenizer>();
        var methodsCache = Substitute.For<IMethodsCache>();
        var services = new ServiceCollection();
        services.AddSingleton(parser);
        services.AddSingleton(methodsCache);
        services.AddSingleton(tokenizer);
        var sut = new WebHLinqQueryBinder(new HLinqCore(services.BuildServiceProvider()));
        var action = () => sut.BindModelAsync(null);

        //act && assert
        action.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task BindModelAsync_WhenCalledWithContext_ThenSetsResult()
    {
        //arrange
        var expected = "";
        var parser = Substitute.For<IHLinqParser>();
        var tokenizer = Substitute.For<IHLinqTokenizer>();
        var methodsCache = Substitute.For<IMethodsCache>();
        var services = new ServiceCollection();
        services.AddSingleton(parser);
        services.AddSingleton(methodsCache);
        services.AddSingleton(tokenizer);
        var method = typeof(IHLinqParser).GetMethods().First(m => m.Name == "Parse").MakeGenericMethod(typeof(DummyEntity));
        methodsCache.GetInstanceGeneric(Arg.Any<Type>(), Arg.Any<string>(), Arg.Any<Func<ParameterInfo[], bool>>(), Arg.Any<Type[]>())
            .Returns(method);
        var query = new HLinqQuery<DummyEntity>();
        parser.Parse<DummyEntity>(Arg.Any<IToken[]>(), Arg.Any<string>()).Returns(query);
        var sut = new WebHLinqQueryBinder(new HLinqCore(services.BuildServiceProvider()));
        var context = Substitute.For<ModelBindingContext>();
        context.HttpContext.Request.QueryString.Returns(new QueryString("?key=value"));
        context.ModelType.Returns(typeof(HLinqQuery<DummyEntity>));

        //act
        await sut.BindModelAsync(context);

        //assert
        context.Result.Model.Should().NotBeNull();
        context.Result.Model.Should().Be(query);
    }
}
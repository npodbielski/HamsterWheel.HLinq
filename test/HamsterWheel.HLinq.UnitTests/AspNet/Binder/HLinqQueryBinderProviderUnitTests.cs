using FluentAssertions;
using HamsterWheel.HLinq.AspNet.Binder;
using HamsterWheel.HLinq.Request;
using HamsterWheel.HLinq.UnitTests.TestUtils.Dummies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using NSubstitute;

namespace HamsterWheel.HLinq.UnitTests.AspNet.Binder;

public class HLinqQueryBinderProviderUnitTests
{
    [Fact]
    public void GetBinder_WhenNullPassed_ThenThrowsAnException()
    {
        //arrange
        var sut = new HLinqQueryBinderProvider();
        var action = () => sut.GetBinder(null!);

        //act
        var actual = action.Should().Throw<ArgumentNullException>();

        //assert
        actual.WithParameterName("context");
    }

    [Fact]
    public void GetBinder_WhenContextPassedWithCorrectInterface_ThenReturnsQueryBinder()
    {
        //arrange
        var sut = new HLinqQueryBinderProvider();
        var context = Substitute.For<ModelBinderProviderContext>();
        var action = (HLinqQuery<DummyEntity> query) => Console.WriteLine("demo");
        var modelMetadata =
            Substitute.For<ModelMetadata>(ModelMetadataIdentity.ForParameter(action.Method.GetParameters()[0]));
        context.Metadata.Returns(modelMetadata);

        //act
        var actual = sut.GetBinder(context);

        //assert
        actual.Should().BeOfType<BinderTypeModelBinder>();
    }

    [Fact]
    public void GetBinder_WhenContextPassedWithoutInterface_ThenReturnsNull()
    {
        //arrange
        var sut = new HLinqQueryBinderProvider();
        var action = (HttpContext context) => Console.WriteLine("demo");
        var modelMetadata =
            Substitute.For<ModelMetadata>(ModelMetadataIdentity.ForParameter(action.Method.GetParameters()[0]));
        var context = Substitute.For<ModelBinderProviderContext>();
        context.Metadata.Returns(modelMetadata);

        //act
        var actual = sut.GetBinder(context);

        //assert
        actual.Should().BeNull();
    }
}
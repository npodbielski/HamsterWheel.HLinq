using FluentAssertions;
using HamsterWheel.HLinq.Reflection;

namespace HamsterWheel.HLinq.UnitTests.Reflection;

public class MethodsCacheUnitTests
{
    [Fact]
    public void GetStaticOrThrow_WhenCantFindMethod_ThenThrows()
    {
        //arrange
        var methodsCache = new MethodsCache();
        var action = () => methodsCache.GetStaticOrThrow(typeof(int), "Test", null);

        //act
        var actual = action.Should().Throw<MethodsCache.MissingStaticMethodException>();

        //assert
        actual.WithMessage("Could not find static * method");
    }
}
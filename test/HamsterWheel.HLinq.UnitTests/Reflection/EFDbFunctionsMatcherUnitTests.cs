using System.Reflection;
using FluentAssertions;
using HamsterWheel.HLinq.Reflection;
using Microsoft.EntityFrameworkCore;

namespace HamsterWheel.HLinq.UnitTests.Reflection;

public class EfDbFunctionsMatcherUnitTests
{
    [Fact]
    public void IsEfDbFunction_WhenCalledWithMethodInfo_ThenTrue()
    {
        //arrange
        var expected = true;
        var info = typeof(NpgsqlDbFunctionsExtensions).GetMethod(nameof(NpgsqlDbFunctionsExtensions.ILike),
            BindingFlags.Public | BindingFlags.Static, null, [typeof(DbFunctions), typeof(string), typeof(string)],
            null);

        //act
        var actual = EfDbFunctionsMatcher.IsEfDbFunction(info!);

        //assert
        actual.Should().Be(expected);
    }

    [Fact]
    public void IsEfDbFunction_WhenCalledWithParameters_ThenTrue()
    {
        //arrange
        var expected = true;
        var info = typeof(NpgsqlDbFunctionsExtensions).GetMethod(nameof(NpgsqlDbFunctionsExtensions.StringToArray),
            BindingFlags.Public | BindingFlags.Static, null, [typeof(DbFunctions), typeof(string), typeof(string)],
            null);

        //act
        var actual = EfDbFunctionsMatcher.IsEfDbFunction(info!.GetParameters());

        //assert
        actual.Should().Be(expected);
    }

    [Fact]
    public void IsEfDbFunctionParameter_WhenCalledWithParameter_ThenTrue()
    {
        //arrange
        var expected = true;
        var info = typeof(NpgsqlDbFunctionsExtensions).GetMethod(nameof(NpgsqlDbFunctionsExtensions.Reverse));

        //act
        var actual = EfDbFunctionsMatcher.IsEfDbFunctionParameter(info!.GetParameters().First());

        //assert
        actual.Should().Be(expected);
    }
}
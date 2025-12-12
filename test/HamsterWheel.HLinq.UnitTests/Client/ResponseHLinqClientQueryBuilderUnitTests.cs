using System.Reflection;
using System.Text.Json;
using FluentAssertions;
using HamsterWheel.HLinq.Client;
using HamsterWheel.HLinq.Client.Exceptions;
using HamsterWheel.HLinq.UnitTests.TestUtils.Dummies;

namespace HamsterWheel.HLinq.UnitTests.Client;

public class ResponseHLinqClientQueryBuilderUnitTests
{
    [Fact]
    public void Deserialize_WhenDeserializesNull_ThenThrows()
    {
        //arrange
        var type = typeof(ResponseHLinqClientQueryBuilder<int?>);
        var sut = (ResponseHLinqClientQueryBuilder<int?>)type.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)[0].Invoke([null]);
        var action = () => sut.Deserialize("null");

        //act
        var exception = action.Should().Throw<CouldNotDeserializeException<int?>>();

        //assert
        exception.WithMessage("Could not deserialize string*'");
    }

    [Fact]
    public void Deserialize_WhenIncorrectString_ThenThrows()
    {
        //arrange
        var type = typeof(ResponseHLinqClientQueryBuilder<DummyEntity>);
        var sut = (ResponseHLinqClientQueryBuilder<DummyEntity>)type.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)[0].Invoke([null]);
        var action = () => sut.Deserialize("");

        //act
        var exception = action.Should().Throw<JsonException>();

        //assert
        exception.WithMessage("The input does not contain any JSON tokens.*");
    }
}
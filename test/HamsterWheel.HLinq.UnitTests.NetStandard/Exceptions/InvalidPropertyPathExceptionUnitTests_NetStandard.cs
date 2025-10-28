using FluentAssertions;
using HamsterWheel.HLinq.Exceptions;

namespace HamsterWheel.HLinq.UnitTests.Exceptions;

// ReSharper disable once InconsistentNaming
public class InvalidPropertyPathExceptionUnitTests_NetStandard
{
    [Fact]
    public void Message_WhenTypeAndPropertiesProvided_ThenProducedMessageIsCorrect()
    {
        //arrange
        var invalidProp = "Surname";
        var expected =  $"Invalid property path '{invalidProp}' for entity '{nameof(Person)}'. Available properties at this point are: 'Name'";

        //act
        var actual = new InvalidPropertyPathException(typeof(Person), invalidProp, [nameof(Person.Name)]);

        //assert
        actual.Message.Should().Be(expected);
    }

    private record Person(string Name);
}
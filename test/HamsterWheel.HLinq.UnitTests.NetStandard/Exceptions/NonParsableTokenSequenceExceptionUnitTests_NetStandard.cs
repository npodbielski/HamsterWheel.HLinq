using FluentAssertions;
using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Parsers;
using HamsterWheel.HLinq.Tokens;
using NSubstitute;

namespace HamsterWheel.HLinq.UnitTests.Exceptions;

public class NonParsableTokenSequenceExceptionUnitTests_NetStandard
{
    [Fact]
    public void Message_WhenCalledWithOneParser_ThenItIsMentionedInMessage()
    {
        //arrange
        var hlinqQueryString = "foo[].bar[]";
        var example = "where[1=1]";
        var expected =  $"HLinq query '{hlinqQueryString}' is invalid at character 0: '{hlinqQueryString}'. Was expecting for example: '{example}'.";
        var elementParser = Substitute.For<IElementParser>();
        elementParser.ExampleTokens.Returns([new TokenExample(example)]);

        //act
        var actual = new NonParsableTokenSequenceException("foo[].bar[]",
            [new TokenExample("foo[]"), new TokenExample("."), new TokenExample("bar[]")], [elementParser]);

        //assert
        actual.Message.Should().Be(expected);
    }
}
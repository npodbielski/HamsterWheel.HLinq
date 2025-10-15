using FluentAssertions;
using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Parsers;
using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tree.Filtering;
using NSubstitute;

namespace HamsterWheel.HLinq.UnitTests.Exceptions;

public class NonParsableTokenSequenceExceptionUnitTests
{
    [Fact]
    public void Message_WhenCalledWithOneParser_ThenItIsMentionedInMessage()
    {
        //arrange
        var hlinqQueryString = "foo[].bar[]";
        var expected =  $"HLinq query '{hlinqQueryString}' is invalid at character 0: '{hlinqQueryString}'. Was expecting for example: 'where[x.Name==Jan]'.";

        //act
        var actual = new NonParsableTokenSequenceException("foo[].bar[]",
            [new TokenExample("foo[]"), new TokenExample("."), new TokenExample("bar[]")], [new WhereRoot.Parser()]);

        //assert
        actual.Message.Should().Be(expected);
    }
}
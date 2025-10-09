using FluentAssertions;
using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Tokenizer;
using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Filtering;
using HamsterWheel.HLinq.Tokens.Ordering;
using HamsterWheel.HLinq.Tokens.Paging;
using HamsterWheel.HLinq.Tokens.Selecting;

namespace HamsterWheel.HLinq.UnitTests.Exceptions;

public class InvalidTokenCollectionExceptionUnitTests
{
    [Fact]
    public void Message_WhenNoAlternatives_ThenCorrectlyFormatsMessage()
    {
        //arrange
        const string hlinqQueryString = "select[]";
        const string expected = $"HLinq query '{hlinqQueryString}' is invalid at character 0: 'select[]'.";

        //act
        var e = new InvalidTokenCollectionException(hlinqQueryString, [
            new Select(..6), new RightSquareBracket(6..7), new LeftSquareBracket(7..8)
        ], []);

        //assert
        e.Message.Should().Be(expected);
    }

    [Fact]
    public void Message_WhenSingleExpectedProvided_ThenCorrectlyFormatsMessage()
    {
        //arrange
        const string hlinqQueryString = "select[]";
        const string expected =
            $"HLinq query '{hlinqQueryString}' is invalid at character 0: 'select[]'. Was expecting for example: 'select[x.Name]'.";

        //act
        var e = new InvalidTokenCollectionException(hlinqQueryString, [
                new Select(..6), new LeftSquareBracket(6..7), new RightSquareBracket(7..8)
            ],
            [
                new Select(default), new LeftSquareBracket(default), new Entity(default), new Dot(default),
                new PropertyAccess(default), new RightSquareBracket(default)
            ]);

        //assert
        e.Message.Should().Be(expected);
    }

    [Fact]
    public void Message_WhenWhereReverseComparison_ThenCorrectlyFormatsMessage()
    {
        //arrange
        const string hlinqQueryString = "where[2=x.Int]";
        const string expected = $""" 
                                 HLinq query '{hlinqQueryString}' is invalid at character 6: '2=x.Int'. Was expecting for example: 'x.Name==Jan'.
                                 You can also try:
                                  - &&
                                  - ||
                                  - MethodCall
                                 """;

        //act
        var e = new InvalidTokenCollectionException(hlinqQueryString, [
                new NameOrValue(6..7), new Assignment(7..8), new LeftSquareBracket(8..13)
            ],
            [
                new Entity(default), new Dot(default), new PropertyAccess(default), new Equality(default),
                new NameOrValue(default)
            ],
            [new And(default)],
            [new Or(default)],
            [new MethodCall(default)]);

        //assert
        e.Message.Should().Be(expected);
    }

    [Fact]
    public void Message_WhenConditionGroupOrMethodIsNotFinishedWithCircleBracket_ThenCorrectlyFormatsMessage()
    {
        //arrange
        const string hlinqQueryString = "where[(x.Flag]";
        const string expected =
            $"HLinq query '{hlinqQueryString}' is invalid at character 13: ']'. Was expecting for example: ')'.";

        //act
        var e = new InvalidTokenCollectionException(hlinqQueryString, [new RightSquareBracket(13..14)],
            [new RightCircleBracket(default)]);

        //assert
        e.Message.Should().Be(expected);
    }

    [Fact]
    public void Message_WhenRootIsNotFinishedWithSquareBracket_ThenCorrectlyFormatsMessage()
    {
        //arrange
        const string hlinqQueryString = "where[x.Int=2";
        const string expected =
            $"HLinq query '{hlinqQueryString}' is invalid and not finished properly. Was expecting for example: ']'.";

        //act
        var e = new InvalidTokenCollectionException(hlinqQueryString, [], [new RightSquareBracket(default)]);

        //assert
        e.Message.Should().Be(expected);
    }

    [Fact]
    public void Message_WhenTakeIsEmpty_ThenCorrectlyFormatsMessage()
    {
        //arrange
        const string hlinqQueryString = "take[]";
        const string expected =
            $"HLinq query '{hlinqQueryString}' is invalid and not finished properly. Was expecting for example: 'take[10]'.";

        //act
        var e = new InvalidTokenCollectionException(hlinqQueryString, [], [
            new Take(default),
            new LeftSquareBracket(default),
            new TokenExample("10"),
            new RightSquareBracket(default),
        ]);

        //assert
        e.Message.Should().Be(expected);
    }

    [Fact]
    public void Message_WhenCalledFromUnknownTokenException_ThenCorrectlyFormatsMessage()
    {
        //arrange
        const string hlinqQueryString = "foo[]";
        const string expected =
            """
            HLinq query 'foo[]' is invalid and not finished properly. Was expecting for example: 'where'.
            You can also try:
             - select
             - skip
             - take
             - orderby
             - orderbyDescending
            """;

        //act
        var e = new HLinqTokenizer.UnknownTokenException(hlinqQueryString, [
            new Where(default), new Select(default), new Skip(default), new Take(default), new OrderBy(default),
            new OrderByDescending(default)
        ]);

        //assert
        e.Message.Should().Be(expected);
    }
}
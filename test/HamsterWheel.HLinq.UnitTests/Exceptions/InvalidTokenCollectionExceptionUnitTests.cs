using FluentAssertions;
using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Pipeline.Tokenizer;
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
            Select.Build(..6), RightSquareBracket.Build(6..7), LeftSquareBracket.Build(7..8)
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
                Select.Build(..6), LeftSquareBracket.Build(6..7), RightSquareBracket.Build(7..8)
            ],
            [
                Select.Empty, LeftSquareBracket.Empty, Entity.Empty, Dot.Empty,
                PropertyName.Empty, RightSquareBracket.Empty
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
                                  - Method
                                 """;

        //act
        var e = new InvalidTokenCollectionException(hlinqQueryString, [
                NameOrValue.Build(6..7), Assignment.Build(7..8), LeftSquareBracket.Build(8..13)
            ],
            [
                Entity.Empty, Dot.Empty, PropertyName.Empty, Equality.Empty,
                NameOrValue.Empty
            ],
            [And.Empty],
            [Or.Empty],
            [MethodName.Empty]);

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
        var e = new InvalidTokenCollectionException(hlinqQueryString, [RightSquareBracket.Build(13..14)],
            [RightCircleBracket.Empty]);

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
        var e = new InvalidTokenCollectionException(hlinqQueryString, [], [RightSquareBracket.Empty]);

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
            Take.Empty,
            LeftSquareBracket.Empty,
            new TokenExample("10"),
            RightSquareBracket.Empty,
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
             - orderBy
             - orderByDescending
            """;

        //act
        var e = new HLinqTokenizer.UnknownTokenException(hlinqQueryString, [
            Where.Empty, Select.Empty, Skip.Empty, Take.Empty, OrderBy.Empty,
            OrderByDescending.Empty
        ]);

        //assert
        e.Message.Should().Be(expected);
    }
}
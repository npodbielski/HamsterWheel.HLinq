using FluentAssertions;
using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Tokens;

namespace HamsterWheel.HLinq.UnitTests.Exceptions;

public class InvalidTokenCollectionExceptionUnitTests_NetStandard
{
    [Fact]
    public void Message_WhenNoAlternatives_ThenCorrectlyFormatsMessage()
    {
        //arrange
        const string hlinqQueryString = "select[]";
        const string expected = $"HLinq query '{hlinqQueryString}' is invalid at character 0: 'select[]'.";

        //act
        var e = new InvalidTokenCollectionException(hlinqQueryString, [
            new TokenExample("select"), new TokenExample("["), new TokenExample("]")
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
                new TokenExample("select"), new TokenExample("["), new TokenExample("]")
            ],
            [
                new TokenExample("select"), new TokenExample("["), new TokenExample("x"), new TokenExample("."),
                new TokenExample("Name"), new TokenExample("]")
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
                                 HLinq query '{hlinqQueryString}' is invalid at character 0: '2=x.Int'. Was expecting for example: 'x.Name==Jan'.
                                 You can also try:
                                  - &&
                                  - ||
                                  - MethodCall
                                 """;

        //act
        var e = new InvalidTokenCollectionException(hlinqQueryString, [
                new TokenExample("2"), new TokenExample("="), new TokenExample("x.Int")
            ],
            [
                new TokenExample("x"), new TokenExample("."), new TokenExample("Name"), new TokenExample("=="),
                new TokenExample("Jan")
            ],
            [new TokenExample("&&")],
            [new TokenExample("||")],
            [new TokenExample("MethodCall")]);

        //assert
        e.Message.Should().Be(expected);
    }

    [Fact]
    public void Message_WhenConditionGroupOrMethodIsNotFinishedWithCircleBracket_ThenCorrectlyFormatsMessage()
    {
        //arrange
        const string hlinqQueryString = "where[(x.Flag]";
        const string expected =
            $"HLinq query '{hlinqQueryString}' is invalid at character 0: ']'. Was expecting for example: ')'.";

        //act
        var e = new InvalidTokenCollectionException(hlinqQueryString, [new TokenExample("]")], [new TokenExample(")")]);

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
        var e = new InvalidTokenCollectionException(hlinqQueryString, [], [new TokenExample("]")]);

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
            new TokenExample("take"),
            new TokenExample("["),
            new TokenExample("10"),
            new TokenExample("]"),
        ]);

        //assert
        e.Message.Should().Be(expected);
    }

    [Theory]
    [MemberData(nameof(TokensData))]
    public void ExampleOfTokenValue_WhenCalledWithType_ThenReturnsCorrectString(IToken param, string expected)
    {
        //arrange
        //act
        var actual = InvalidTokenCollectionException.ExampleOfTokenValue(param, param.GetType());

        //assert
        actual.Should().Be(expected);
    }

    public static TheoryData<IToken, string> TokensData =>
        new()
        {
            { new DummyTokens.Select(), "select" },
            { new DummyTokens.Where(), "where" },
            { new DummyTokens.OrderBy(), "orderBy" },
            { new DummyTokens.OrderByDescending(), "orderByDescending" },
            { new DummyTokens.ThenBy(), "thenBy" },
            { new DummyTokens.ThenByDescending(), "thenByDescending" },
            { new DummyTokens.LeftSquareBracket(), "[" },
            { new DummyTokens.RightSquareBracket(), "]" },
            { new DummyTokens.Entity(), "x" },
            { new DummyTokens.Dot(), "." },
            { new DummyTokens.PropertyName(), "Name" },
            { new DummyTokens.Skip(), "skip" },
            { new DummyTokens.Take(), "take" },
            { new DummyTokens.Equality(), "==" },
            { new DummyTokens.NameOrValue(), "Jan" },
            { new DummyTokens.And(), "&&" },
            { new DummyTokens.Or(), "||" },
            { new DummyTokens.MethodName(), "Method" },
            { new DummyTokens.LeftCircleBracket(), "(" },
            { new DummyTokens.RightCircleBracket(), ")" }
        };

    public class DummyTokens
    {
        public record Select : IToken
        {
            public Range Range => default;

            public string GetValue(string str) => str;

            public override string ToString() => nameof(Select);
        }

        public record Where : IToken
        {
            public Range Range => default;

            public string GetValue(string str) => str;

            public override string ToString() => nameof(Where);
        }

        public record OrderBy : IToken
        {
            public Range Range => default;

            public string GetValue(string str) => str;

            public override string ToString() => nameof(OrderBy);
        }

        public record OrderByDescending : IToken
        {
            public Range Range => default;

            public string GetValue(string str) => str;

            public override string ToString() => nameof(OrderByDescending);
        }

        public record ThenBy : IToken
        {
            public Range Range => default;

            public string GetValue(string str) => str;

            public override string ToString() => nameof(ThenBy);
        }

        public record ThenByDescending : IToken
        {
            public Range Range => default;

            public string GetValue(string str) => str;

            public override string ToString() => nameof(ThenByDescending);
        }

        public record LeftSquareBracket : IToken
        {
            public Range Range => default;

            public string GetValue(string str) => str;

            public override string ToString() => nameof(LeftSquareBracket);
        }

        public record RightSquareBracket : IToken
        {
            public Range Range => default;

            public string GetValue(string str) => str;

            public override string ToString() => nameof(RightSquareBracket);
        }

        public record Entity : IToken
        {
            public Range Range => default;

            public string GetValue(string str) => str;

            public override string ToString() => nameof(Entity);
        }

        public record Dot : IToken
        {
            public Range Range => default;

            public string GetValue(string str) => str;

            public override string ToString() => nameof(Dot);
        }

        public record PropertyName : IToken
        {
            public Range Range => default;

            public string GetValue(string str) => str;

            public override string ToString() => nameof(PropertyName);
        }

        public record Skip : IToken
        {
            public Range Range => default;

            public string GetValue(string str) => str;

            public override string ToString() => nameof(Skip);
        }

        public record Take : IToken
        {
            public Range Range => default;

            public string GetValue(string str) => str;

            public override string ToString() => nameof(Take);
        }

        public record Equality : IToken
        {
            public Range Range => default;

            public string GetValue(string str) => str;

            public override string ToString() => nameof(Equality);
        }

        public record NameOrValue : IToken
        {
            public Range Range => default;

            public string GetValue(string str) => str;

            public override string ToString() => nameof(NameOrValue);
        }

        public record And : IToken
        {
            public Range Range => default;

            public string GetValue(string str) => str;

            public override string ToString() => nameof(And);
        }

        public record Or : IToken
        {
            public Range Range => default;

            public string GetValue(string str) => str;

            public override string ToString() => nameof(Or);
        }

        public record MethodName : IToken
        {
            public Range Range => default;

            public string GetValue(string str) => str;

            public override string ToString() => nameof(MethodName);
        }

        public record RightCircleBracket : IToken
        {
            public Range Range => default;

            public string GetValue(string str) => str;

            public override string ToString() => nameof(RightCircleBracket);
        }

        public record LeftCircleBracket : IToken
        {
            public Range Range => default;

            public string GetValue(string str) => str;

            public override string ToString() => nameof(LeftCircleBracket);
        }
    }
}
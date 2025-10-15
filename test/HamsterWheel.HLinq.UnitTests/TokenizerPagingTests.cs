using static HamsterWheel.HLinq.UnitTests.Assertions.ExpectedToken;

namespace HamsterWheel.HLinq.UnitTests;

partial class HLinqTokenizerUnitTests
{
    [Fact]
    public void Tokenize_WhenHaveOnlyPaging_CanParse()
    {
        const string query = "skip[10].take[20]";
        var tokens = _sut.Tokenize(query);
        tokens.Should().HaveSequenceOf(query, [
            Skip,
            LeftSquareBracket,
            NameOrValue("10"),
            RightSquareBracket,
            Dot,
            Take,
            LeftSquareBracket,
            NameOrValue("20"),
            RightSquareBracket
        ]);
    }

    [Fact]
    public void Tokenize_WhenHaveOnlySkip_CanParse()
    {
        const string query = "skip[10]";
        var tokens = _sut.Tokenize(query);
        tokens.Should().HaveSequenceOf(query, [
            Skip,
            LeftSquareBracket,
            NameOrValue("10"),
            RightSquareBracket
        ]);
    }

    [Fact]
    public void Tokenize_WhenHaveOnlyTake_CanParse()
    {
        const string query = "take[10]";
        var tokens = _sut.Tokenize(query);
        tokens.Should().HaveSequenceOf(query, [
            Take,
            LeftSquareBracket,
            NameOrValue("10"),
            RightSquareBracket
        ]);
    }

    [Fact]
    public void Tokenize_WhenHaveTakeSkip_CanParse()
    {
        const string query = "take[10].skip[1]";
        var tokens = _sut.Tokenize(query);
        tokens.Should().HaveSequenceOf(query, [
            Take,
            LeftSquareBracket,
            NameOrValue("10"),
            RightSquareBracket,
            Dot,
            Skip,
            LeftSquareBracket,
            NameOrValue("1"),
            RightSquareBracket
        ]);
    }
}
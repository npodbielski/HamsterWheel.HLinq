using FluentAssertions;
using HamsterWheel.HLinq.UnitTests.TestUtils;
using static HamsterWheel.HLinq.UnitTests.TestUtils.Assertions.ExpectedToken;

namespace HamsterWheel.HLinq.UnitTests.Pipeline.Tokenizer;

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

    [Fact]
    public void Tokenize_WhenTakeHasRange_CanParse()
    {
        // the first '.' of `10..20` must not be tokenized as Dot or Unknown
        const string query = "take[10..20]";
        var tokens = _sut.Tokenize(query);
        tokens.Should().HaveSequenceOf(query, [
            Take,
            LeftSquareBracket,
            NameOrValue("10"),
            RangeOperator,
            NameOrValue("20"),
            RightSquareBracket
        ]);
    }

    [Theory]
    [InlineData("take[10..]")]
    [InlineData("take[..20]")]
    [InlineData("take[..]")]
    public void Tokenize_WhenTakeHasOpenEndedRange_CanParse(string query)
    {
        // take[..] is rejected later by the parser, the tokenizer happily produces a `..` for it
        var tokens = _sut.Tokenize(query);
        tokens.Count(t => t.GetType() == RangeOperator.Type).Should().Be(1);
        tokens.Count(t => t.GetType().Name == "Unknown").Should().Be(0);
        tokens.Last().GetType().Should().Be(RightSquareBracket.Type);
    }

    [Fact]
    public void Tokenize_WhenTakeHasOpenEndedRange_CanParseValues()
    {
        const string query = "take[10..]";
        var tokens = _sut.Tokenize(query);
        tokens.Should().HaveSequenceOf(query, [
            Take,
            LeftSquareBracket,
            NameOrValue("10"),
            RangeOperator,
            RightSquareBracket
        ]);

        const string query2 = "take[..20]";
        var tokens2 = _sut.Tokenize(query2);
        tokens2.Should().HaveSequenceOf(query2, [
            Take,
            LeftSquareBracket,
            RangeOperator,
            NameOrValue("20"),
            RightSquareBracket
        ]);
    }

    [Fact]
    public void Tokenize_WhenSkipThenTakeRange_CanParse()
    {
        const string query = "skip[5].take[10..20]";
        var tokens = _sut.Tokenize(query);
        tokens.Should().HaveSequenceOf(query, [
            Skip,
            LeftSquareBracket,
            NameOrValue("5"),
            RightSquareBracket,
            Dot,
            Take,
            LeftSquareBracket,
            NameOrValue("10"),
            RangeOperator,
            NameOrValue("20"),
            RightSquareBracket
        ]);
    }

    [Theory]
    [InlineData("take[10 .. 20]")]
    [InlineData("take[ 10..20 ]")]
    [InlineData("take[ .. 20 ]")]
    public void Tokenize_WhenTakeRangeHasWhiteSpaces_CanParse(string query)
    {
        var tokens = _sut.Tokenize(query);
        tokens.Count(t => t.GetType() == RangeOperator.Type).Should().Be(1);
        tokens.Count(t => t.GetType().Name == "Unknown").Should().Be(0);
    }
}
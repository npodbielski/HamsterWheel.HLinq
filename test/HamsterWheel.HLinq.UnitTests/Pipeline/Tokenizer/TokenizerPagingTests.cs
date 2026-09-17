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

    [Theory]
    [InlineData("where[x.Double==1.21]", new[] { "1.21" })] // a single dot belongs to the value...
    [InlineData("take[10.2]", new[] { "10.2" })] // ...the value stays one token even when it is not a valid take number...
    [InlineData("take[10..20]", new[] { "20" })] // ...a double dot ends it: `10..` is one RangeOperator token
    public void Tokenize_WhenValueContainsDots_ThenAmbiguityResolvedByTokenGrammars(string query, string[] values)
    {
        // NameOrValue never holds `..` and RangeOperator matches the number in front of `..` (`10..`),
        // so the plain single window walk decides `1.21` vs `10..20` without peeking at syntax
        var tokens = _sut.Tokenize(query);

        tokens.Where(t => t.GetType().Name == "NameOrValue").Select(t => t.GetValue(query))
            .Should().Equal(values);
        tokens.Count(t => t.GetType() == RangeOperator.Type)
            .Should().Be(query.Contains("..", StringComparison.Ordinal) ? 1 : 0);
    }

    [Fact]
    public void Tokenize_WhenTakeHasRange_CanParse()
    {
        // the first '.' of `10..20` must not be tokenized as Dot or Unknown and the start number
        // is matched together with the operator: one `10..` RangeOperator token
        const string query = "take[10..20]";
        var tokens = _sut.Tokenize(query);
        tokens.Should().HaveSequenceOf(query, [
            Take,
            LeftSquareBracket,
            RangeOperatorWithStart("10"),
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
            RangeOperatorWithStart("10"),
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

    [Theory]
    [InlineData("take[0..10]", "0", "10")] // first page of ten results
    [InlineData("take[1..1000]", "1", "1000")]
    [InlineData("take[1000..1010]", "1000", "1010")]
    [InlineData("take[676766..66878878]", "676766", "66878878")]
    public void Tokenize_WhenTakeRangeHasBigNumbers_CanParse(string query, string start, string end)
    {
        // the number of digits does not matter, `1000..` commits on the second dot just like `1..`
        var tokens = _sut.Tokenize(query);

        tokens.Should().HaveSequenceOf(query, [
            Take,
            LeftSquareBracket,
            RangeOperatorWithStart(start),
            NameOrValue(end),
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
            RangeOperatorWithStart("10"),
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
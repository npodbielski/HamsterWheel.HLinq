using HamsterWheel.HLinq.UnitTests.Extensions;
using static HamsterWheel.HLinq.UnitTests.Extensions.Assertions.ExpectedToken;

namespace HamsterWheel.HLinq.UnitTests;

public partial class HLinqTokenizerUnitTests
{
    [Fact]
    public void Tokenize_WhenLeadingUpperCase_CanParse()
    {
        const string query = "OrderBy[x.Name]";
        var tokens = _sut.Tokenize(query);
        tokens.Should().HaveSequenceOf(query, [
            OrderBy,
            LeftSquareBracket,
            Entity(),
            Dot,
            Prop("Name"),
            RightSquareBracket
        ]);
    }

    [Fact]
    public void Tokenize_WhenKeywordUpperCase_CanParse()
    {
        const string query = "ORDERBY[x.Name]";
        var tokens = _sut.Tokenize(query);
        tokens.Should().HaveSequenceOf(query, [
            OrderBy,
            LeftSquareBracket,
            Entity(),
            Dot,
            Prop("Name"),
            RightSquareBracket
        ]);
    }

    [Fact]
    public void Tokenize_WhenKeywordLowerCase_CanParse()
    {
        const string query = "orderby[x.Name]";
        var tokens = _sut.Tokenize(query);
        tokens.Should().HaveSequenceOf(query, [
            OrderBy,
            LeftSquareBracket,
            Entity(),
            Dot,
            Prop("Name"),
            RightSquareBracket
        ]);
    }

    [Fact]
    public void Tokenize_WhenStrangeCasing_CanParse()
    {
        const string query = "oRdErByDeScEnDiNg[x.Name]";
        var tokens = _sut.Tokenize(query);
        tokens.Should().HaveSequenceOf(query, [
            OrderByDescending,
            LeftSquareBracket,
            Entity(),
            Dot,
            Prop("Name"),
            RightSquareBracket
        ]);
    }
}
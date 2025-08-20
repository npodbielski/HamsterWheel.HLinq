using HamsterWheel.HLinq.UnitTests.Extensions;
using static HamsterWheel.HLinq.UnitTests.Extensions.Assertions.ExpectedToken;

namespace HamsterWheel.HLinq.UnitTests;

public partial class HLinqTokenizerUnitTests
{
    [Fact]
    public void Tokenize_WhenHaveLeadingWhiteSpace_CanParse()
    {
        const string query = "   orderBy[x.Name]";
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
    public void Tokenize_WhenHaveWhiteSpaceBeforeLeftSquareBracket_CanParse()
    {
        const string query = "orderBy  [x.Name]";
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
    public void Tokenize_WhenHaveWhiteSpaceAfterLeftSquareBracket_CanParse()
    {
        const string query = "orderBy[\tx.Name]";
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
    public void Tokenize_WhenHaveWhiteSpaceAfterEntity_CanParse()
    {
        const string query = "orderBy[x\r\n.Name]";
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
    public void Tokenize_WhenHaveWhiteSpaceAfterDot_CanParse()
    {
        const string query = "orderBy[x.                        Name]";
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
    public void Tokenize_WhenHaveWhiteSpaceBeforeRightSquareBracket_CanParse()
    {
        const string query = "orderBy[x.Name                  ]";
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
    public void Tokenize_WhenHaveTrailingWhiteSpace_CanParse()
    {
        const string query = "orderBy[x.Name]    \t\t\r\n                      ";
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
    public void Tokenize_WhenHaveWhiteSpaceBetweenRoot_CanParse()
    {
        const string query = "orderBy[x.Name]      .\tskip[10].\r\ntake[100]";
        var tokens = _sut.Tokenize(query);
        tokens.Should().HaveSequenceOf(query, [
            OrderBy,
            LeftSquareBracket,
            Entity(),
            Dot,
            Prop("Name"),
            RightSquareBracket,
            Dot,
            Skip,
            LeftSquareBracket,
            NameOrValue("10"),
            RightSquareBracket,
            Dot,
            Take,
            LeftSquareBracket,
            NameOrValue("100"),
            RightSquareBracket
        ]);
    }
}
using static HamsterWheel.HLinq.UnitTests.Assertions.ExpectedToken;

namespace HamsterWheel.HLinq.UnitTests;

partial class HLinqTokenizerUnitTests
{
    [Fact]
    public void Tokenize_WhenHaveOrderThenBy_CanParse()
    {
        const string query = "orderBy[x.Name].thenBy[x.Id]";
        var tokens = _sut.Tokenize(query);
        tokens.Should().HaveSequenceOf(query, [
            OrderBy,
            LeftSquareBracket,
            Entity(),
            Dot,
            Prop("Name"),
            RightSquareBracket,
            Dot,
            ThenBy,
            LeftSquareBracket,
            Entity(),
            Dot,
            Prop("Id"),
            RightSquareBracket
        ]);
    }

    [Fact]
    public void Tokenize_WhenHaveOnlyOrder_CanParse()
    {
        const string query = "orderBy[x.Name]";
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
    public void Tokenize_WhenHaveOnlyOrderByDescending_CanParse()
    {
        const string query = "orderByDescending[x.Name]";
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

    [Fact]
    public void Tokenize_WhenHaveOnlyOrderByDescendingthenBy_CanParse()
    {
        const string query = "orderByDescending[x.Name].thenBy[x.Id]";
        var tokens = _sut.Tokenize(query);
        tokens.Should().HaveSequenceOf(query, [
            OrderByDescending,
            LeftSquareBracket,
            Entity(),
            Dot,
            Prop("Name"),
            RightSquareBracket,
            Dot,
            ThenBy,
            LeftSquareBracket,
            Entity(),
            Dot,
            Prop("Id"),
            RightSquareBracket
        ]);
    }

    [Fact]
    public void Tokenize_WhenHaveOnlyOrderBythenByDesc_CanParse()
    {
        const string query = "orderBy[x.Name].thenByDescending[x.Id]";
        var tokens = _sut.Tokenize(query);
        tokens.Should().HaveSequenceOf(query, [
            OrderBy,
            LeftSquareBracket,
            Entity(),
            Dot,
            Prop("Name"),
            RightSquareBracket,
            Dot,
            ThenByDescending,
            LeftSquareBracket,
            Entity(),
            Dot,
            Prop("Id"),
            RightSquareBracket
        ]);
    }

    [Fact]
    public void Tokenize_WhenHaveOnlyOrderByDescthenByDesc_CanParse()
    {
        const string query = "orderByDescending[x.Name].thenByDescending[x.Id]";
        var tokens = _sut.Tokenize(query);
        tokens.Should().HaveSequenceOf(query, [
            OrderByDescending,
            LeftSquareBracket,
            Entity(),
            Dot,
            Prop("Name"),
            RightSquareBracket,
            Dot,
            ThenByDescending,
            LeftSquareBracket,
            Entity(),
            Dot,
            Prop("Id"),
            RightSquareBracket
        ]);
    }
}
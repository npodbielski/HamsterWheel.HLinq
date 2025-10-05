using static HamsterWheel.HLinq.UnitTests.Assertions.ExpectedToken;

namespace HamsterWheel.HLinq.UnitTests;

partial class HLinqTokenizerUnitTests
{
    [Fact]
    public void Tokenize_WhenSelectWithOneProp_CanParse()
    {
        const string query = "select[x.Name]";
        var tokens = _sut.Tokenize(query);
        tokens.Should().HaveSequenceOf(query, [
            Select,
            LeftSquareBracket,
            Entity(),
            Dot,
            Prop("Name"),
            RightSquareBracket
        ]);
    }

    [Fact]
    public void Tokenize_WhenOnePropRename_CanParse()
    {
        const string query = "select[Mode=x.Name]";
        var tokens = _sut.Tokenize(query);
        tokens.Should().HaveSequenceOf(query, [
            Select,
            LeftSquareBracket,
            NameOrValue("Mode"),
            Assignment,
            Entity(),
            Dot,
            Prop("Name"),
            RightSquareBracket
        ]);
    }

    [Fact]
    public void Tokenize_WhenSelectWithConstValueProperty_CanParse()
    {
        const string query = "select[Directory=Core]";
        var tokens = _sut.Tokenize(query);
        tokens.Should().HaveSequenceOf(query, [
            Select,
            LeftSquareBracket,
            NameOrValue("Directory"),
            Assignment,
            NameOrValue("Core"),
            RightSquareBracket
        ]);
    }

    [Fact]
    public void Tokenize_WhenSelectWithSourcePropAndWithConstValueProperty_CanParse()
    {
        const string query = "select[x.Id,Directory=Core]";
        var tokens = _sut.Tokenize(query);
        tokens.Should().HaveSequenceOf(query, [
            Select,
            LeftSquareBracket,
            Entity(),
            Dot,
            Prop("Id"),
            Comma,
            NameOrValue("Directory"),
            Assignment,
            NameOrValue("Core"),
            RightSquareBracket
        ]);
    }

    [Fact]
    public void Tokenize_WhenSelectWithConstValuePropertyAndWithSourceProp_CanParse()
    {
        const string query = "select[Directory=Core,x.Name]";
        var tokens = _sut.Tokenize(query);
        tokens.Should().HaveSequenceOf(query, [
            Select,
            LeftSquareBracket,
            NameOrValue("Directory"),
            Assignment,
            NameOrValue("Core"),
            Comma,
            Entity(),
            Dot,
            Prop("Name"),
            RightSquareBracket
        ]);
    }

    [Fact]
    public void Tokenize_WhenSelectWithTwoProps_CanParse()
    {
        const string query = "select[x.Name, x.Id]";
        var tokens = _sut.Tokenize(query);
        tokens.Should().HaveSequenceOf(query, [
            Select,
            LeftSquareBracket,
            Entity(),
            Dot,
            Prop("Name"),
            Comma,
            Entity(),
            Dot,
            Prop("Id"),
            RightSquareBracket
        ]);
    }

    [Fact]
    public void Tokenize_WhenSelectWithSourcePropertyAndConstValueProperty_CanParse()
    {
        const string query = "select[x.Name,Directory=Core]";
        var tokens = _sut.Tokenize(query);
        tokens.Should().HaveSequenceOf(query, [
            Select,
            LeftSquareBracket,
            Entity(),
            Dot,
            Prop("Name"),
            Comma,
            NameOrValue("Directory"),
            Assignment,
            NameOrValue("Core"),
            RightSquareBracket
        ]);
    }

    [Fact]
    public void Tokenize_WhenSelectWitNestedProp_CanParse()
    {
        const string query = "select[x.Nested.Id]";
        var tokens = _sut.Tokenize(query);
        tokens.Should().HaveSequenceOf(query, [
            Select,
            LeftSquareBracket,
            Entity(),
            Dot,
            Prop("Nested"),
            Dot,
            Prop("Id"),
            RightSquareBracket
        ]);
    }
}
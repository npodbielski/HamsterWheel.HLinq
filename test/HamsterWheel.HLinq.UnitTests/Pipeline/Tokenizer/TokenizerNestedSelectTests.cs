using HamsterWheel.HLinq.UnitTests.TestUtils;
using static HamsterWheel.HLinq.UnitTests.TestUtils.Assertions.ExpectedToken;

namespace HamsterWheel.HLinq.UnitTests.Pipeline.Tokenizer;

/// <summary>
/// The call a nested select is written as: a navigation, the select word on it, and the members
/// of the related row between the brackets. The alias of the element is free text that ends at
/// the dot of the path it roots - a value swallows its dots, this never does, and only inside
/// the open call of a select.
/// </summary>
partial class HLinqTokenizerUnitTests
{
    [Fact]
    public void Tokenize_WhenNestedSelect_ThenAliasSeparatesFromItsName()
    {
        const string query = "select[x.Addresses.select(a.Street,a.City)]";
        var tokens = _sut.Tokenize(query);
        tokens.Should().HaveSequenceOf(query, [
            Select, LeftSquareBracket,
            Entity(), Dot, Prop("Addresses"), Dot, MethodName("select"), LeftCircleBracket,
            SubEntity("a"), Dot, Prop("Street"), Comma, SubEntity("a"), Dot, Prop("City"),
            RightCircleBracket, RightSquareBracket
        ]);
    }

    [Fact]
    public void Tokenize_WhenNestedSelectRenamed_ThenRenamesOnBothSides()
    {
        const string query = "select[addresses=x.Addresses.select(contact=x.Name,a.City)]";
        var tokens = _sut.Tokenize(query);
        tokens.Should().HaveSequenceOf(query, [
            Select, LeftSquareBracket,
            NameOrValue("addresses"), Assignment,
            Entity(), Dot, Prop("Addresses"), Dot, MethodName("select"), LeftCircleBracket,
            NameOrValue("contact"), Assignment, Entity(), Dot, Prop("Name"),
            Comma, SubEntity("a"), Dot, Prop("City"),
            RightCircleBracket, RightSquareBracket
        ]);
    }

    [Fact]
    public void Tokenize_WhenNestedSelectFollowedByComma_ThenCommaFollowsTheBracket()
    {
        const string query = "select[x.Addresses.select(a.Street),x.Name]";
        var tokens = _sut.Tokenize(query);
        tokens.Should().HaveSequenceOf(query, [
            Select, LeftSquareBracket,
            Entity(), Dot, Prop("Addresses"), Dot, MethodName("select"), LeftCircleBracket,
            SubEntity("a"), Dot, Prop("Street"),
            RightCircleBracket, Comma, Entity(), Dot, Prop("Name"),
            RightSquareBracket
        ]);
    }

    [Fact]
    public void Tokenize_WhenNestedSelectInsideNestedSelect_ThenInnerCallStandsAsACall()
    {
        const string query = "select[x.Addresses.select(a.Street,country=a.Country.select(c.Name))]";
        var tokens = _sut.Tokenize(query);
        tokens.Should().HaveSequenceOf(query, [
            Select, LeftSquareBracket,
            Entity(), Dot, Prop("Addresses"), Dot, MethodName("select"), LeftCircleBracket,
            SubEntity("a"), Dot, Prop("Street"), Comma,
            NameOrValue("country"), Assignment,
            SubEntity("a"), Dot, Prop("Country"), Dot, MethodName("select"), LeftCircleBracket,
            SubEntity("c"), Dot, Prop("Name"),
            RightCircleBracket, RightCircleBracket, RightSquareBracket
        ]);
    }

    [Fact]
    public void Tokenize_WhenValueHoldsDotsOutsideOfACall_ThenItStaysOneValue()
    {
        //the alias is asked only inside an open call of a select; a value of a rename keeps the
        //dots it always kept
        const string query = "select[Version=1.2]";
        var tokens = _sut.Tokenize(query);
        tokens.Should().HaveSequenceOf(query, [
            Select, LeftSquareBracket, NameOrValue("Version"), Assignment, NameOrValue("1.2"),
            RightSquareBracket
        ]);
    }

    [Fact]
    public void Tokenize_WhenValueHoldsDotInsideACall_ThenItStaysOneValue()
    {
        //an argument that is a value with a dot - a version as a constant of a filter method -
        //is not an alias: an alias roots a path and so ends at a dot of its own
        const string query = "where[x.Name.StartsWith(v.2)]";
        var tokens = _sut.Tokenize(query);
        tokens.Should().HaveSequenceOf(query, [
            Where, LeftSquareBracket,
            Entity(), Dot, Prop("Name"), Dot, MethodName("StartsWith"), LeftCircleBracket,
            NameOrValue("v.2"),
            RightCircleBracket, RightSquareBracket
        ]);
    }
}

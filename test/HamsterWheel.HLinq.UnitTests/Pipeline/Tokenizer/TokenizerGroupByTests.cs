using FluentAssertions;
using HamsterWheel.HLinq.Pipeline.Tokenizer;
using HamsterWheel.HLinq.UnitTests.TestUtils;
using static HamsterWheel.HLinq.UnitTests.TestUtils.Assertions.ExpectedToken;

namespace HamsterWheel.HLinq.UnitTests.Pipeline.Tokenizer;

partial class HLinqTokenizerUnitTests
{
    [Fact]
    public void Tokenize_WhenGroupByOneProp_CanParse()
    {
        const string query = "groupBy[x.Status]";
        var tokens = _sut.Tokenize(query);
        tokens.Should().HaveSequenceOf(query, [
            GroupBy,
            LeftSquareBracket,
            Entity(),
            Dot,
            Prop("Status"),
            RightSquareBracket
        ]);
    }

    [Fact]
    public void Tokenize_WhenGroupByCompositeKey_CanParse()
    {
        const string query = "groupBy[x.Vendor, x.Status]";
        var tokens = _sut.Tokenize(query);
        tokens.Should().HaveSequenceOf(query, [
            GroupBy,
            LeftSquareBracket,
            Entity(),
            Dot,
            Prop("Vendor"),
            Comma,
            Entity(),
            Dot,
            Prop("Status"),
            RightSquareBracket
        ]);
    }

    [Fact]
    public void Tokenize_WhenGroupByThenSelect_CanParse()
    {
        const string query = "groupBy[x.Status].select[x.Name]";
        var tokens = _sut.Tokenize(query);
        tokens.Should().HaveSequenceOf(query, [
            GroupBy,
            LeftSquareBracket,
            Entity(),
            Dot,
            Prop("Status"),
            RightSquareBracket,
            Dot,
            Select,
            LeftSquareBracket,
            Entity(),
            Dot,
            Prop("Name"),
            RightSquareBracket
        ]);
    }

    [Fact]
    public void Tokenize_WhenGroupByThenCount_CanParse()
    {
        const string query = "groupBy[x.Status].count[]";
        var tokens = _sut.Tokenize(query);
        tokens.Should().HaveSequenceOf(query, [
            GroupBy,
            LeftSquareBracket,
            Entity(),
            Dot,
            Prop("Status"),
            RightSquareBracket,
            Dot,
            Count,
            LeftSquareBracket,
            RightSquareBracket
        ]);
    }

    [Fact]
    public void Tokenize_WhenGroupByThenWhereOnKey_ThenTheComparisonIsAComparison()
    {
        const string query = "groupBy[x.Status].where[x.Key==Paid]";
        var tokens = _sut.Tokenize(query);
        tokens.Should().HaveSequenceOf(query, [
            GroupBy,
            LeftSquareBracket,
            Entity(),
            Dot,
            Prop("Status"),
            RightSquareBracket,
            Dot,
            Where,
            LeftSquareBracket,
            Entity(),
            Dot,
            Prop("Key"),
            Equality,
            NameOrValue("Paid"),
            RightSquareBracket
        ]);
    }
}

using FluentAssertions;
using HamsterWheel.HLinq.Pipeline.Tokenizer;
using HamsterWheel.HLinq.UnitTests.TestUtils;
using HamsterWheel.HLinq.UnitTests.TestUtils.Assertions;
using static HamsterWheel.HLinq.UnitTests.TestUtils.Assertions.ExpectedToken;

namespace HamsterWheel.HLinq.UnitTests.Pipeline.Tokenizer;

/// <summary>
///     The clause a token stands in is part of its adjacency rule, so the same character is a
///     different token per clause. These tests pin that scope: a rename exists only inside a select
///     clause and a comparison only outside it, which is what lets both tokens spell themselves with
///     their own characters; the exact streams of these queries live in the tokenizer corpus.
/// </summary>
partial class HLinqTokenizerUnitTests
{
    [Fact]
    public void Tokenize_WhenEqualsInsideSelectClause_ThenRenames()
    {
        const string query = "select[FullName=x.Name]";
        var tokens = _sut.Tokenize(query);
        tokens.Should().HaveSequenceOf(query, [
            Select,
            LeftSquareBracket,
            NameOrValue("FullName"),
            Assignment,
            Entity(),
            Dot,
            Prop("Name"),
            RightSquareBracket
        ]);
    }

    [Fact]
    public void Tokenize_WhenEqualsOutsideSelectClause_ThenUnknownToken()
    {
        //`=` is the rename of a select clause and nothing else; a comparison spells itself with `==`
        const string query = "where[x.Int=10]";

        var act = () => _sut.Tokenize(query);

        act.Should().Throw<HLinqTokenizer.UnknownTokenException>();
    }

    [Fact]
    public void Tokenize_WhenDoubleEqualsInsideWhereClause_ThenSingleEqualsIsNotCommittedFirst()
    {
        const string query = "where[Name==5]";
        var tokens = _sut.Tokenize(query);
        tokens.Should().HaveSequenceOf(query, [
            Where,
            LeftSquareBracket,
            NameOrValue("Name"),
            Equality,
            NameOrValue("5"),
            RightSquareBracket
        ]);
    }

    [Fact]
    public void Tokenize_WhenComparisonInsideSelectClause_ThenUnknownToken()
    {
        const string query = "select[x.Name==test]";

        var act = () => _sut.Tokenize(query);

        act.Should().Throw<HLinqTokenizer.UnknownTokenException>();
    }

    [Fact]
    public void Tokenize_WhenComparisonAfterSelectClause_ThenCompares()
    {
        const string query = "select[FullName=x.Name].where[x.Int==10]";
        var tokens = _sut.Tokenize(query);
        tokens.Should().HaveSequenceOf(query, [
            Select,
            LeftSquareBracket,
            NameOrValue("FullName"),
            Assignment,
            Entity(),
            Dot,
            Prop("Name"),
            RightSquareBracket,
            Dot,
            Where,
            LeftSquareBracket,
            Entity(),
            Dot,
            Prop("Int"),
            Equality,
            NameOrValue("10"),
            RightSquareBracket
        ]);
    }

    [Fact]
    public void Tokenize_WhenValueStartsWithEntityLetter_ThenValueIsNotEntity()
    {
        const string query = "where[x.Name==xander]";
        var tokens = _sut.Tokenize(query);
        tokens.Should().HaveSequenceOf(query, [
            Where,
            LeftSquareBracket,
            Entity(),
            Dot,
            Prop("Name"),
            Equality,
            NameOrValue("xander"),
            RightSquareBracket
        ]);
    }

    [Fact]
    public void Tokenize_WhenValueComparesProperty_ThenComparisonKeepsItsPlace()
    {
        const string query = "where[10<x.Int]";
        var tokens = _sut.Tokenize(query);
        tokens.Should().HaveSequenceOf(query, [
            Where,
            LeftSquareBracket,
            NameOrValue("10"),
            ExpectedToken.LessThan,
            Entity(),
            Dot,
            Prop("Int"),
            RightSquareBracket
        ]);
    }
}

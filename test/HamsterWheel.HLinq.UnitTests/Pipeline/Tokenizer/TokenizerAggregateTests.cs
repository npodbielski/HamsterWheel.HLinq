using HamsterWheel.HLinq.UnitTests.TestUtils;
using static HamsterWheel.HLinq.UnitTests.TestUtils.Assertions.ExpectedToken;

namespace HamsterWheel.HLinq.UnitTests.Pipeline.Tokenizer;

partial class HLinqTokenizerUnitTests
{
    //an aggregate is a call over the group written in a value position, and the empty argument list
    //(`()`) is what tells `Count` from a property named Count without the tokenizer peeking anywhere
    [Fact]
    public void Tokenize_WhenGroupByThenWhereCount_ThenEmptyBracketsAreTheirOwnTokens()
    {
        const string query = "groupBy[x.Status].where[x.Count()>5]";
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
            MethodName("Count"),
            LeftCircleBracket,
            RightCircleBracket,
            GreaterThan,
            NameOrValue("5"),
            RightSquareBracket
        ]);
    }

    [Fact]
    public void Tokenize_WhenOrderByMaxWithPropertyArgument_ThenArgumentIsAPropertyPath()
    {
        const string query = "groupBy[x.Status].orderBy[x.Max(x.Age)]";
        var tokens = _sut.Tokenize(query);
        tokens.Should().HaveSequenceOf(query, [
            GroupBy,
            LeftSquareBracket,
            Entity(),
            Dot,
            Prop("Status"),
            RightSquareBracket,
            Dot,
            OrderBy,
            LeftSquareBracket,
            Entity(),
            Dot,
            MethodName("Max"),
            LeftCircleBracket,
            Entity(),
            Dot,
            Prop("Age"),
            RightCircleBracket,
            RightSquareBracket
        ]);
    }

    [Fact]
    public void Tokenize_WhenSelectAggregateAssignment_ThenCallFollowsTheAssignment()
    {
        const string query = "groupBy[x.Status].select[C=x.Count()]";
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
            NameOrValue("C"),
            Assignment,
            Entity(),
            Dot,
            MethodName("Count"),
            LeftCircleBracket,
            RightCircleBracket,
            RightSquareBracket
        ]);
    }

    [Fact]
    public void Tokenize_WhenSelectNavigationCount_ThenCallHangsOnThePropertyPath()
    {
        const string query = "select[N=x.Addresses.Count()]";
        var tokens = _sut.Tokenize(query);
        tokens.Should().HaveSequenceOf(query, [
            Select,
            LeftSquareBracket,
            NameOrValue("N"),
            Assignment,
            Entity(),
            Dot,
            Prop("Addresses"),
            Dot,
            MethodName("Count"),
            LeftCircleBracket,
            RightCircleBracket,
            RightSquareBracket
        ]);
    }

    //the parenthesis is never the start of a written value, which is what lets `()` close an empty
    //argument list instead of becoming a one-character value - a value that merely holds parentheses
    //stays a value
    [Fact]
    public void Tokenize_WhenValueFollowsEmptyCall_ThenOnlyTheClosingBracketIsTheBracket()
    {
        const string query = "where[x.Count()==x.Name]";
        var tokens = _sut.Tokenize(query);
        tokens.Should().HaveSequenceOf(query, [
            Where,
            LeftSquareBracket,
            Entity(),
            Dot,
            MethodName("Count"),
            LeftCircleBracket,
            RightCircleBracket,
            Equality,
            Entity(),
            Dot,
            Prop("Name"),
            RightSquareBracket
        ]);
    }
}

using HamsterWheel.HLinq.Pipeline.Tokenizer;
using FluentAssertions;
using HamsterWheel.HLinq.UnitTests.TestUtils;
using static HamsterWheel.HLinq.UnitTests.TestUtils.Assertions.ExpectedToken;

namespace HamsterWheel.HLinq.UnitTests.Pipeline.Tokenizer;

/// <summary>
/// The math operators are words of the select clause and of nothing else.
/// </summary>
public class TokenizerMathTests
{
    private static readonly TestServicesCollection TestServicesCollection = new();

    [Fact]
    public void Tokenize_WhenJoinedMembers_ThenOperatorStandsBetweenThem()
    {
        const string query = "select[FullName=x.Name+\" \"+x.Surname]";
        var tokens = TestServicesCollection.Tokenizer.Tokenize(query);

        tokens.Should().HaveSequenceOf(query, [
            Select,
            LeftSquareBracket,
            NameOrValue("FullName"),
            Assignment,
            Entity(),
            Dot,
            Prop("Name"),
            Plus,
            Quoted(" "),
            Plus,
            Entity(),
            Dot,
            Prop("Surname"),
            RightSquareBracket
        ]);
    }

    [Fact]
    public void Tokenize_WhenUnnamedExpression_ThenOperatorOpensTheMember()
    {
        const string query = "select[x.Name+\" \"+x.Surname]";
        var tokens = TestServicesCollection.Tokenizer.Tokenize(query);

        tokens.Should().HaveSequenceOf(query, [
            Select,
            LeftSquareBracket,
            Entity(),
            Dot,
            Prop("Name"),
            Plus,
            Quoted(" "),
            Plus,
            Entity(),
            Dot,
            Prop("Surname"),
            RightSquareBracket
        ]);
    }

    [Theory]
    [InlineData("select[N=x.Int*2]", "*")]
    [InlineData("select[N=x.Int/2]", "/")]
    [InlineData("select[N=x.Int-2]", "-")]
    public void Tokenize_WhenOtherOperators_ThenEachIsItsOwnWord(string query, string keyword)
    {
        var tokens = TestServicesCollection.Tokenizer.Tokenize(query);

        tokens.Select(t => t.GetValue(query)).Should().ContainInOrder(
            "select", "[", "N", "=", "x", ".", "Int", keyword, "2", "]");
    }

    [Fact]
    public void Tokenize_WhenExpressionInsideNestedSelect_ThenOperatorsStandInTheInnerBrackets()
    {
        const string query = "select[A=x.Addresses.select(City=a.City+\" \"+a.City)]";
        var tokens = TestServicesCollection.Tokenizer.Tokenize(query);

        tokens.Should().HaveSequenceOf(query, [
            Select,
            LeftSquareBracket,
            NameOrValue("A"),
            Assignment,
            Entity(),
            Dot,
            Prop("Addresses"),
            Dot,
            MethodName("select"),
            LeftCircleBracket,
            NameOrValue("City"),
            Assignment,
            SubEntity("a"),
            Dot,
            Prop("City"),
            Plus,
            Quoted(" "),
            Plus,
            SubEntity("a"),
            Dot,
            Prop("City"),
            RightCircleBracket,
            RightSquareBracket
        ]);
    }

    [Fact]
    public void Tokenize_WhenQuotedTextOpensTheValue_ThenQuotesSayTextOfTheirOwn()
    {
        //a text opens an expression inside a group: the parentheses say arithmetic, and inside
        //them a written value is read by the grammar of an operand
        const string query = "select[FullName=(\"Mr \"+x.Name)]";
        var tokens = TestServicesCollection.Tokenizer.Tokenize(query);

        tokens.Should().HaveSequenceOf(query, [
            Select,
            LeftSquareBracket,
            NameOrValue("FullName"),
            Assignment,
            LeftCircleBracket,
            Quoted("Mr "),
            Plus,
            Entity(),
            Dot,
            Prop("Name"),
            RightCircleBracket,
            RightSquareBracket
        ]);
    }

    [Fact]
    public void Tokenize_WhenQuotedTextStandsAloneAfterTheEquals_ThenItIsTheFreeValueItAlwaysWas()
    {
        //an expression opens with a member, a call or a group: after the '=' alone the quotes
        //belong to the free value grammar of the clause, the shipped `Name="x"` says text
        const string query = "select[FullName=\"Mr \"]";
        var tokens = TestServicesCollection.Tokenizer.Tokenize(query);

        tokens.Should().HaveSequenceOf(query, [
            Select,
            LeftSquareBracket,
            NameOrValue("FullName"),
            Assignment,
            NameOrValue("\"Mr \""),
            RightSquareBracket
        ]);
    }

    [Fact]
    public void Tokenize_WhenExpressionHangsOnCalls_ThenOperatorFollowsTheClosingBracket()
    {
        const string query = "select[U=x.Name.ToUpper()+\"!\"+x.Text]";
        var tokens = TestServicesCollection.Tokenizer.Tokenize(query);

        tokens.Should().HaveSequenceOf(query, [
            Select,
            LeftSquareBracket,
            NameOrValue("U"),
            Assignment,
            Entity(),
            Dot,
            Prop("Name"),
            Dot,
            MethodName("ToUpper"),
            LeftCircleBracket,
            RightCircleBracket,
            Plus,
            Quoted("!"),
            Plus,
            Entity(),
            Dot,
            Prop("Text"),
            RightSquareBracket
        ]);
    }

    [Fact]
    public void Tokenize_WhenExpressionIsOneMemberAmongMany_ThenMembersStaySeparate()
    {
        const string query = "select[FullName=x.Name+\"-\"+x.Int,x.Id]";
        var tokens = TestServicesCollection.Tokenizer.Tokenize(query);

        tokens.Should().HaveSequenceOf(query, [
            Select,
            LeftSquareBracket,
            NameOrValue("FullName"),
            Assignment,
            Entity(),
            Dot,
            Prop("Name"),
            Plus,
            Quoted("-"),
            Plus,
            Entity(),
            Dot,
            Prop("Int"),
            Comma,
            Entity(),
            Dot,
            Prop("Id"),
            RightSquareBracket
        ]);
    }

    [Fact]
    public void Tokenize_WhenNumberInsideExpressionEndsTheValue_ThenNumberCompletesAtItsOwnEnd()
    {
        //the written number is not free text: it completes at the first character it cannot be
        //made of, so an expression of several operators chains without a single space
        const string query = "select[N=x.Int+1-2]";
        var tokens = TestServicesCollection.Tokenizer.Tokenize(query);

        tokens.Should().HaveSequenceOf(query, [
            Select,
            LeftSquareBracket,
            NameOrValue("N"),
            Assignment,
            Entity(),
            Dot,
            Prop("Int"),
            Plus,
            Number("1"),
            Minus,
            Number("2"),
            RightSquareBracket
        ]);
    }

    [Theory]
    [InlineData("where[x.Name==a+b]", "a+b")]
    [InlineData("where[x.Name==x+1]", "x+1")]
    [InlineData("where[x.Name==+48123]", "+48123")]
    [InlineData("where[x.Name==2023-10-12]", "2023-10-12")]
    public void Tokenize_WhenOperatorInsideAValue_ThenValueIsOneTokenAsItAlwaysWas(string query, string value)
    {
        //the rule the whole language keeps: an operator stands only after what can be an operand,
        //never after a free value - so every value that happens to hold one of these characters,
        //a date, a GUID, a search text, stays one value of its clause
        var tokens = TestServicesCollection.Tokenizer.Tokenize(query);

        tokens.Should().HaveSequenceOf(query, [
            Where,
            LeftSquareBracket,
            Entity(),
            Dot,
            Prop("Name"),
            Equality,
            NameOrValue(value),
            RightSquareBracket
        ]);
    }

    [Fact]
    public void Tokenize_WhenQuotedTextHoldsABracket_ThenTheWholeQuotedRunIsOneToken()
    {
        const string query = "select[B=\"jan braces[]\"]";
        var tokens = TestServicesCollection.Tokenizer.Tokenize(query);

        //after '=' the quoted run is the free value of the clause it always was - one token to
        //its closing quote and no further, because what follows the quote ends a free text - the
        //'[' inside never terminates anything
        tokens.Should().HaveSequenceOf(query, [
            Select,
            LeftSquareBracket,
            NameOrValue("B"),
            Assignment,
            NameOrValue("\"jan braces[]\""),
            RightSquareBracket
        ]);
    }

    [Fact]
    public void Tokenize_WhenExpressionHoldsABracketInQuotedText_ThenTheBracketIsText()
    {
        const string query = "select[B=x.Name+\" [a]\"]";
        var tokens = TestServicesCollection.Tokenizer.Tokenize(query);

        //after an operator the quoted text is the operand and the whole bracketed run is its
        //text: only the matching quote closes it, whatever characters stand between the quotes
        tokens.Should().HaveSequenceOf(query, [
            Select,
            LeftSquareBracket,
            NameOrValue("B"),
            Assignment,
            Entity(),
            Dot,
            Prop("Name"),
            Plus,
            Quoted(" [a]"),
            RightSquareBracket
        ]);
    }

    [Theory]
    [InlineData("select[x.Name+]")]
    [InlineData("select[FullName=x.Name+test]")]
    public void Tokenize_WhenNothingOperandFollowsTheOperator_ThenQueryIsRefused(string query)
    {
        //a bare word is no operand: only a member, a call, a quoted text or a number can be one,
        //and making a free text one would take the operators into the delimiters of every free
        //text of every clause - the dates and GUIDs above would stop being one value
        Action act = () => TestServicesCollection.Tokenizer.Tokenize(query);

        act.Should().Throw<HLinqTokenizer.UnknownTokenException>();
    }

    [Fact]
    public void Tokenize_WhenDifferenceComparesToValue_ThenOperatorHangsOnTheMember()
    {
        //the operators are words of `where` too: a computation that begins with a member is written
        //bare, and no free text ever ran after a member for it to contest
        const string query = "where[x.Total-x.Paid>5]";
        var tokens = TestServicesCollection.Tokenizer.Tokenize(query);

        tokens.Should().HaveSequenceOf(query, [
            Where,
            LeftSquareBracket,
            Entity(),
            Dot,
            Prop("Total"),
            Minus,
            Entity(),
            Dot,
            Prop("Paid"),
            GreaterThan,
            NameOrValue("5"),
            RightSquareBracket
        ]);
    }

    [Theory]
    [InlineData("where[x.Total-x.Paid]", "-")]
    [InlineData("where[x.Total+x.Paid]", "+")]
    [InlineData("where[x.Total/x.Paid]", "/")]
    [InlineData("where[x.Total-x.Paid&&x.Int>1]", "-")]
    [InlineData("where[x.Total-x.Paid==x.Int]", "-")]
    public void Tokenize_WhenComparisonMissingTheMathStillEndsTheValue(string query, string keyword)
    {
        //a math run ends at whatever ends a value of the clause: the closer, a logical operator, a
        //comparison - the operand's delimiters are derived from those adjacencies
        var tokens = TestServicesCollection.Tokenizer.Tokenize(query);

        tokens.Select(t => t.GetValue(query)).Should().ContainInOrder(
            "where", "[", "x", ".", "Total", keyword, "x", ".", "Paid");
    }

    [Fact]
    public void Tokenize_WhenMathHangsOnCallsAndMembers_ThenBothSidesMayCarryIt()
    {
        const string query = "where[x.Name.ToUpper()+x.Name==q]";
        var tokens = TestServicesCollection.Tokenizer.Tokenize(query);

        tokens.Should().HaveSequenceOf(query, [
            Where,
            LeftSquareBracket,
            Entity(),
            Dot,
            Prop("Name"),
            Dot,
            MethodName("ToUpper"),
            LeftCircleBracket,
            RightCircleBracket,
            Plus,
            Entity(),
            Dot,
            Prop("Name"),
            Equality,
            NameOrValue("q"),
            RightSquareBracket
        ]);
    }

    [Theory]
    [InlineData("orderBy[x.Total-x.Paid]")]
    [InlineData("orderByDescending[x.Total-x.Paid]")]
    [InlineData("thenBy[x.Total-x.Paid]")]
    [InlineData("thenByDescending[x.Total-x.Paid]")]
    public void Tokenize_WhenKeyIsAMathRun_ThenOperatorsAreWordsOfTheOrderingToo(string query)
    {
        //ordering reads its one value, and after a member no free text runs there either
        var tokens = TestServicesCollection.Tokenizer.Tokenize(query);

        var root = query[..(query.IndexOf('['))];
        tokens.Select(t => t.GetValue(query)).Should().ContainInOrder(
            root, "[", "x", ".", "Total", "-", "x", ".", "Paid", "]");
    }

    [Fact]
    public void Tokenize_WhenGroupStandsOnAValueSide_ThenTheBracketOpensTheMath()
    {
        //a `(` whose previous token is a comparison opens a math group: the computation the query
        //cannot write bare, because it begins with a written value, says so with the group
        const string query = "where[x.Int>(2*3)]";
        var tokens = TestServicesCollection.Tokenizer.Tokenize(query);

        tokens.Should().HaveSequenceOf(query, [
            Where,
            LeftSquareBracket,
            Entity(),
            Dot,
            Prop("Int"),
            GreaterThan,
            LeftCircleBracket,
            Number("2"),
            Multiply,
            Number("3"),
            RightCircleBracket,
            RightSquareBracket
        ]);
    }

    [Fact]
    public void Tokenize_WhenDateSpelledAsSubtractionsStandsOnAValueSide_ThenItIsArithmetic()
    {
        //`x.Date<=(2022-12-31)` is arithmetic and `x.Date<=2022-12-31` is a date: the parentheses
        //say which of the two the query means, and the written operand reads the number the culture
        //of the query spells
        const string query = "where[x.Date<=(2022-12-31)]";
        var tokens = TestServicesCollection.Tokenizer.Tokenize(query);

        tokens.Should().HaveSequenceOf(query, [
            Where,
            LeftSquareBracket,
            Entity(),
            Dot,
            Prop("Date"),
            LessOrEqualThan,
            LeftCircleBracket,
            Number("2022"),
            Minus,
            Number("12"),
            Minus,
            Number("31"),
            RightCircleBracket,
            RightSquareBracket
        ]);
    }

    [Fact]
    public void Tokenize_WhenBracketOpensTheCondition_ThenItStaysTheConditionGroupItAlwaysWas()
    {
        //after `[`, `and` or `or` the bracket is a condition group, and a written value is no token
        //that starts after it - `where[(2-1)>x.Int]` stays a refusal the user answers by moving the
        //computation to the value side
        const string query = "where[(2-1)>x.Int]";
        var tokens = TestServicesCollection.Tokenizer.Tokenize(query);

        //the bracket after `[` opens the condition group, and inside it `2-1` is still the free
        //value it always was - no operator of the clause completes at its dash, because nothing of
        //the clause is a token after a value the walk just committed
        tokens.Should().HaveSequenceOf(query, [
            Where,
            LeftSquareBracket,
            LeftCircleBracket,
            NameOrValue("2-1"),
            RightCircleBracket,
            GreaterThan,
            Entity(),
            Dot,
            Prop("Int"),
            RightSquareBracket
        ]);
    }
}

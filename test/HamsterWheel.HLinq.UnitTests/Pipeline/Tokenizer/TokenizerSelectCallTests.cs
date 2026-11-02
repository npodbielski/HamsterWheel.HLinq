using HamsterWheel.HLinq.UnitTests.TestUtils;
using static HamsterWheel.HLinq.UnitTests.TestUtils.Assertions.ExpectedToken;

namespace HamsterWheel.HLinq.UnitTests.Pipeline.Tokenizer;

partial class HLinqTokenizerUnitTests
{
    [Fact]
    public void Tokenize_WhenStaticCallAfterAssignment_ThenMethodNameIsTheCallItIs()
    {
        const string query = "select[R=reverse(x.Name)]";
        var tokens = _sut.Tokenize(query);
        tokens.Should().HaveSequenceOf(query, [
            Select,
            LeftSquareBracket,
            NameOrValue("R"),
            Assignment,
            MethodName("reverse"),
            LeftCircleBracket,
            Entity(),
            Dot,
            Prop("Name"),
            RightCircleBracket,
            RightSquareBracket
        ]);
    }

    [Fact]
    public void Tokenize_WhenInstanceCallAfterAssignment_ThenNameStaysAPropertyAndCallHangsOnIt()
    {
        const string query = "select[U=x.Name.ToUpper()]";
        var tokens = _sut.Tokenize(query);
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
            RightSquareBracket
        ]);
    }

    //the other side of the race: a window that never reaches `(` belongs to the value alone
    [Theory]
    [InlineData("select[R=abc]", "abc")]
    [InlineData("select[R=reverse]", "reverse")]
    [InlineData("select[R=7]", "7")]
    public void Tokenize_WhenTextAfterAssignmentWithoutCall_ThenStaysAConstant(string query, string value)
    {
        var tokens = _sut.Tokenize(query);
        tokens.Should().HaveSequenceOf(query, [
            Select,
            LeftSquareBracket,
            NameOrValue("R"),
            Assignment,
            NameOrValue(value),
            RightSquareBracket
        ]);
    }

    [Fact]
    public void Tokenize_WhenCallWithWrittenArgumentsAfterAssignment_ThenArgumentsAreTheirOwnTokens()
    {
        const string query = "select[R=x.Name.Substring(0,2)]";
        var tokens = _sut.Tokenize(query);
        tokens.Should().HaveSequenceOf(query, [
            Select,
            LeftSquareBracket,
            NameOrValue("R"),
            Assignment,
            Entity(),
            Dot,
            Prop("Name"),
            Dot,
            MethodName("Substring"),
            LeftCircleBracket,
            NameOrValue("0"),
            Comma,
            NameOrValue("2"),
            RightCircleBracket,
            RightSquareBracket
        ]);
    }

    [Fact]
    public void Tokenize_WhenStaticCallInsideNestedSelect_ThenCallStandsInTheInnerBrackets()
    {
        const string query = "select[A=x.Addresses.select(City=upper(x.Name))]";
        var tokens = _sut.Tokenize(query);
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
            MethodName("upper"),
            LeftCircleBracket,
            Entity(),
            Dot,
            Prop("Name"),
            RightCircleBracket,
            RightCircleBracket,
            RightSquareBracket
        ]);
    }

    [Fact]
    public void Tokenize_WhenInstanceCallInsideNestedSelect_ThenCallHangsOnWhicheverPathItNames()
    {
        const string query = "select[A=x.Addresses.select(City=x.Name.ToUpper())]";
        var tokens = _sut.Tokenize(query);
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
            Entity(),
            Dot,
            Prop("Name"),
            Dot,
            MethodName("ToUpper"),
            LeftCircleBracket,
            RightCircleBracket,
            RightCircleBracket,
            RightSquareBracket
        ]);
    }
}

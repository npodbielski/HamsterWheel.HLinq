using FluentAssertions;
using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Pipeline.Parser;
using HamsterWheel.HLinq.Request;
using HamsterWheel.HLinq.UnitTests.TestUtils;
using HamsterWheel.HLinq.UnitTests.TestUtils.Dummies;

namespace HamsterWheel.HLinq.UnitTests.Pipeline.Parser;

/// <summary>
/// One name answering two values is refused at the parse, from the text alone.
/// </summary>
public class HLinqParserDuplicateMemberNameUnitTests
{
    private static readonly TestServicesCollection TestServicesCollection = new();
    private readonly HLinqParser _sut = new(TestServicesCollection.Parsers);

    [Theory]
    [InlineData("select[x.Text,x.Text]")] //the same value twice is still one name answering two members
    [InlineData("select[Z=x.Text,Z=x.Int]")]
    [InlineData("select[Z=x.Text,z=x.Int]")] //the name a member answers under is matched as a name is
    [InlineData("select[x.Name.ToUpper(),x.Text.ToUpper()]")] //an unnamed call names itself by the method
    [InlineData("select[Z=x.Name.Substring(0,2),Z=x.Int]")] //the arguments of a call are part of its value
    [InlineData("select[Z=2,Z=3]")] //a written value is a value like a member is
    [InlineData("groupBy[x.Text,x.Text]")]
    [InlineData("select[S=x.Children.select(Z=a.Name,Z=a.Id)]")]
    [InlineData("select[S=x.Children.select(Name=a.Name,Name=a.Id)]")]
    [InlineData("groupBy[x.Name].select[Rows=x.Rows.select(Z=a.Text,Z=a.Int)]")]
    [InlineData("groupBy[x.Name].select[Z=x.Text,Z=x.Int]")]
    public void Parse_WhenTwoMembersOfOneListAnswerUnderOneName_ThenRefusedWithTheReason(string query)
    {
        //act
        var act = () => _sut.Parse(new HLinqQuery<DummyEntity>
        {
            SourceQueryString = query
        }, TestServicesCollection.Tokenizer.Tokenize(query));

        //assert
        act.Should().Throw<MemberNameCollisionException>();
    }

    [Fact]
    public void Parse_WhenTwoMembersAnswerUnderOneName_ThenTheReasonNamesBothValues()
    {
        //act
        var act = () => _sut.Parse(new HLinqQuery<DummyEntity>
        {
            SourceQueryString = "select[Z=x.Text,Z=x.Int]"
        }, TestServicesCollection.Tokenizer.Tokenize("select[Z=x.Text,Z=x.Int]"));

        //assert
        //query itself spelled it, not as the answer would name it
        act.Should().Throw<MemberNameCollisionException>()
            .WithMessage("'Z' answers two values - 'x.Text' and 'x.Int';*");
    }

    [Theory]
    [InlineData("select[x.Text,x.Int]")]
    [InlineData("select[Z=x.Text,I=x.Int]")]
    [InlineData("select[x.Text,Z=x.Text]")] //one value may be answered twice, under two names
    [InlineData("select[x.Name.ToUpper(),U=x.Text.ToUpper()]")]
    [InlineData("groupBy[x.Text,x.Int]")]
    [InlineData("select[S=x.Children.select(Name=a.Name),T=x.Children.select(Id=a.Id)]")]
    [InlineData("select[S=x.Children.select(Name),x.Name]")] //a nested list is checked on its own
    [InlineData("groupBy[x.Name].select[Rows=x.Rows.select(a.Text),C=x.Count()]")]
    public void Parse_WhenEveryMemberAnswersUnderItsOwnName_ThenParses(string query)
    {
        //act
        var act = () => _sut.Parse(new HLinqQuery<DummyEntity>
        {
            SourceQueryString = query
        }, TestServicesCollection.Tokenizer.Tokenize(query));

        //assert
        act.Should().NotThrow();
    }
}

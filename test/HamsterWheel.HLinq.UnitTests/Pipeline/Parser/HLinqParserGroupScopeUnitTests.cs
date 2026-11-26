using FluentAssertions;
using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Pipeline.Parser;
using HamsterWheel.HLinq.Request;
using HamsterWheel.HLinq.Tree.Filtering;
using HamsterWheel.HLinq.UnitTests.TestUtils;
using HamsterWheel.HLinq.UnitTests.TestUtils.Dummies;

namespace HamsterWheel.HLinq.UnitTests.Pipeline.Parser;

/// <summary>
/// What a <c>groupBy</c> changes in the clauses after it is answered by the parse.
/// </summary>
public class HLinqParserGroupScopeUnitTests
{
    private static readonly TestServicesCollection TestServicesCollection = new();
    private readonly HLinqParser _sut = new(TestServicesCollection.Parsers);

    [Theory]
    [InlineData("groupBy[x.Name].where[x.Rows.Text==a]")]
    [InlineData("groupBy[x.Name].where[x.Rows.Count()>1]")]
    [InlineData("groupBy[x.Name].orderBy[x.Rows.Text]")]
    [InlineData("groupBy[x.Name].orderByDescending[x.Rows]")]
    [InlineData("groupBy[x.Name].thenBy[x.Rows]")]
    [InlineData("groupBy[x.Name].thenByDescending[x.Rows]")]
    public void Parse_WhenAClauseAfterAGroupNamesTheRows_ThenRefusedWithTheReason(string query)
    {
        Parse(query).Should().Throw<RowsCannotBeOrderedOrFilteredException>();
    }

    [Theory]
    [InlineData("where[x.Count()>1]")]
    [InlineData("where[x.Rows==1]")]
    [InlineData("orderBy[x.Count()]")]
    [InlineData("select[C=x.Count()]")]
    [InlineData("where[x.Name.Count()>1]")]
    [InlineData("where[x.Children.Count()>1]")]
    [InlineData("orderBy[x.Int]")]
    [InlineData("groupBy[x.Name].where[x.Count()>1]")]
    [InlineData("groupBy[x.Name].where[x.Key==New]")]
    [InlineData("groupBy[x.Name].select[C=x.Count(),Rows=x.Rows.select(a.Int)]")]
    public void Parse_WhenTheQueryNeedsNoTypeToJudge_ThenParses(string query)
    {
        Parse(query).Should().NotThrow();
    }

    [Fact]
    public void Parse_WhenEntityCallIsAnAggregate_ThenTheCallSaysSoOnTheTree()
    {
        //arrange
        const string query = "groupBy[x.Name].where[x.Count()>1]";

        //act
        var where = ParseTree(query).Children.OfType<WhereRoot>().Single();

        //assert
        Calls(where).Single().IsGroupAggregate.Should().BeTrue();
    }

    [Theory]
    [InlineData("where[x.Children.Count()>1]")] // the same name over a collection the path reached
    [InlineData("where[x.Name.ToUpper()==a]")] // a name no aggregate of the language is
    public void Parse_WhenTheCallDoesNotStandOnTheEntityAsAnAggregate_ThenTheTreeSaysSo(string query)
    {
        //act
        var where = ParseTree(query).Children.OfType<WhereRoot>().Single();

        //assert
        Calls(where).Single().IsGroupAggregate.Should().BeFalse();
    }

    [Theory]
    [InlineData("groupBy[x.Name].select[Key=x.Count()]")]
    [InlineData("groupBy[x.Name].select[Rows=x.Count()]")]
    [InlineData("groupBy[x.Name].select[rows=x.count()]")]
    [InlineData("groupBy[x.Name].select[C=x.Count(),Rows=x.Sum(x.Int)]")]
    [InlineData("groupBy[x.Name].select[A=x.Count(),A=x.Sum(x.Int)]")]
    public void Parse_WhenAnAggregateTakesANameTheGroupAnswersUnder_ThenRefusedWithTheReason(string query)
    {
        //act
        //both facts of the text, so no expression of the answer has to be built to refuse this
        var act = () => _sut.Parse(new HLinqQuery<DummyEntity>
        {
            SourceQueryString = query
        }, TestServicesCollection.Tokenizer.Tokenize(query));

        //assert
        act.Should().Throw<GroupAggregateNameCollisionException>();
    }

    //a call is a child of the tree like a property is, so the test walks to it rather than
    //widening src with a walk nothing else asks for
    private static IEnumerable<Method> Calls(ITreeBranch branch)
    {
        foreach (var child in branch.Children)
        {
            switch (child)
            {
                case Method method:
                    yield return method;
                    break;
                case ITreeBranch nested:
                    foreach (var deeper in Calls(nested))
                    {
                        yield return deeper;
                    }

                    break;
            }
        }
    }

    private Action Parse(string query) => () => ParseTree(query);

    private IHLinqQuery ParseTree(string query)
    {
        var hlinqQuery = new HLinqQuery<DummyEntity>
        {
            SourceQueryString = query
        };
        return _sut.Parse(hlinqQuery, TestServicesCollection.Tokenizer.Tokenize(query));
    }
}

using FluentAssertions;
using HamsterWheel.HLinq.Pipeline;
using HamsterWheel.HLinq.Pipeline.Parser;
using HamsterWheel.HLinq.Pipeline.Tokenizer;
using HamsterWheel.HLinq.Request;
using HamsterWheel.HLinq.UnitTests.TestUtils;
using HamsterWheel.HLinq.UnitTests.TestUtils.Dummies;

namespace HamsterWheel.HLinq.UnitTests.Pipeline.Applier;

public class HLinqQueryGroupByCollisionUnitTests
{
    private static readonly TestServicesCollection ServicesCollection = new();
    private readonly IHLinqTokenizer _tokenizer = ServicesCollection.Tokenizer;
    private readonly HLinqParser _parser = new(ServicesCollection.Parsers);

    private readonly HLinqQueryApplier _sut = ServicesCollection.NewApplier();

    [Fact]
    public void Apply_WhenGroupedByANameTheEntityHasItself_ThenTheEntitiesOwnPropertyWinsAndItIsReported()
    {
        //arrange
        var rows = new[]
        {
            new EntityWithItsOwnKey { Key = "a", Name = "n1" },
            new EntityWithItsOwnKey { Key = "b", Name = "n2" },
            new EntityWithItsOwnKey { Key = "a", Name = "n3" },
        };
        var queryString = "groupBy[x.Key].select[x.Key, x.Name]";
        var hlinqQuery = new HLinqQuery<EntityWithItsOwnKey> { SourceQueryString = queryString };
        _parser.Parse(hlinqQuery, _tokenizer.Tokenize(queryString));

        //act
        var actual = (object[])_sut.Apply(rows.AsQueryable(), hlinqQuery);

        //assert
        actual.Select(e => e.GetType().GetProperty("Key")!.GetValue(e)).Should().BeEquivalentTo(["a", "b"]);
        var entryA = actual.Single(e => e.GetType().GetProperty("Key")!.GetValue(e)!.Equals("a"));
        var row = ((System.Collections.IEnumerable)entryA.GetType().GetProperty("Rows")!.GetValue(entryA)!)
            .Cast<object>().First();
        row.GetType().GetProperty("Key")!.GetValue(row).Should().Be("a");

        //and the collision is reported, naming both candidates
        hlinqQuery.Diagnostics.Warnings.Should().NotBeEmpty();
        hlinqQuery.Diagnostics.Warnings
            .Should().AllSatisfy(w => w.Competitors.Should()
                .Contain(["EntityWithItsOwnKey.Key", "group.Key"]));
    }

    [Fact]
    public void Apply_WhenNoGroupByAndEntityHasItsOwnRows_ThenTheNameIsTheEntitysProperty()
    {
        //arrange
        var rows = new[]
        {
            new EntityWithItsOwnRows { Name = "n1", Rows = [1, 2] },
            new EntityWithItsOwnRows { Name = "n2", Rows = [] },
        };
        var queryString = "select[x.Name, x.Rows]";
        var hlinqQuery = new HLinqQuery<EntityWithItsOwnRows> { SourceQueryString = queryString };
        _parser.Parse(hlinqQuery, _tokenizer.Tokenize(queryString));

        //act
        var actual = (object[])_sut.Apply(rows.AsQueryable(), hlinqQuery);

        //assert
        RowsValueOf(actual[0]).Should().Equal(1, 2);
        RowsValueOf(actual[1]).Should().BeEmpty();
        hlinqQuery.Diagnostics.Warnings.Should().BeEmpty();
    }

    private static List<int> RowsValueOf(object row) =>
        ((System.Collections.IEnumerable)row.GetType().GetProperty("Rows")!.GetValue(row)!).Cast<int>().ToList();
}

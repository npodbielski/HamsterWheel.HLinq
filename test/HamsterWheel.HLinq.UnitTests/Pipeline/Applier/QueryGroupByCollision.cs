using FluentAssertions;
using HamsterWheel.HLinq.Pipeline;
using HamsterWheel.HLinq.Pipeline.Applier;
using HamsterWheel.HLinq.Pipeline.Applier.Builders;
using HamsterWheel.HLinq.Pipeline.Parser;
using HamsterWheel.HLinq.Pipeline.Tokenizer;
using HamsterWheel.HLinq.Reflection;
using HamsterWheel.HLinq.Request;
using HamsterWheel.HLinq.UnitTests.TestUtils;
using HamsterWheel.HLinq.UnitTests.TestUtils.Dummies;
using Microsoft.Extensions.DependencyInjection;

namespace HamsterWheel.HLinq.UnitTests.Pipeline.Applier;

public class HLinqQueryGroupByCollisionUnitTests
{
    private static readonly TestServicesCollection ServicesCollection = new();
    private readonly IHLinqTokenizer _tokenizer = ServicesCollection.Tokenizer;
    private readonly HLinqParser _parser = new(ServicesCollection.Parsers);

    private readonly HLinqQueryApplier _sut =
        new(ServicesCollection.Provider.GetRequiredService<IElementApplierFactory>(),
            ServicesCollection.Provider.GetRequiredService<IMethodsCache>(),
            ServicesCollection.Provider.GetRequiredService<IExpressionBuilder>());

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
        var actual = (object[])_sut.Apply(rows.AsQueryable(), hlinqQuery)!;

        //assert - the entity's own Key is what x.Key means everywhere else, so it means that here too
        actual.Select(e => e.GetType().GetProperty("Key")!.GetValue(e)).Should().BeEquivalentTo(["a", "b"]);
        var entryA = actual.Single(e => e.GetType().GetProperty("Key")!.GetValue(e)!.Equals("a"));
        var row = ((System.Collections.IEnumerable)entryA.GetType().GetProperty("Rows")!.GetValue(entryA)!)
            .Cast<object>().First();
        row.GetType().GetProperty("Key")!.GetValue(row).Should().Be("a");

        //and the collision is reported, naming both candidates
        hlinqQuery.Warnings.Reported.Should().NotBeEmpty();
        hlinqQuery.Warnings.Reported
            .Should().AllSatisfy(w => w.Competitors.Should()
                .Contain(["EntityWithItsOwnKey.Key", "group.Key"]));
    }
}

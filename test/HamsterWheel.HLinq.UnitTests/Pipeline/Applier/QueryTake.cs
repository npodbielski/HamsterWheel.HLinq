using FluentAssertions;
using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Pipeline.Applier;
using HamsterWheel.HLinq.Pipeline.Parser;
using HamsterWheel.HLinq.Pipeline.Tokenizer;
using HamsterWheel.HLinq.Request;
using HamsterWheel.HLinq.Tree.Paging;
using HamsterWheel.HLinq.UnitTests.TestUtils.Dummies;
using Microsoft.Extensions.DependencyInjection;

namespace HamsterWheel.HLinq.UnitTests.Pipeline.Applier;

partial class HLinqQueryApplierUnitTests
{
    private static DummyEntity[] Entities(int count) =>
        Enumerable.Range(0, count).Select(i => new DummyEntity($"entity{i}")).ToArray();

    private object Apply(string queryString, IQueryable<DummyEntity> queryable)
    {
        var hlinqQuery = new HLinqQuery<DummyEntity>
        {
            SourceQueryString = queryString
        };
        var tokens = _tokenizer.Tokenize(queryString);
        _parser.Parse(hlinqQuery, tokens);
        return _sut.Apply(queryable, hlinqQuery)!;
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(0, 5)]
    [InlineData(1, 3)]
    [InlineData(3, 5)]
    [InlineData(5, 20)]
    [InlineData(19, 20)]
    [InlineData(0, 25)]
    [InlineData(24, 25)]
    [InlineData(25, 26)] // range beyond data, skip yields nothing
    [InlineData(1, 1000)] // big numbers, uncapped (MaxTake is 1000)
    [InlineData(1000, 1010)]
    [InlineData(676766, 66878878)] // the count is capped at MaxTake, the skip is not - empty anyway
    [InlineData(20, 30)] // second page of ten over 25 rows, data exhausted -> 5 rows, no throw
    [InlineData(10, 20)] // second page of ten over 25 rows, fully inside data
    public void Apply_WhenTakeRange_ThenSameAsSkipThenTake(int start, int end)
    {
        //arrange
        var entities = Entities(25);

        //act
        var actual = Apply($"take[{start}..{end}]", entities.AsQueryable());

        //assert
        actual.Should().BeEquivalentTo(entities.Skip(start).Take(end - start));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(24)]
    public void Apply_WhenTakeRangeOpenEnd_ThenSameAsSkip(int start)
    {
        //arrange
        var entities = Entities(25);

        //act
        var actual = Apply($"take[{start}..]", entities.AsQueryable());

        //assert
        actual.Should().BeEquivalentTo(entities.Skip(start));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(10)]
    public void Apply_WhenTakeRangeOpenStart_ThenSameAsTake(int end)
    {
        //arrange
        var entities = Entities(25);

        //act
        var actual = Apply($"take[..{end}]", entities.AsQueryable());

        //assert
        actual.Should().BeEquivalentTo(entities.Take(end));
    }

    [Fact]
    public void Apply_WhenSkipThenTakeRange_ThenCanApply()
    {
        //arrange
        var entities = Entities(25);

        //act
        var actual = Apply("skip[2].take[3..7]", entities.AsQueryable());

        //assert
        actual.Should().BeEquivalentTo(entities.Skip(2).Skip(3).Take(4));
    }

    [Theory]
    [InlineData("take[5..2]")] // start after end
    [InlineData("take[3..3]")] // empty range
    [InlineData("take[0..0]")] // empty range
    [InlineData("take[-1..2]")] // negative start
    [InlineData("take[1..-2]")] // negative end
    public void Apply_WhenInvalidTakeRangeValues_ThenThrows(string queryString)
    {
        //arrange
        var entities = Entities(25);

        //act
        var action = () => Apply(queryString, entities.AsQueryable());

        //assert
        action.Should().Throw<InvalidTokenCollectionException>();
    }

    [Fact]
    public void Apply_WhenTakeRangeHasNonNumericValues_ThenThrowsOnConversion()
    {
        //arrange
        var entities = Entities(25);

        //act
        // `take[a..b]` is rejected by the tokenizer already (RangeOperator grammar reads `10..`
        // together with a number), the open start side still fails on conversion only
        var action = () => Apply("take[..b]", entities.AsQueryable());

        //assert
        action.Should().Throw<InvalidConstantStringToTypeConversionException>();
    }

    [Theory]
    [InlineData("take[0..10]", 3)] // count capped at MaxTake
    [InlineData("take[..10]", 3)] // open start, capped count
    [InlineData("take[1..4]", 3)] // 10 - 1 = 9 capped at MaxTake
    [InlineData("take[5..]", 20)] // take[5..] is a plain skip - never capped
    public void Apply_WhenTakeRangeCountBiggerThanMaxTake_ThenCapsAtMaxTake(string queryString, int expected)
    {
        //arrange
        var services = new ServiceCollection();
        HLinqCore.ConfigureServices(services, new HLinqOptions
        {
            HttpDefaultMaxTakeRecords = 3
        });
        // HLinqCore.ConfigureServices registers IHLinqOptions again with defaults, last registration wins
        services.AddSingleton<IHLinqOptions>(new HLinqOptions
        {
            HttpDefaultMaxTakeRecords = 3
        });
        var provider = services.BuildServiceProvider();
        var entities = Entities(25);
        var hlinqQuery = new HLinqQuery<DummyEntity>
        {
            SourceQueryString = queryString
        };
        var tokens = provider.GetRequiredService<IHLinqTokenizer>().Tokenize(queryString);
        provider.GetRequiredService<IHLinqParser>().Parse(hlinqQuery, tokens);

        //act
        var actual = provider.GetRequiredService<IHLinqQueryApplier>().Apply(entities.AsQueryable(), hlinqQuery);

        //assert
        ((Array)actual!).Length.Should().Be(expected);
    }

    [Fact]
    public void ApplyTo_WhenQueryHasTakeRange_ThenNoEmptyTakeIsAppended()
    {
        //arrange
        var entities = Entities(25);
        var binder = ServicesCollection.Provider.GetRequiredService<IHLinqQueryBinder>();

        //act
        var query = (HLinqQuery<DummyEntity>)binder.BindQuery("take[10..20]", typeof(HLinqQuery<DummyEntity>));
        var actual = query.ApplyTo(entities.AsQueryable());

        //assert - HLinqQuery.ApplyTo appends an empty take[] when the query has no take,
        //a range take is an explicit take so exactly one take root must remain
        query.Children.Should().ContainSingle(c => c is TakeRoot);
        actual.Should().BeEquivalentTo(entities.Skip(10).Take(10));
    }
}

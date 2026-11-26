using FluentAssertions;
using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Request;
using HamsterWheel.HLinq.Tree.Filtering;
using HamsterWheel.HLinq.UnitTests.TestUtils.Dummies;

namespace HamsterWheel.HLinq.UnitTests.Pipeline.Applier;

/// <summary>
/// A clause reads its values by what answers them, never by a child position.
/// </summary>
partial class HLinqQueryApplierUnitTests
{
    [Fact]
    public void Apply_WhenCountOfACollectionIsComparedWithAValue_ThenKeepsTheRowsWithEnoughOfThem()
    {
        //act
        var actual = Apply("where[x.Children.Count()>1]", RowsWithOwnChildren().AsQueryable());

        //assert
        Names(actual).Should().Equal("two", "three");
    }

    [Fact]
    public void Apply_WhenAValueIsComparedWithTheCountOfACollection_ThenKeepsTheSameRows()
    {
        //act
        //whatever that side answers; the comparison itself keeps the order it was written in
        var actual = Apply("where[1<x.Children.Count()]", RowsWithOwnChildren().AsQueryable());

        //assert
        Names(actual).Should().Equal("two", "three");
    }

    [Fact]
    public void Apply_WhenCountOfACollectionIsComparedWithAMember_ThenBothSidesAreBuilt()
    {
        //act
        //and not in place of a written value
        var actual = Apply("where[x.Children.Count()>x.Int]", RowsWithOwnChildren().AsQueryable());

        //assert
        Names(actual).Should().Equal("three");
    }

    [Fact]
    public void Apply_WhenTwoCollectionsOfARowAreCountedAgainstEachOther_ThenBothCallsAreBuilt()
    {
        //act
        var actual = ApplyNames("where[x.Prices.Count()>x.Tokens.Count()]", PriceRows().AsQueryable());

        //assert
        actual.Should().Equal("wide");
    }

    [Fact]
    public void Apply_WhenAComparisonStandsBesideTheCountOfACollection_ThenBothNarrowTheRows()
    {
        //act
        var actual = Apply("where[x.Children.Count()>1].where[x.Int>=3]",
            RowsWithOwnChildren().AsQueryable());

        //assert
        Names(actual).Should().Equal("two");
    }

    [Fact]
    public void Apply_WhenTheLengthOfTextIsComparedWithAValue_ThenKeepsTheLongerText()
    {
        //act
        var actual = Apply("where[x.Name.Length>4]", RowsWithOwnChildren().AsQueryable());

        //assert
        Names(actual).Should().Equal("three");
    }

    [Fact]
    public void Apply_WhenAComparisonOfACountIsJoinedWithAnother_ThenAllOfThemNarrowTheRows()
    {
        //act
        var actual = Apply("where[x.Children.Count()>1||x.Name==two]", RowsWithOwnChildren().AsQueryable());

        //assert
        Names(actual).Should().Equal("two", "three");
    }

    [Fact]
    public void Apply_WhenOrderedByTheCountOfACollection_ThenRowsStandByTheirOwnCount()
    {
        //act
        var actual = Apply("orderBy[x.Children.Count()]", RowsWithOwnChildren().AsQueryable());

        //assert
        Names(actual).Should().Equal("none", "one", "two", "three");
    }

    [Fact]
    public void Apply_WhenOrderedByTheCountOfACollectionDescending_ThenRowsStandBackwards()
    {
        //act
        var actual = Apply("orderByDescending[x.Children.Count()]", RowsWithOwnChildren().AsQueryable());

        //assert
        Names(actual).Should().Equal("three", "two", "one", "none");
    }

    [Fact]
    public void Apply_WhenOrderedByACountAndThenByAMember_ThenBothDecideTheOrder()
    {
        //act
        var actual = Apply("orderBy[x.Children.Count()].thenBy[x.Name]", RowsWithOwnChildren().AsQueryable());

        //assert
        Names(actual).Should().Equal("none", "one", "two", "three");
    }

    [Fact]
    public void Apply_WhenOrderedByTheGreatestValueOfACollection_ThenRowsStandByThatValue()
    {
        //act
        //in and whatever their own name says
        var actual = ApplyNames("orderBy[x.Prices.Max()]", PriceRows().AsQueryable());

        //assert
        actual.Should().Equal("narrow", "wide");
    }

    [Fact]
    public void Apply_WhenOrderedByACallOverText_ThenRowsStandByWhatTheCallAnswers()
    {
        //act
        var actual = Apply("orderBy[x.Name.ToUpper()]", RowsWithOwnChildren().AsQueryable());

        //assert
        Names(actual).Should().Equal("none", "one", "three", "two");
    }

    [Fact]
    public void Apply_WhenCountOfACollectionIsTheWholeCondition_ThenRefusedWithTheReason()
    {
        //act
        var act = () => Apply("where[x.Children.Count()]", RowsWithOwnChildren().AsQueryable());

        //assert
        act.Should().Throw<Condition.NotSupportedConditionException>();
    }

    [Fact]
    public void Apply_WhenAnUnknownNameIsCalledOverTheRow_ThenRefusedWithTheAggregateNames()
    {
        //act
        var act = () => Apply("where[x.Nope()>1]", RowsWithOwnChildren().AsQueryable());

        //assert
        act.Should().Throw<AggregateMethodToExpressionConverter.NotAnAggregateException>();
    }

    [Fact]
    public void Apply_WhenAnUnknownNameIsCalledOverACollection_ThenRefusedWithTheMethodsOfTheCollection()
    {
        //act
        //collection, and a `List` has none of that name
        var act = () => Apply("where[x.Children.Nope()>1]", RowsWithOwnChildren().AsQueryable());

        //assert
        act.Should().Throw<Method.InvalidMethodException>();
    }

    [Fact]
    public void Apply_WhenAnUnknownMemberIsCalledOver_ThenRefusedWithTheMembersOfTheRow()
    {
        //act
        //member is refused by its own reason and not by the call
        var act = () => Apply("where[x.Nope.Count()>1]", RowsWithOwnChildren().AsQueryable());

        //assert
        act.Should().Throw<InvalidPropertyPathException>();
    }

    [Fact]
    public void Apply_WhenCountIsCalledOverText_ThenRefusedWithTheReason()
    {
        //act
        var act = () => Apply("where[x.Name.Count()>1]", RowsWithOwnChildren().AsQueryable());

        //assert
        act.Should().Throw<AggregateMethodToExpressionConverter.AggregateArgumentsException>();
    }

    [Fact]
    public void Apply_WhenTheGreatestOfRowsIsAskedWithoutAMemberToFold_ThenRefusedWithTheReason()
    {
        //act
        var act = () => Apply("where[x.Children.Max()>1]", RowsWithOwnChildren().AsQueryable());

        //assert
        act.Should().Throw<AggregateMethodToExpressionConverter.AggregateArgumentsException>();
    }

    [Fact]
    public void Apply_WhenOnlyValuesAreCompared_ThenRefusedWithTheReason()
    {
        //act
        var act = () => Apply("where[1==1]", RowsWithOwnChildren().AsQueryable());

        //assert
        act.Should().Throw<Condition.ConditionElementMustHaveLeftAndRightException>();
    }

    private static DummyEntity[] RowsWithOwnChildren()
    {
        var rows = new[]
        {
            new DummyEntity("none") { Int = 1 },
            new DummyEntity("one") { Int = 2 },
            new DummyEntity("two") { Int = 3 },
            new DummyEntity("three") { Int = 2 },
        };
        rows[1].Children.Add(new NestedDummyEntity());
        rows[2].Children.AddRange([new NestedDummyEntity(), new NestedDummyEntity()]);
        rows[3].Children.AddRange([new NestedDummyEntity(), new NestedDummyEntity(), new NestedDummyEntity()]);
        return rows;
    }

    //two collections of the same row, so a comparison has a call on both of its sides
    private static PriceRow[] PriceRows() =>
    [
        new PriceRow([4, 5], ["a"], "wide"),
        new PriceRow([3], ["b", "c"], "narrow"),
    ];

    private static IEnumerable<string> Names(object actual) =>
        ((object[])actual).Cast<DummyEntity>().Select(e => e.Name);

    private object ApplyValue<T>(string queryString, IQueryable<T> queryable)
    {
        var hlinqQuery = new HLinqQuery<T>
        {
            SourceQueryString = queryString
        };
        _parser.Parse(hlinqQuery, _tokenizer.Tokenize(queryString));
        return _sut.Apply(queryable, hlinqQuery);
    }

    /// <summary>
    /// The names the shaped rows answer under.
    /// </summary>
    private IEnumerable<string> ApplyNames<T>(string queryString, IQueryable<T> queryable) =>
        [.. ((object[])ApplyValue(queryString, queryable))
            .Select(e => e.GetType().GetProperty("Name")!.GetValue(e)!.ToString()!)];

    private sealed class PriceRow(int[] prices, string[] tokens, string name)
    {
        // ReSharper disable once UnusedMember.Local -> the queries of this file read them
        public List<int> Prices { get; set; } = [.. prices];

        // ReSharper disable once UnusedMember.Local -> the queries of this file read them
        public List<string> Tokens { get; set; } = [.. tokens];

        // ReSharper disable once UnusedMember.Local -> the queries of this file read it
        public string Name { get; set; } = name;
    }
}

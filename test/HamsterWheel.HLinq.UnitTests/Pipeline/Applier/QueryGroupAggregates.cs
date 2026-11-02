using FluentAssertions;
using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Pipeline.Applier;
using HamsterWheel.HLinq.Tree.Filtering;
using HamsterWheel.HLinq.UnitTests.TestUtils.Dummies;

namespace HamsterWheel.HLinq.UnitTests.Pipeline.Applier;

partial class HLinqQueryApplierUnitTests
{
    [Fact]
    public void Apply_WhenGroupByThenSelectCount_ThenAnswersCountBesideKeyAndRows()
    {
        //act
        var actual = AggregatesOf(ApplyAggregated("groupBy[x.Name].select[C=x.Count()]"));

        //assert
        ValueOf(actual["New"], "C").Should().Be(2);
        ValueOf(actual["Paid"], "C").Should().Be(1);
        ValueOf(actual["New"], GroupingState.RowsName).Should().BeAssignableTo<System.Collections.IEnumerable>();
    }

    [Fact]
    public void Apply_WhenGroupByThenSelectEveryAggregate_ThenEachAnswersItsValue()
    {
        //act
        var actual = AggregatesOf(ApplyAggregated(
            "groupBy[x.Name].select[C=x.Count(),S=x.Sum(x.Int),A=x.Average(x.Int),Mi=x.Min(x.Int),M=x.Max(x.Int)]"));

        //assert
        var entry = actual["New"];
        ValueOf(entry, "C").Should().Be(2);
        ValueOf(entry, "S").Should().Be(40);
        ValueOf(entry, "A").Should().Be(20.0);
        ValueOf(entry, "Mi").Should().Be(10);
        ValueOf(entry, "M").Should().Be(30);
    }

    [Fact]
    public void Apply_WhenGroupByThenWhereCount_ThenKeepsOnlyGroupsTheAggregateAnswersTrue()
    {
        //act
        var actual = AsDictionary(ApplyAggregated("groupBy[x.Name].where[x.Count()>1]"));

        //assert
        actual.Keys.Should().BeEquivalentTo("New");
    }

    [Fact]
    public void Apply_WhenGroupByThenWhereKeyAndCount_ThenBothNarrowTheGroups()
    {
        //act
        var actual = AsDictionary(ApplyAggregated("groupBy[x.Name].where[x.Count()>1].where[x.Key==New]"));

        //assert
        actual.Keys.Should().BeEquivalentTo("New");
    }

    [Fact]
    public void Apply_WhenGroupByThenOrderByCount_ThenOrdersGroupsByTheAggregate()
    {
        //act
        var actual = AsDictionary(ApplyAggregated("groupBy[x.Name].where[x.Count()>0].orderBy[x.Max(x.Int)]"));

        //assert - New (max 30) before Paid (40) before Sent (50)
        actual.Keys.Should().ContainInOrder("New", "Paid", "Sent");
    }

    [Fact]
    public void Apply_WhenGroupByThenOrderByDescendingCount_ThenOrdersGroupsBackwards()
    {
        //act
        var actual = AsDictionary(ApplyAggregated("groupBy[x.Name].where[x.Count()>0].orderByDescending[x.Count()]"));

        //assert - the two-row group first, the one-row groups keep their arrival order
        actual.Keys.First().Should().Be("New");
    }

    [Fact]
    public void Apply_WhenGroupByThenAggregateWhereThenTake_ThenPagesTheFilteredGroups()
    {
        //act
        var actual = AsDictionary(ApplyAggregated("groupBy[x.Name].where[x.Count()>0].take[2]"));

        //assert
        actual.Should().HaveCount(2);
    }

    [Fact]
    public void Apply_WhenGroupByThenWhereCountThenCount_ThenCountsTheFilteredGroups()
    {
        //act
        var actual = ApplyAggregated("groupBy[x.Name].where[x.Count()>1].count[]");

        //assert
        actual.Should().Be(1);
    }

    [Fact]
    public void Apply_WhenGroupByThenSelectAggregateWithRowSelect_ThenRowsTakeTheSelectedShape()
    {
        //act - one select naming a row member and an aggregate: the member shapes the rows, the
        //aggregate rides beside Key and Rows
        var actual = AggregatesOf(ApplyAggregated("groupBy[x.Name].select[x.Text,C=x.Count()]"));

        //assert
        ValueOf(actual["New"], "C").Should().Be(2);
        ((System.Collections.IEnumerable)ValueOf(actual["New"], GroupingState.RowsName))
            .Cast<object>().Should().HaveCount(2);
    }

    [Fact]
    public void Apply_WhenNestedGroupByThenSelectCount_ThenCountsTheOuterGroupsOfRows()
    {
        //act
        var actual = AggregatesOf(ApplyAggregated("groupBy[x.Name].groupBy[x.Int].select[C=x.Count()]"));

        //assert - the aggregate counts the outer group's rows, written above the inner grouping it
        //keeps answering the groups of the chain
        ValueOf(actual["New"], "C").Should().Be(2);
        ValueOf(actual["Paid"], "C").Should().Be(1);
    }

    [Fact]
    public void Apply_WhenGroupByThenSelectAggregateNamedKey_ThenRefusedWithTheReason()
    {
        //act - the answer already names Key; a second property of that name has no meaning
        var act = () => ApplyAggregated("groupBy[x.Name].select[Key=x.Count()]");

        //assert
        act.Should().Throw<AggregateMethodToExpressionConverter.AggregateNameCollisionException>();
    }

    [Fact]
    public void Apply_WhenGroupByThenAggregateWithBadArgument_ThenRefused()
    {
        //act
        var act = () => ApplyAggregated("groupBy[x.Name].orderBy[x.Max(x.Nope)]");

        //assert
        act.Should().Throw<InvalidPropertyPathException>();
    }

    [Fact]
    public void Apply_WhenGroupByThenCountWithPredicate_ThenRefusedUntilConditionsInsideCallsExist()
    {
        //act - a condition inside the parentheses is the nested-conditions grammar, which the
        //tokenizer does not read yet - the query is refused where it is written
        var act = () => ApplyAggregated("groupBy[x.Name].where[x.Count(x.Int>5)>1]");

        //assert
        act.Should().Throw<InvalidTokenCollectionException>();
    }

    [Fact]
    public void Apply_WhenNotGroupedByThenEntityAggregate_ThenRefusedWithTheReason()
    {
        //act - there is no group to aggregate over when nothing was grouped
        var act = () => ApplyAggregated("where[x.Count()>2]");

        //assert
        act.Should().Throw<EntityAggregateNeedsGroupException>();
    }

    [Fact]
    public void Apply_WhenSelectEntityAggregateWithoutGroup_ThenRefusedWithTheReason()
    {
        //act
        var act = () => ApplyAggregated("select[C=x.Count()]");

        //assert
        act.Should().Throw<EntityAggregateNeedsGroupException>();
    }

    [Fact]
    public void Apply_WhenEntityCallIsNotAnAggregate_ThenRefusedWithTheAggregateNames()
    {
        //act
        var act = () => ApplyAggregated("groupBy[x.Name].where[x.Nope()>2]");

        //assert
        act.Should().Throw<AggregateMethodToExpressionConverter.NotAnAggregateException>();
    }

    [Fact]
    public void Apply_WhenGroupByThenWhereAny_ThenKeepsTheNonEmptyGroups()
    {
        //act - Any folds a collection like Count does, so a group answers it too
        var actual = AsDictionary(ApplyAggregated("groupBy[x.Name].where[x.Any()]"));

        //assert - every group of the data has a row, none is emptied
        actual.Should().HaveCount(3);
    }

    [Fact]
    public void Apply_WhenGroupByThenAggregateWithPredicateShapedArgument_ThenRefused()
    {
        //act - the arguments an aggregate binds are the member it folds; Enumerable's First takes
        //a predicate where this shape puts a selector, and a condition inside the brackets is
        //another grammar this language does not write yet
        var act = () => ApplyAggregated("groupBy[x.Name].select[C=x.First(x.Int)]");

        //assert
        act.Should().Throw<AggregateMethodToExpressionConverter.AggregateArgumentsException>();
    }

    [Fact]
    public void Apply_WhenAggregateTakesAValueArgument_ThenRefusedWithTheReason()
    {
        //act - the argument of an aggregate is the member its selector reads; a written value
        //has no slot in the binding and is refused rather than silently dropped
        var act = () => ApplyAggregated("groupBy[x.Name].select[C=x.First(5)]");

        //assert
        act.Should().Throw<AggregateMethodToExpressionConverter.AggregateArgumentsException>();
    }

    [Fact]
    public void Apply_WhenNavigationCountInSelect_ThenAnswersTheCountPerRow()
    {
        //arrange - the dummies of the shared fixture carry their children
        var rows = new[]
        {
            new DummyEntity("a"),
            new DummyEntity("b"),
            new DummyEntity("c"),
        };
        rows[0].Children.AddRange([new NestedDummyEntity(), new NestedDummyEntity()]);
        rows[1].Children.Add(new NestedDummyEntity());

        //act - the roadmap's nested count: a call over a navigation collection in a value position
        var actual = Apply("select[N=x.Children.Count()]", rows.AsQueryable());

        //assert
        ((object[])actual).Select(e => e.GetType().GetProperty("N")!.GetValue(e)).Should().Equal(2, 1, 0);
    }

    [Fact]
    public void Apply_WhenNavigationBareMax_ThenAnswersTheGreatestValue()
    {
        //arrange - a collection whose own values are numbers, so `Max()` has a greatest one;
        //a shape of its own: a collection of values on the shared dummy would widen what the
        //provider packs can bind there
        var rows = new[]
        {
            new PriceList([3, 11, 7]),
            new PriceList([1]),
        };

        //act - the bare Min/Max of a list of numbers, in a value position
        var actual = ApplyOver("select[H=x.Prices.Max(),L=x.Prices.Min()]", rows.AsQueryable());

        //assert
        var highs = ((object[])actual).Select(e => e.GetType().GetProperty("H")!.GetValue(e)).ToList();
        var lows = ((object[])actual).Select(e => e.GetType().GetProperty("L")!.GetValue(e)).ToList();
        highs.Should().Equal(11, 1);
        lows.Should().Equal(3, 1);
    }

    [Fact]
    public void Apply_WhenBareMaxOverValuesThatDoNotCompare_ThenRefusedWithTheReason()
    {
        //act - the children of a dummy are rows, and a row does not compare to another row
        var act = () => ApplyAggregated("select[H=x.Children.Max()]");

        //assert
        act.Should().Throw<AggregateMethodToExpressionConverter.AggregateArgumentsException>();
    }

    [Fact]
    public void Apply_WhenGroupByThenBareMax_ThenRefusedWithTheReason()
    {
        //act - the rows of a group are the items and a whole row has no greatest value
        var act = () => ApplyAggregated("groupBy[x.Name].orderBy[x.Max()]");

        //assert
        act.Should().Throw<AggregateMethodToExpressionConverter.AggregateArgumentsException>();
    }

    private class PriceList(int[] prices)
    {
        // ReSharper disable once UnusedMember.Local -> used in tests
        public List<int> Prices { get; set; } = [.. prices];
    }

    private object ApplyOver(string queryString, IQueryable<PriceList> queryable)
    {
        var hlinqQuery = new Request.HLinqQuery<PriceList>
        {
            SourceQueryString = queryString
        };
        _parser.Parse(hlinqQuery, _tokenizer.Tokenize(queryString));
        return _sut.Apply(queryable, hlinqQuery);
    }

    [Fact]
    public void Apply_WhenNavigationAggregateOverNotACollection_ThenRefusedWithTheReason()
    {
        //act - a string is not a collection to count
        var act = () => ApplyAggregated("select[N=x.Name.Count()]");

        //assert
        act.Should().Throw<AggregateMethodToExpressionConverter.AggregateArgumentsException>();
    }

    //the rows of the groups carry distinct ages, so every aggregate of a group has its own answer
    private static DummyEntity[] Aggregated() =>
    [
        new("New") { Int = 10 },
        new("New") { Int = 30 },
        new("Paid") { Int = 40 },
        new("Sent") { Int = 50 },
    ];

    private object ApplyAggregated(string queryString) =>
        Apply(queryString, Aggregated().AsQueryable());

    private static Dictionary<string, object> AggregatesOf(object result)
    {
        result.Should().BeAssignableTo<object[]>();
        return ((object[])result).ToDictionary(
            e => e.GetType().GetProperty(GroupingState.KeyName)!.GetValue(e)!.ToString()!,
            e => e);
    }

    private static object ValueOf(object entry, string name) =>
        entry.GetType().GetProperty(name)!.GetValue(entry)!;
}
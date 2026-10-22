using FluentAssertions;
using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Pipeline;
using HamsterWheel.HLinq.Pipeline.Applier;
using HamsterWheel.HLinq.Pipeline.Parser;
using HamsterWheel.HLinq.Pipeline.Tokenizer;
using HamsterWheel.HLinq.Reflection;
using HamsterWheel.HLinq.Request;
using HamsterWheel.HLinq.UnitTests.TestUtils;
using HamsterWheel.HLinq.UnitTests.TestUtils.Dummies;
using Microsoft.Extensions.DependencyInjection;

namespace HamsterWheel.HLinq.UnitTests.Pipeline.Applier;

partial class HLinqQueryApplierUnitTests
{
    private static DummyEntity[] Grouped() =>
    [
        new("New") { Text = "a", Int = 1 },
        new("New") { Text = "b", Int = 2 },
        new("Paid") { Text = "c", Int = 3 },
        new("Sent") { Text = "d", Int = 4 },
    ];

    private object ApplyGrouped(string queryString) =>
        Apply(queryString, Grouped().AsQueryable());

    //a grouping answers the list of {Key, Rows} entries, whatever the key is; keyed by the text of
    //the key so a test reads one group at a time, in the order the answer came in
    private static Dictionary<string, List<object>> AsDictionary(object result)
    {
        result.Should().BeAssignableTo<object[]>();
        return ((object[])result).ToDictionary(
            e => e.GetType().GetProperty(GroupingState.KeyName)!.GetValue(e)!.ToString()!,
            e => ((System.Collections.IEnumerable)e.GetType().GetProperty(GroupingState.RowsName)!.GetValue(e)!)
                .Cast<object>().ToList());
    }

    private static IEnumerable<object> RowsOf(Dictionary<string, List<object>> dictionary, string key) =>
        dictionary[key];

    private static void AssertKeyAndText(object row, string? key, string? text)
    {
        var type = row.GetType();
        type.GetProperty("Key")!.GetValue(row).Should().Be(key);
        type.GetProperty("Text")!.GetValue(row).Should().Be(text);
    }

    [Fact]
    public void Apply_WhenGroupBy_ThenAnswersAListOfKeyAndRowsEntries()
    {
        //act
        var actual = AsDictionary(ApplyGrouped("groupBy[x.Name]"));

        //assert
        actual.Keys.Should().BeEquivalentTo("New", "Paid", "Sent");
        RowsOf(actual, "New").Should().HaveCount(2);
        RowsOf(actual, "Paid").Should().HaveCount(1);
    }

    [Fact]
    public void Apply_WhenGroupByWithSelect_ThenRowsTakeTheSelectedShape()
    {
        //act
        var actual = AsDictionary(ApplyGrouped("groupBy[x.Name].select[x.Text]"));

        //assert
        RowsOf(actual, "New").Should().Equal("a", "b");
        RowsOf(actual, "Paid").Should().Equal("c");
    }

    [Fact]
    public void Apply_WhenGroupByWithSelectOfKeyAndRow_ThenKeyRepeatsBesideEachRow()
    {
        //act
        var actual = AsDictionary(ApplyGrouped("groupBy[x.Name].select[x.Key, x.Text]"));

        //assert
        var row = RowsOf(actual, "Paid").Single();
        AssertKeyAndText(row, "Paid", "c");
    }

    //a grouping written after a grouping groups the rows of every group: the answer holds inner
    //{Key, Rows} entries under the outer groups, read here as one dictionary per outer key
    private static Dictionary<string, Dictionary<string, List<object>>> AsNestedDictionary(object result)
    {
        result.Should().BeAssignableTo<object[]>();
        return ((object[])result).ToDictionary(
            e => e.GetType().GetProperty(GroupingState.KeyName)!.GetValue(e)!.ToString()!,
            e => ((System.Collections.IEnumerable)e.GetType().GetProperty(GroupingState.RowsName)!.GetValue(e)!)
                .Cast<object>().ToDictionary(
                    inner => inner.GetType().GetProperty(GroupingState.KeyName)!.GetValue(inner)!.ToString()!,
                    inner => ((System.Collections.IEnumerable)inner.GetType()
                        .GetProperty(GroupingState.RowsName)!.GetValue(inner)!).Cast<object>().ToList()));
    }

    [Fact]
    public void Apply_WhenNestedGroupBy_ThenAnswersGroupsUnderGroups()
    {
        //act
        var actual = AsNestedDictionary(ApplyGrouped("groupBy[x.Name].groupBy[x.Int]"));

        //assert
        actual.Keys.Should().BeEquivalentTo("New", "Paid", "Sent");
        actual["New"].Keys.Should().BeEquivalentTo("1", "2");
        actual["New"]["1"].Should().HaveCount(1);
        actual["Paid"].Keys.Should().BeEquivalentTo("3");
    }

    [Fact]
    public void Apply_WhenNestedGroupByWithSelect_ThenInnerRowsTakeTheSelectedShape()
    {
        //act
        var actual = AsNestedDictionary(ApplyGrouped("groupBy[x.Name].groupBy[x.Int].select[x.Text]"));

        //assert
        actual["New"]["1"].Should().Equal("a");
        actual["New"]["2"].Should().Equal("b");
        actual["Paid"]["3"].Should().Equal("c");
    }

    [Fact]
    public void Apply_WhenNestedGroupByThenTake_ThenKeepsOuterGroupsNotInner()
    {
        //act
        var actual = AsNestedDictionary(ApplyGrouped("groupBy[x.Name].groupBy[x.Int].take[2]"));

        //assert - the clauses after a grouping act on the groups of the chain, the outer ones,
        //the same way they did before the inner grouping was written
        actual.Should().HaveCount(2);
    }

    [Fact]
    public void Apply_WhenNestedGroupByThenCount_ThenCountsOuterGroups()
    {
        //act
        var actual = ApplyGrouped("groupBy[x.Name].groupBy[x.Int].count[]");

        //assert
        actual.Should().Be(3);
    }

    [Fact]
    public void Apply_WhenGroupByThenTake_ThenKeepsGroupsNotRows()
    {
        //act
        var actual = AsDictionary(ApplyGrouped("groupBy[x.Name].take[2]"));

        //assert
        actual.Should().HaveCount(2);
    }

    [Fact]
    public void Apply_WhenGroupByThenSkip_ThenSkipsGroupsNotRows()
    {
        //act
        var actual = AsDictionary(ApplyGrouped("groupBy[x.Name].skip[1]"));

        //assert
        actual.Should().HaveCount(2);
    }

    [Fact]
    public void Apply_WhenGroupByThenCount_ThenCountsGroups()
    {
        //act
        var actual = ApplyGrouped("groupBy[x.Name].count[]");

        //assert
        actual.Should().Be(3);
    }

    [Fact]
    public void Apply_WhenGroupBySelectThenCount_ThenCountsGroups()
    {
        //act
        var actual = ApplyGrouped("groupBy[x.Name].select[x.Text].count[]");

        //assert
        actual.Should().Be(3);
    }

    [Fact]
    public void Apply_WhenGroupByThenOrderByKey_ThenOrdersGroups()
    {
        //act
        var actual = AsDictionary(ApplyGrouped("groupBy[x.Name].orderBy[x.Key]"));

        //assert
        actual.Keys.Should().ContainInOrder("New", "Paid", "Sent");
    }

    [Fact]
    public void Apply_WhenGroupByThenOrderByDescendingKey_ThenOrdersGroupsBackwards()
    {
        //act
        var actual = AsDictionary(ApplyGrouped("groupBy[x.Name].orderByDescending[x.Key]"));

        //assert
        actual.Keys.Should().ContainInOrder("Sent", "Paid", "New");
    }

    [Fact]
    public void Apply_WhenGroupByThenWhereOnKey_ThenKeepsMatchingGroups()
    {
        //act
        var actual = AsDictionary(ApplyGrouped("groupBy[x.Name].where[x.Key==Paid]"));

        //assert
        actual.Keys.Should().BeEquivalentTo("Paid");
    }

    [Fact]
    public void Apply_WhenWhereBeforeGroupBy_ThenFiltersRowsBeforeGrouping()
    {
        //act
        var actual = AsDictionary(ApplyGrouped("where[x.Int>1].groupBy[x.Name].select[x.Text]"));

        //assert
        actual.Keys.Should().BeEquivalentTo("New", "Paid", "Sent");
        RowsOf(actual, "New").Should().Equal("b");
    }

    [Fact]
    public void Apply_WhenGroupByCompositeKey_ThenAnswersAListOfKeyAndRowsEntries()
    {
        //act
        var actual = ApplyGrouped("groupBy[x.Name, x.Int]");

        //assert - the same list shape as a single-key grouping, with a key of several parts
        actual.Should().BeAssignableTo<object[]>();
        var entries = ((object[])actual).ToList();
        entries.Should().HaveCount(4);

        var key = entries[0].GetType().GetProperty("Key")!.GetValue(entries[0])!;
        key.GetType().GetProperty("Name")!.GetValue(key).Should().Be("New");
        key.GetType().GetProperty("Int")!.GetValue(key).Should().Be(1);
    }

    [Fact]
    public void Apply_WhenGroupByThenOrderByRowMember_ThenRefused()
    {
        //act - a row of the group is not a name of the group; only the key and the rows are
        var act = () => ApplyGrouped("groupBy[x.Name].orderBy[x.Text]");

        //assert
        act.Should().Throw<InvalidPropertyPathException>();
    }

    [Fact]
    public void Apply_WhenGroupByThenWhereOnRowMember_ThenRefused()
    {
        //act
        var act = () => ApplyGrouped("groupBy[x.Name].where[x.Text==a]");

        //assert
        act.Should().Throw<InvalidPropertyPathException>();
    }

    [Fact]
    public void Apply_WhenGroupByThenOrderByTheRowsOfTheGroup_ThenRefusedWithTheReason()
    {
        //act - the provider cannot order groups by their rows, so the name is refused with the reason
        var act = () => ApplyGrouped("groupBy[x.Name].orderBy[x.Rows.Text]");

        //assert
        act.Should().Throw<RowsCannotBeOrderedOrFilteredException>();
    }

    [Fact]
    public void Apply_WhenGroupByThenWhereOnTheRowsOfTheGroup_ThenRefusedWithTheReason()
    {
        //act
        var act = () => ApplyGrouped("groupBy[x.Name].where[x.Rows.Text==a]");

        //assert
        act.Should().Throw<RowsCannotBeOrderedOrFilteredException>();
    }

    [Fact]
    public void Parse_WhenSelectNotDirectlyAfterGroupBy_ThenRefused()
    {
        //act - the parse refuses the order the query wrote, before anything is applied
        var act = () => ApplyGrouped("groupBy[x.Name].take[2].select[x.Text]");

        //assert
        act.Should().Throw<InvalidTokenCollectionException>();
    }

    [Fact]
    public void Parse_WhenSelectAfterSelectAfterGroupBy_ThenRefused()
    {
        //act
        var act = () => ApplyGrouped("groupBy[x.Name].select[x.Text].select[x.Text]");

        //assert
        act.Should().Throw<InvalidTokenCollectionException>();
    }

    [Fact]
    public void Apply_WhenGroupByThenSelectThenTake_ThenTakesGroupsOfTheSelectedRows()
    {
        //act
        var actual = AsDictionary(ApplyGrouped("groupBy[x.Name].select[x.Text].take[2]"));

        //assert
        actual.Should().HaveCount(2);
        RowsOf(actual, "New").Should().Equal("a", "b");
    }
}


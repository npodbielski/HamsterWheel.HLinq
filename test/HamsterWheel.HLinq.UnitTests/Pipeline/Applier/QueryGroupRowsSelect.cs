using FluentAssertions;
using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Pipeline.Applier;

namespace HamsterWheel.HLinq.UnitTests.Pipeline.Applier;

partial class HLinqQueryApplierUnitTests
{
    [Fact]
    public void Apply_WhenGroupRowsSelected_ThenItAnswersWhatTheMembersBesideAggregatesAnswer()
    {
        //act
        var actual = Json(ApplyGrouped("groupBy[x.Name].select[Rows=x.Rows.select(a.Text,a.Int)]"));

        //assert
        actual.Should().Be(Json(ApplyGrouped("groupBy[x.Name].select[x.Text,x.Int]")));
    }

    [Fact]
    public void Apply_WhenGroupRowsSelectNamesTheElement_ThenItStandsOnTheRow()
    {
        //act
        var actual = Json(ApplyGrouped("groupBy[x.Name].select[Rows=x.Rows.select(a.Text)]"));

        //assert
        actual.Should().Be(Json(ApplyGrouped("groupBy[x.Name].select[Rows=x.Rows.select(Text)]")));
    }

    [Fact]
    public void Apply_WhenGroupRowsSelectIsLowerCased_ThenItIsTheRowsSelect()
    {
        //act
        var actual = Json(ApplyGrouped("groupBy[x.name].select[rows=x.rows.select(a.Text)]"));

        //assert
        actual.Should().Be(Json(ApplyGrouped("groupBy[x.Name].select[Rows=x.Rows.select(a.Text)]")));
    }

    [Fact]
    public void Apply_WhenGroupRowsSelectWithKey_ThenTheRowAnswersTheKeyBesideItsMembers()
    {
        //act
        var rows = AsDictionary(ApplyGrouped("groupBy[x.Name].select[Rows=x.Rows.select(a.Text,x.Key)]"))["New"];

        //assert
        AssertKeyAndText(rows[0], "New", "a");
        AssertKeyAndText(rows[1], "New", "b");
    }

    [Fact]
    public void Apply_WhenGroupRowsSelectWithAggregate_ThenTheAggregateRidesBesideTheSelectedRows()
    {
        //act
        var actual = AggregatesOf(ApplyAggregated(
            "groupBy[x.Name].select[C=x.Count(),Rows=x.Rows.select(a.Int)]"));

        //assert
        ValueOf(actual["New"], "C").Should().Be(2);
        ((System.Collections.IEnumerable)ValueOf(actual["New"], GroupingState.RowsName))
            .Cast<object>().Should().HaveCount(2);
    }

    [Fact]
    public void Apply_WhenGroupRowsRenamedFromNavigation_ThenRefusedWithTheReason()
    {
        //act
        var act = () => ApplyGrouped("groupBy[x.Name].select[Rows=x.Children.select(a.Name)]");

        //assert
        act.Should().Throw<GroupRowsSelectSourceException>();
    }

    [Theory]
    [InlineData("groupBy[x.Name].select[Rows=x.Count()]")]
    [InlineData("groupBy[x.Name].select[C=x.Count(),Rows=x.Sum(x.Int)]")]
    [InlineData("groupBy[x.Name].select[rows=x.count()]")]
    public void Apply_WhenGroupRowsNamedByAnAggregate_ThenRefusedWithTheReason(string query)
    {
        //act
        var act = () => ApplyGrouped(query);

        //assert
        act.Should().Throw<GroupAggregateNameCollisionException>();
    }

    [Fact]
    public void Apply_WhenGroupRowsRenamedFromMember_ThenRefusedWithTheReason()
    {
        //act
        var act = () => ApplyGrouped("groupBy[x.Name].select[Rows=x.Text]");

        //assert
        act.Should().Throw<GroupRowsNameReservedException>();
    }

    [Fact]
    public void Apply_WhenGroupRowsSelectBesideRowMember_ThenRefusedAsSelectedTwice()
    {
        //act
        var act = () => ApplyGrouped("groupBy[x.Name].select[x.Text,Rows=x.Rows.select(a.Text)]");

        //assert
        act.Should().Throw<GroupRowsSelectedTwiceException>();
    }

    [Fact]
    public void Apply_WhenGroupRowsSelectTwice_ThenRefusedAsSelectedTwice()
    {
        //act
        var act = () => ApplyGrouped(
            "groupBy[x.Name].select[Rows=x.Rows.select(a.Text),Rows=x.Rows.select(a.Int)]");

        //assert
        act.Should().Throw<GroupRowsSelectedTwiceException>();
    }

    [Fact]
    public void Apply_WhenNestedGroupRowsSelect_ThenItSelectsTheRowsOfTheInnerGroup()
    {
        //arrange
        var query = "groupBy[x.Name].groupBy[x.Int].select[Rows=x.Rows.select(a.Text)]";

        //act
        var groups = AsDictionary(ApplyGrouped(query))["New"];

        //assert
        groups.Should().HaveCount(2);
        foreach (var inner in groups)
        {
            var innerRows = (System.Collections.IEnumerable)
                inner.GetType().GetProperty(GroupingState.RowsName)!.GetValue(inner)!;
            innerRows.Cast<object>().Select(r => r.GetType().GetProperty("Text")!.GetValue(r))
                .Should().ContainSingle();
            inner.GetType().GetProperty("Id").Should().BeNull();
        }
    }
}

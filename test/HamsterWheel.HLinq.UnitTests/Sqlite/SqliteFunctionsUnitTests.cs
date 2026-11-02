using System.Linq.Expressions;
using FluentAssertions;
using HamsterWheel.HLinq.Sqlite;
using HamsterWheel.HLinq.Pipeline.Applier.Builders;
using HamsterWheel.HLinq.UnitTests.TestUtils;
using HamsterWheel.HLinq.UnitTests.TestUtils.Dummies;
using Microsoft.EntityFrameworkCore;

namespace HamsterWheel.HLinq.UnitTests.Sqlite;

/// <summary>
/// The call an author writes by hand in Linq is the oracle of the tree the binder builds from the
/// query text. The pack hands over the generic EF class alone, so `Like` is the one name of it a
/// query reaches - `Random` is a call with no argument at all, which no query is written as.
/// </summary>
public class SqliteFunctionsUnitTests
{
    private static readonly TestServicesCollection Services = new();
    private static readonly IStaticMethodSource Source = new SqliteEntityFrameworkStaticMethodProvider();

    [Fact]
    public void GivenFilterOnLike_WhenBuilt_ThenBuildsTheTreeOfTheEfCall()
    {
        Expression<Func<DummyEntity, bool>> twin = x => EF.Functions.Like(x.Name, "%Bill%");

        EfTwin.AssertSameTree(twin, ProviderFilters.Whole(Services, Source, "where[Like(x.Name, %Bill%)]"));
    }

    [Fact]
    public void GivenFilterOnLikeWithEscape_WhenBuilt_ThenBuildsTheTreeOfTheEfCall()
    {
        Expression<Func<DummyEntity, bool>> twin = x => EF.Functions.Like(x.Name, "%Bill%", "\\");

        EfTwin.AssertSameTree(twin, ProviderFilters.Whole(Services, Source, "where[Like(x.Name, %Bill%, \\)]"));
    }

    [Fact]
    public void GivenFilterOnRandomWrittenWithAnArgument_WhenBuilt_ThenRefusedNamingTheArgument()
    {
        var action = () => ProviderFilters.Whole(Services, Source, "where[Random(x.Name)]");

        action.Should().Throw<HamsterWheel.HLinq.Tree.Filtering.Method.InvalidStaticMethodException>()
            .Which.Message.Should().Contain("Random()");
    }
}

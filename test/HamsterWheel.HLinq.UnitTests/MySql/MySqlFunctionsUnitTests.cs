using System.Linq.Expressions;
using HamsterWheel.HLinq.MySql;
using HamsterWheel.HLinq.Pipeline.Applier.Builders;
using HamsterWheel.HLinq.UnitTests.TestUtils;
using HamsterWheel.HLinq.UnitTests.TestUtils.Dummies;
using Microsoft.EntityFrameworkCore;

namespace HamsterWheel.HLinq.UnitTests.MySql;

/// <summary>
/// The call an author writes by hand in Linq is the oracle of the tree the binder builds from the
/// query text: the same method, the same arguments in the same order, a column where the twin reads
/// one and a value where the twin writes one. The names are the ones the page of the engine writes.
/// </summary>
public class MySqlFunctionsUnitTests
{
    private static readonly TestServicesCollection Services = new();
    private static readonly IStaticMethodSource Source = new MySqlEntityFrameworkStaticMethodProvider();

    [Fact]
    public void GivenFilterOnLike_WhenBuilt_ThenBuildsTheTreeOfTheEfCall()
    {
        Expression<Func<DummyEntity, bool>> twin = x => EF.Functions.Like(x.Name, "%Bill%");

        EfTwin.AssertSameTree(twin, ProviderFilters.Whole(Services, Source, "where[Like(x.Name, %Bill%)]"));
    }

    [Fact]
    public void GivenFilterOnIsMatch_WhenBuilt_ThenBuildsTheTreeOfTheEfCall()
    {
        Expression<Func<DummyEntity, bool>> twin =
            x => MySqlDbFunctionsExtensions.IsMatch(EF.Functions, x.Name, "abc",
                MySqlMatchSearchMode.NaturalLanguage);

        EfTwin.AssertSameTree(twin,
            ProviderFilters.Whole(Services, Source, "where[IsMatch(x.Name, abc, NaturalLanguage)]"));
    }

    [Fact]
    public void GivenFilterOnJsonContains_WhenBuilt_ThenBuildsTheTreeOfTheEfCall()
    {
        Expression<Func<DummyEntity, bool>> twin =
            x => MySqlJsonDbFunctionsExtensions.JsonContains(EF.Functions, x.Name, x.Text);

        EfTwin.AssertSameTree(twin,
            ProviderFilters.Whole(Services, Source, "where[JsonContains(x.Name, x.Text)]"));
    }

    [Fact]
    public void GivenFilterOnJsonContainsPath_WhenBuilt_ThenBuildsTheTreeOfTheEfCall()
    {
        Expression<Func<DummyEntity, bool>> twin =
            x => MySqlJsonDbFunctionsExtensions.JsonContainsPath(EF.Functions, x.Name, "$.a.b");

        EfTwin.AssertSameTree(twin,
            ProviderFilters.Whole(Services, Source, "where[JsonContainsPath(x.Name, $.a.b)]"));
    }

    [Fact]
    public void GivenFilterOnJsonOverlaps_WhenBuilt_ThenBuildsTheTreeOfTheEfCall()
    {
        Expression<Func<DummyEntity, bool>> twin =
            x => MySqlJsonDbFunctionsExtensions.JsonOverlaps(EF.Functions, x.Name, x.Text);

        EfTwin.AssertSameTree(twin,
            ProviderFilters.Whole(Services, Source, "where[JsonOverlaps(x.Name, x.Text)]"));
    }

    [Fact]
    public void GivenFilterOnJsonSearchAny_WhenBuilt_ThenBuildsTheTreeOfTheEfCall()
    {
        Expression<Func<DummyEntity, bool>> twin =
            x => MySqlJsonDbFunctionsExtensions.JsonSearchAny(EF.Functions, x.Name, "abc");

        EfTwin.AssertSameTree(twin,
            ProviderFilters.Whole(Services, Source, "where[JsonSearchAny(x.Name, abc)]"));
    }

    [Fact]
    public void GivenFilterOnDateDiffDayComparedWithNumber_WhenBuilt_ThenBuildsTheTreeOfTheEfCall()
    {
        Expression<Func<DummyEntity, bool>> twin =
            x => MySqlDbFunctionsExtensions.DateDiffDay(EF.Functions, x.DateTime, x.DateTime) > 7;

        EfTwin.AssertSameTree(twin,
            ProviderFilters.Whole(Services, Source, "where[DateDiffDay(x.DateTime, x.DateTime)>7]"));
    }

    [Fact]
    public void GivenFilterOnMatchComparedWithNumber_WhenBuilt_ThenBuildsTheTreeOfTheEfCall()
    {
        Expression<Func<DummyEntity, bool>> twin =
            x => MySqlDbFunctionsExtensions.Match(EF.Functions, x.Name, "abc",
                MySqlMatchSearchMode.NaturalLanguage) > 1;

        EfTwin.AssertSameTree(twin,
            ProviderFilters.Whole(Services, Source, "where[Match(x.Name, abc, NaturalLanguage)>1]"));
    }

    [Fact]
    public void GivenFilterOnConvertTimeZoneComparedWithColumn_WhenBuilt_ThenBuildsTheTreeOfTheEfCall()
    {
        Expression<Func<DummyEntity, bool>> twin =
            x => MySqlDbFunctionsExtensions.ConvertTimeZone(EF.Functions, x.DateTime, "UTC")
                == x.NullableDateTime;

        EfTwin.AssertSameTree(twin,
            ProviderFilters.Whole(Services, Source, "where[ConvertTimeZone(x.DateTime, UTC)==x.NullableDateTime]"));
    }

    [Fact]
    public void GivenFilterOnDegreesComparedWithNumber_WhenBuilt_ThenBuildsTheTreeOfTheEfCall()
    {
        Expression<Func<DummyEntity, bool>> twin =
            x => MySqlDbFunctionsExtensions.Degrees(EF.Functions, x.Double) > 3;

        EfTwin.AssertSameTree(twin,
            ProviderFilters.Whole(Services, Source, "where[Degrees(x.Double)>3]"));
    }

    [Fact]
    public void GivenFilterOnRadiansComparedWithNumber_WhenBuilt_ThenBuildsTheTreeOfTheEfCall()
    {
        Expression<Func<DummyEntity, bool>> twin =
            x => MySqlDbFunctionsExtensions.Radians(EF.Functions, x.Double) < 1;

        EfTwin.AssertSameTree(twin,
            ProviderFilters.Whole(Services, Source, "where[Radians(x.Double)<1]"));
    }

    [Fact]
    public void GivenFilterOnUnhexComparedWithText_WhenBuilt_ThenBuildsTheTreeOfTheEfCall()
    {
        Expression<Func<DummyEntity, bool>> twin =
            x => MySqlDbFunctionsExtensions.Unhex(EF.Functions, x.Name) == "abc";

        EfTwin.AssertSameTree(twin,
            ProviderFilters.Whole(Services, Source, "where[Unhex(x.Name)==abc]"));
    }

    [Fact]
    public void GivenFilterOnJsonTypeComparedWithText_WhenBuilt_ThenBuildsTheTreeOfTheEfCall()
    {
        Expression<Func<DummyEntity, bool>> twin =
            x => MySqlJsonDbFunctionsExtensions.JsonType(EF.Functions, x.Name) == "abc";

        EfTwin.AssertSameTree(twin,
            ProviderFilters.Whole(Services, Source, "where[JsonType(x.Name)==abc]"));
    }

    [Fact]
    public void GivenFilterOnJsonQuoteComparedWithText_WhenBuilt_ThenBuildsTheTreeOfTheEfCall()
    {
        Expression<Func<DummyEntity, bool>> twin =
            x => MySqlJsonDbFunctionsExtensions.JsonQuote(EF.Functions, x.Name) == "abc";

        EfTwin.AssertSameTree(twin,
            ProviderFilters.Whole(Services, Source, "where[JsonQuote(x.Name)==abc]"));
    }

    [Fact]
    public void GivenFilterOnJsonUnquoteComparedWithText_WhenBuilt_ThenBuildsTheTreeOfTheEfCall()
    {
        Expression<Func<DummyEntity, bool>> twin =
            x => MySqlJsonDbFunctionsExtensions.JsonUnquote(EF.Functions, x.Name) == "abc";

        EfTwin.AssertSameTree(twin,
            ProviderFilters.Whole(Services, Source, "where[JsonUnquote(x.Name)==abc]"));
    }
}

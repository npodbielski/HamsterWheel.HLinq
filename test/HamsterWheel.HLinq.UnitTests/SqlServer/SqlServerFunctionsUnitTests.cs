using System.Linq.Expressions;
using FluentAssertions;
using HamsterWheel.HLinq.Pipeline.Applier.Builders;
using HamsterWheel.HLinq.Pipeline.Parser;
using HamsterWheel.HLinq.SqlServer;
using HamsterWheel.HLinq.Tree.Filtering;
using HamsterWheel.HLinq.UnitTests.TestUtils;
using HamsterWheel.HLinq.UnitTests.TestUtils.Dummies;
using Microsoft.EntityFrameworkCore;

namespace HamsterWheel.HLinq.UnitTests.SqlServer;

/// <summary>
/// A provider function is named by its word with the column as its argument.
/// </summary>
public class SqlServerFunctionsUnitTests
{
    private static readonly TestServicesCollection Services = new();

    private readonly ExpressionBuilder _sut = new(Services.PropertiesCache, Services.MethodsCache,
        Services.ConverterFactory, new ParametersConverter(Services.WrittenValues, Services.DefaultConverter),
        [new SqlServerEntityFrameworkStaticMethodProvider()], Services.WrittenValues, null!);

    private readonly IHLinqParser _parser = new HLinqParser(Services.Parsers);

    /// <summary>
    /// The hand-written Linq call is the oracle of the tree the binder builds.
    /// </summary>
    [Fact]
    public void GivenFilterOnIsDate_WhenBuilt_ThenBuildsTheTreeOfTheEfCall()
    {
        Expression<Func<DummyEntity, bool>> twin =
            x => SqlServerDbFunctionsExtensions.IsDate(EF.Functions, x.Name);

        EfTwin.AssertSameTree(twin, WholeFilter("where[IsDate(x.Name)]"));
    }

    [Fact]
    public void GivenFilterOnFreeText_WhenBuilt_ThenBuildsTheTreeOfTheEfCall()
    {
        Expression<Func<DummyEntity, bool>> twin =
            x => SqlServerDbFunctionsExtensions.FreeText(EF.Functions, x.Name, "abc");

        EfTwin.AssertSameTree(twin, WholeFilter("where[FreeText(x.Name, abc)]"));
    }

    [Fact]
    public void GivenFilterOnContains_WhenBuilt_ThenBuildsTheTreeOfTheEfCall()
    {
        Expression<Func<DummyEntity, bool>> twin =
            x => SqlServerDbFunctionsExtensions.Contains(EF.Functions, x.Name, "abc");

        EfTwin.AssertSameTree(twin, WholeFilter("where[Contains(x.Name, abc)]"));
    }

    [Fact]
    public void GivenFilterOnDateDiffDayComparedWithNumber_WhenBuilt_ThenBuildsTheTreeOfTheEfCall()
    {
        Expression<Func<DummyEntity, bool>> twin =
            x => SqlServerDbFunctionsExtensions.DateDiffDay(EF.Functions, x.DateTime, x.DateTime) > 7;

        EfTwin.AssertSameTree(twin, WholeFilter("where[DateDiffDay(x.DateTime, x.DateTime)>7]"));
    }

    [Fact]
    public void GivenFilterOnDataLengthComparedWithNumber_WhenBuilt_ThenBuildsTheTreeOfTheEfCall()
    {
        Expression<Func<DummyEntity, bool>> twin =
            x => SqlServerDbFunctionsExtensions.DataLength(EF.Functions, x.Name) > 100;

        EfTwin.AssertSameTree(twin, WholeFilter("where[DataLength(x.Name)>100]"));
    }

    [Fact]
    public void GivenFilterOnPatIndexComparedWithNumber_WhenBuilt_ThenBuildsTheTreeOfTheEfCall()
    {
        Expression<Func<DummyEntity, bool>> twin =
            x => SqlServerDbFunctionsExtensions.PatIndex(EF.Functions, "%Bill%", x.Name) > 0;

        EfTwin.AssertSameTree(twin, WholeFilter("where[PatIndex(%Bill%, x.Name)>0]"));
    }

    /// <summary>
    /// What a call answers is compared with a value of its own type.
    /// </summary>
    [Fact]
    public void GivenFilterOnTimeFromPartsComparedWithWrittenTimeSpan_WhenBuilt_ThenTheWrittenSpanReachesTheComparison()
    {
        //act
        var comparison = (BinaryExpression)WholeFilter(
            "where[TimeFromParts(x.Int, x.Int, x.Int, x.Int, x.Int) > 00:02:00]").Body;

        //assert
        comparison.Right.Should().BeAssignableTo<ConstantExpression>()
            .Which.Value.Should().Be(TimeSpan.FromMinutes(2));
    }

    [Fact]
    public void GivenFilterOnFunctionOfTheProvider_WhenBuilt_ThenCallsTheFunctionWithTheColumn()
    {
        //act
        var actual = Filter("where[IsDate(x.Name)]");

        //assert
        actual.Method.DeclaringType.Should().Be(typeof(SqlServerDbFunctionsExtensions));
        actual.Method.Name.Should().Be("IsDate");
        actual.Arguments.OfType<MemberExpression>().Select(a => a.Member.Name).Should().Contain("Name");
    }

    [Fact]
    public void GivenFilterOnWordBothTheProviderAndTheValueHave_WhenBuiltOnTheValue_ThenCallsTheValue()
    {
        //act
        var actual = Filter("where[x.Name.Contains(ill)]");

        //assert
        actual.Method.DeclaringType.Should().Be(typeof(string));
        actual.Method.Name.Should().Be("Contains");
    }

    /// <summary>
    /// A parameter of no type in particular takes a column too.
    /// </summary>
    [Fact]
    public void GivenFilterOnFunctionOfAnArgumentOfAnyType_WhenBuilt_ThenTheColumnReachesTheFunction()
    {
        //act
        var actual = Filter("where[FreeText(x.Name, abc)]");

        //assert
        actual.Method.DeclaringType.Should().Be(typeof(SqlServerDbFunctionsExtensions));
        actual.ToString().Should().Be("null.FreeText(Param_0.Name, \"abc\")");
    }

    /// <summary>
    /// A function of dates is written with the date columns of a row.
    /// </summary>
    [Fact]
    public void GivenFilterComparingDifferenceOfTwoDateColumns_WhenBuilt_ThenBothColumnsReachTheFunction()
    {
        //act
        var actual = WholeFilter("where[DateDiffDay(x.DateTime, x.DateTime)>7]");

        //assert
        actual.ToString().Should().Be("Param_0 => (null.DateDiffDay(Param_0.DateTime, Param_0.DateTime) > 7)");
    }

    private LambdaExpression WholeFilter(string query)
    {
        //arrange
        var tokens = Services.Tokenizer.Tokenize(query);
        var tree = _parser.TestParseEntryPoint<DummyEntity>(query, tokens);

        //act
        return _sut.GetFilter(typeof(DummyEntity), tree.GetAll<WhereRoot>().First(), query);
    }

    private MethodCallExpression Filter(string query) => (MethodCallExpression)WholeFilter(query).Body;
}

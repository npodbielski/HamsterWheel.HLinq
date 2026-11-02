using System.Linq.Expressions;
using FluentAssertions;
using HamsterWheel.HLinq.Diagnostics;
using HamsterWheel.HLinq.MySql;
using HamsterWheel.HLinq.PgSql;
using HamsterWheel.HLinq.Pipeline.Applier.Builders;
using HamsterWheel.HLinq.Pipeline.Parser;
using HamsterWheel.HLinq.Tree.Filtering;
using HamsterWheel.HLinq.UnitTests.TestUtils;
using HamsterWheel.HLinq.UnitTests.TestUtils.Dummies;

namespace HamsterWheel.HLinq.UnitTests;

/// <summary>
/// A written argument reaches a parameter of a function when the text parses into the parameter's
/// type, and the shape the text is written in decides which of the competing overloads wins - the
/// contests of the shipped packs, built the way a query reaches the binder.
/// </summary>
public class WrittenArgumentsUnitTests
{
    private static readonly TestServicesCollection Services = new();

    private static MethodCallExpression Call(IStaticMethodSource source, string query)
    {
        //arrange
        var builder = new ExpressionBuilder(Services.PropertiesCache, Services.MethodsCache,
            Services.ConverterFactory, new ParametersConverter(Services.WrittenValues, Services.DefaultConverter),
            [source], Services.WrittenValues, null!);
        var parser = new HLinqParser(Services.Parsers);
        var tokens = Services.Tokenizer.Tokenize(query);
        var tree = parser.TestParseEntryPoint<DummyEntity>(query, tokens);

        //act
        var filter = builder.GetFilter(typeof(DummyEntity), tree.GetAll<WhereRoot>().First(), query,
            warnings: new HLinqWarnings());

        //the query compares the call with a value, so the call is the left side of the filter
        return (MethodCallExpression)((BinaryExpression)filter.Body).Left;
    }

    [Fact]
    public void GivenDistanceOfAMomentColumnAndAWrittenDate_WhenBuilt_ThenTheDateTimeOverloadTakesTheDate()
    {
        //act
        var call = Call(new PgSqlEntityFrameworkStaticMethodProvider(),
            "where[Distance(x.DateTime, 2020-01-01) > 00:02:00]");

        //assert - a date is the date's own text and only a slack moment, so the DateTime pair wins
        //over the DateOnly one by the cheapest conversion, and the written date arrives typed
        call.Method.GetParameters()[1].ParameterType.Should().Be(typeof(DateTime));
        call.Arguments[2].Should().BeAssignableTo<ConstantExpression>()
            .Which.Value.Should().Be(new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void GivenDateDiffDayOfAMomentColumnAndAWrittenDate_WhenBuilt_ThenTheMomentOverloadTakesTheDate()
    {
        //act
        var call = Call(new MySqlEntityFrameworkStaticMethodProvider(),
            "where[DateDiffDay(x.DateTime, 2020-01-01) > 7]");

        //assert
        call.Method.GetParameters()[1].ParameterType.Should().Be(typeof(DateTime));
        call.Method.GetParameters()[2].ParameterType.Should().Be(typeof(DateTime));
        call.Arguments[2].Should().BeAssignableTo<ConstantExpression>()
            .Which.Value.Should().Be(new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void GivenDegreesOfAWrittenNumber_WhenBuilt_ThenTheNumberReachesTheDoubleOverload()
    {
        //act
        var call = Call(new MySqlEntityFrameworkStaticMethodProvider(), "where[Degrees(7) > 3]");

        //assert - double and float are the same shape, so the ordinal first one answers, warned
        call.Method.GetParameters()[1].ParameterType.Should().Be(typeof(double));
        call.Arguments[1].Should().BeAssignableTo<ConstantExpression>()
            .Which.Value.Should().Be(7d);
    }

    [Theory]
    [InlineData("where[Degrees(abc) > 3]")]
    [InlineData("where[Degrees(2020-01-01) > 3]")]
    public void GivenDegreesOfATextParsingIntoNoNumber_WhenBuilt_ThenTheCallIsRefused(string query)
    {
        //act
        var action = () => Call(new MySqlEntityFrameworkStaticMethodProvider(), query);

        //assert
        action.Should().Throw<Method.InvalidStaticMethodException>();
    }
}

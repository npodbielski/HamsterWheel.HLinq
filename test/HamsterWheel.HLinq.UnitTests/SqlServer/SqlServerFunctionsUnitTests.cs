using System.Linq.Expressions;
using FluentAssertions;
using HamsterWheel.HLinq.Diagnostics;
using HamsterWheel.HLinq.Pipeline.Applier;
using HamsterWheel.HLinq.Pipeline.Applier.Builders;
using HamsterWheel.HLinq.Pipeline.Parser;
using HamsterWheel.HLinq.SqlServer;
using HamsterWheel.HLinq.Tree.Filtering;
using HamsterWheel.HLinq.UnitTests.TestUtils;
using HamsterWheel.HLinq.UnitTests.TestUtils.Dummies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.SqlServer;
using Xunit;

namespace HamsterWheel.HLinq.UnitTests.SqlServer;

/// <summary>
/// A function of a provider is named by its own word and given the column as its argument, which is how
/// EF reads the same call written in C#. A method written on a value stays a method of that value: the
/// provider has a function of the same word, and the query that spells it on the property is still the
/// string's own - a pack adds words, it does not take them away.
/// </summary>
public class SqlServerFunctionsUnitTests
{
    private static readonly TestServicesCollection Services = new();

    private readonly ExpressionBuilder _sut = new(Services.PropertiesCache, Services.MethodsCache,
        Services.ConverterFactory, new ParametersConverter(), [new SqlServerEntityFrameworkStaticMethodProvider()]);

    private readonly IHLinqParser _parser = new HLinqParser(Services.Parsers);

    private LambdaExpression WholeFilter(string query)
    {
        //arrange
        var tokens = Services.Tokenizer.Tokenize(query);
        var tree = _parser.TestParseEntryPoint<DummyEntity>(query, tokens);

        //act
        return _sut.GetFilter(typeof(DummyEntity), tree.GetAll<WhereRoot>().First(), query,
            warnings: new HLinqWarnings());
    }

    private MethodCallExpression Filter(string query) => (MethodCallExpression)WholeFilter(query).Body;

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
    /// A parameter of no type in particular - <c>FreeText</c> reads its first argument as whatever the
    /// column holds - takes a column too: what it answers is the question of the types, not of the text a
    /// query writes.
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
    /// The arguments of a function are matched with the type of its parameters, so a function of dates is
    /// written with the date columns of a row - a date itself, which a query writes as text, no parameter
    /// of a function is read from.
    /// </summary>
    [Fact]
    public void GivenFilterComparingDifferenceOfTwoDateColumns_WhenBuilt_ThenBothColumnsReachTheFunction()
    {
        //act
        var actual = WholeFilter("where[DateDiffDay(x.DateTime, x.DateTime)>7]");

        //assert
        actual.ToString().Should().Be("Param_0 => (null.DateDiffDay(Param_0.DateTime, Param_0.DateTime) > 7)");
    }
}

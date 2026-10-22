using System.Linq.Expressions;
using FluentAssertions;
using HamsterWheel.HLinq.Diagnostics;
using HamsterWheel.HLinq.MariaDb;
using HamsterWheel.HLinq.MySql;
using HamsterWheel.HLinq.Pipeline.Applier;
using HamsterWheel.HLinq.Pipeline.Applier.Builders;
using HamsterWheel.HLinq.Pipeline.Parser;
using HamsterWheel.HLinq.Sqlite;
using HamsterWheel.HLinq.Tree.Filtering;
using HamsterWheel.HLinq.UnitTests.TestUtils;
using HamsterWheel.HLinq.UnitTests.TestUtils.Dummies;
using Microsoft.EntityFrameworkCore;

namespace HamsterWheel.HLinq.UnitTests;

public class ProviderFunctionsUnitTests
{
    private static readonly TestServicesCollection Services = new();

    internal static MethodCallExpression Filter(IStaticMethodSource source, string query)
    {
        //arrange
        var builder = new ExpressionBuilder(Services.PropertiesCache, Services.MethodsCache, Services.ConverterFactory,
            new ParametersConverter(), [source]);
        var parser = new HLinqParser(Services.Parsers);
        var tokens = Services.Tokenizer.Tokenize(query);
        var tree = parser.TestParseEntryPoint<DummyEntity>(query, tokens);

        //act
        var filter = builder.GetFilter(typeof(DummyEntity), tree.GetAll<WhereRoot>().First(), query,
            warnings: new HLinqWarnings());

        return (MethodCallExpression)filter.Body;
    }

    [Theory]
    [InlineData(typeof(MySqlEntityFrameworkStaticMethodProvider))]
    [InlineData(typeof(MariaDbEntityFrameworkStaticMethodProvider))]
    [InlineData(typeof(SqliteEntityFrameworkStaticMethodProvider))]
    public void GivenLikeFilter_WhenBuilt_ThenCallsEfLike(Type sourceType)
    {
        //arrange
        var source = (IStaticMethodSource)Activator.CreateInstance(sourceType)!;

        //act
        var actual = Filter(source, "where[Like(x.Name, pattern)]");

        //assert
        actual.Method.DeclaringType.Should().Be(typeof(DbFunctionsExtensions));
        actual.Method.Name.Should().Be("Like");
        actual.Arguments.OfType<MemberExpression>().Select(a => a.Member.Name).Should().Contain("Name");
    }
}
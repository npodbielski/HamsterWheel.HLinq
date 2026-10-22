using System.Linq.Expressions;
using FluentAssertions;
using HamsterWheel.HLinq.Diagnostics;
using HamsterWheel.HLinq.PgSql;
using HamsterWheel.HLinq.Pipeline.Applier;
using HamsterWheel.HLinq.Pipeline.Applier.Builders;
using HamsterWheel.HLinq.Pipeline.Parser;
using HamsterWheel.HLinq.Tree.Filtering;
using HamsterWheel.HLinq.UnitTests.TestUtils;
using HamsterWheel.HLinq.UnitTests.TestUtils.Dummies;
using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;

namespace HamsterWheel.HLinq.UnitTests.PgSql;

/// <summary>
/// A filter is a whole function call, so the functions of the provider that answer with a truth are the
/// ones a query can use: <c>ILike</c> and the trigram similarities, each written with the column as one
/// of its arguments and the other argument as a value.
/// </summary>
public class PgSqlFunctionsUnitTests
{
    private static readonly TestServicesCollection Services = new();

    private readonly ExpressionBuilder _sut = new(Services.PropertiesCache, Services.MethodsCache,
        Services.ConverterFactory, new ParametersConverter(), [new PgSqlEntityFrameworkStaticMethodProvider()]);

    private readonly IHLinqParser _parser = new HLinqParser(Services.Parsers);

    private MethodCallExpression Filter(string query)
    {
        //arrange
        var tokens = Services.Tokenizer.Tokenize(query);
        var tree = _parser.TestParseEntryPoint<DummyEntity>(query, tokens);

        //act
        var filter = _sut.GetFilter(typeof(DummyEntity), tree.GetAll<WhereRoot>().First(), query,
            warnings: new HLinqWarnings());

        return (MethodCallExpression)filter.Body;
    }

    [Theory]
    [InlineData("where[ILike(x.Name, x.Text)]", @"null.ILike(Param_0.Name, Param_0.Text)")]
    [InlineData("where[ILike(x.Nested.Name, x.Text)]", @"null.ILike(Param_0.Nested.Name, Param_0.Text)")]
    [InlineData("where[ILike(x.Name, x.Text, abc)]", "null.ILike(Param_0.Name, Param_0.Text, \"abc\")")]
    [InlineData("where[ILike(abc, x.Name, x.Text)]", "null.ILike(\"abc\", Param_0.Name, Param_0.Text)")]
    [InlineData("where[TrigramsAreSimilar(x.Name, x.Text)]", @"null.TrigramsAreSimilar(Param_0.Name, Param_0.Text)")]
    public void GivenFilterComparingTwoColumns_WhenBuilt_ThenBothColumnsReachTheFunction(
        string query, string expected)
    {
        //act
        var actual = Filter(query);

        //assert
        actual.ToString().Should().Be(expected);
    }

    /// <summary>
    /// A column goes to a parameter that takes the type of the column, which is how a function of two
    /// addresses of one row is reachable at all. The network functions are no words of the pack yet, so the
    /// query is written to a source of its own: what is measured here are the arguments a call binds, not
    /// the words a pack hands over.
    /// </summary>
    [Fact]
    public void GivenFilterOnFunctionOfColumnsOfTheTypeOfItsParameters_WhenBuilt_ThenBothColumnsReachTheFunction()
    {
        //arrange
        const string query = "where[SameFamily(x.Source, x.Target)]";
        var ofNetworkFunctions = new ExpressionBuilder(Services.PropertiesCache, Services.MethodsCache,
            Services.ConverterFactory, new ParametersConverter(), [new NetworkFunctions()]);
        var tokens = Services.Tokenizer.Tokenize(query);
        var tree = _parser.TestParseEntryPoint<Hosts>(query, tokens);

        //act
        var actual = (MethodCallExpression)ofNetworkFunctions.GetFilter(typeof(Hosts),
            tree.GetAll<WhereRoot>().First(), query, warnings: new HLinqWarnings()).Body;

        //assert
        actual.ToString().Should().Be("null.SameFamily(Param_0.Source, Param_0.Target)");
    }

    [Theory]
    [InlineData("where[TrigramsAreSimilar(x.Name, abc, def)]", "Query provided following argument(s): Name, abc, def")]
    [InlineData("where[TrigramsAreSimilar(x.Name)]", "Query provided following argument(s): Name")]
    [InlineData("where[TrigramsAreSimilar(x.Name, x.Int)]", "Query provided following argument(s): Name, Int")]
    public void GivenFilterOnFunctionOfArgumentsItDoesNotTake_WhenBuilt_ThenRefusedNamingThem(
        string query, string provided)
    {
        //act
        var action = () => Filter(query);

        //assert
        var exception = action.Should().Throw<Method.InvalidStaticMethodException>();
        exception.Which.Message.Should().Contain(provided).And.Contain("TrigramsAreSimilar(String, String)");
    }

    [Theory]
    [InlineData("ILike", typeof(NpgsqlDbFunctionsExtensions))]
    [InlineData("TrigramsAreSimilar", typeof(NpgsqlTrigramsDbFunctionsExtensions))]
    [InlineData("TrigramsAreWordSimilar", typeof(NpgsqlTrigramsDbFunctionsExtensions))]
    [InlineData("TrigramsAreStrictWordSimilar", typeof(NpgsqlTrigramsDbFunctionsExtensions))]
    [InlineData("TrigramsAreNotWordSimilar", typeof(NpgsqlTrigramsDbFunctionsExtensions))]
    [InlineData("TrigramsAreNotStrictWordSimilar", typeof(NpgsqlTrigramsDbFunctionsExtensions))]
    public void GivenFilterOnFunctionOfTheProvider_WhenBuilt_ThenCallsTheFunctionWithTheColumn(
        string word, Type declaring)
    {
        //act
        var actual = Filter($"where[{word}(x.Name, abc)]");

        //assert
        actual.Method.DeclaringType.Should().Be(declaring);
        actual.Method.Name.Should().Be(word);
        actual.Arguments.OfType<MemberExpression>().Select(a => a.Member.Name).Should().Contain("Name");
    }

    private class NetworkFunctions : IStaticMethodSource
    {
        public Type[] Types { get; } = [typeof(NpgsqlNetworkDbFunctionsExtensions)];
    }

    private class Hosts
    {
        public NpgsqlInet Source { get; set; }
        public NpgsqlInet Target { get; set; }
    }
}

using System.Linq.Expressions;
using HamsterWheel.HLinq.Diagnostics;
using HamsterWheel.HLinq.Pipeline.Applier.Builders;
using HamsterWheel.HLinq.Pipeline.Parser;
using HamsterWheel.HLinq.Tree.Filtering;
using HamsterWheel.HLinq.UnitTests.TestUtils.Dummies;

namespace HamsterWheel.HLinq.UnitTests.TestUtils;

/// <summary>
/// Builds the filter of a query through the same <see cref="ExpressionBuilder.GetFilter"/> an engine
/// test uses, so a record and a tree are measured the way a query reaches the binder.
/// </summary>
public static class ProviderFilters
{
    public static LambdaExpression Whole(TestServicesCollection services, IStaticMethodSource source, string query)
    {
        var builder = new ExpressionBuilder(services.PropertiesCache, services.MethodsCache,
            services.ConverterFactory, new ParametersConverter(services.WrittenValues, services.DefaultConverter),
            [source], services.WrittenValues, null!);
        var tokens = services.Tokenizer.Tokenize(query);
        var tree = new HLinqParser(services.Parsers).TestParseEntryPoint<DummyEntity>(query, tokens);

        return builder.GetFilter(typeof(DummyEntity), tree.GetAll<WhereRoot>().First(), query,
            warnings: new HLinqWarnings());
    }

    public static MethodCallExpression Call(TestServicesCollection services, IStaticMethodSource source,
        string query) =>
        (MethodCallExpression)Whole(services, source, query).Body;
}

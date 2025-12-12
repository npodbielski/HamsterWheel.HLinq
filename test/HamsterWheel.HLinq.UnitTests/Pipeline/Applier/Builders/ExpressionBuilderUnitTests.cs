using System.Globalization;
#if !NETSTANDARD
using HamsterWheel.HLinq.PgSql;
#else
using Microsoft.EntityFrameworkCore;
#endif
using HamsterWheel.HLinq.Pipeline.Applier.Builders;
using HamsterWheel.HLinq.Pipeline.Parser;
using HamsterWheel.HLinq.Pipeline.Tokenizer;
using HamsterWheel.HLinq.UnitTests.TestUtils;

namespace HamsterWheel.HLinq.UnitTests.Pipeline.Applier.Builders;

public partial class ExpressionBuilderUnitTests
{
    private readonly ExpressionBuilder _sut;
    private static readonly TestServicesCollection HLinqServices = new();
    private readonly HLinqTokenizer _tokenizer = new(HLinqServices.TokenPossibilities);
    private readonly HLinqParser _parser = new(HLinqServices.Parsers);
    private readonly ParametersConverter _parametersConverter = new();

    public ExpressionBuilderUnitTests()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        _sut = new ExpressionBuilder(HLinqServices.PropertiesCache, HLinqServices.MethodsCache,
            HLinqServices.ConverterFactory, _parametersConverter,
            [
#if !NETSTANDARD
                new PgSqlEntityFrameworkStaticMethodProvider()
#else
                new DummyStaticMethod()
#endif
            ]);
    }
}

#if NETSTANDARD
public class DummyStaticMethod : IStaticMethodSource
{
    public Type[] Types { get; } = [typeof(DummyMethods)];
}

public static class DummyMethods
{
    public static bool ILike(this DbFunctions _, string matchExpression, string pattern)
        => throw new InvalidOperationException("Dummy method should not be called!");
}
#endif
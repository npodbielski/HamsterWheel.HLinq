using System.Globalization;
using HamsterWheel.HLinq.Builders;
using HamsterWheel.HLinq.Parsers;
using HamsterWheel.HLinq.PgSql;
using HamsterWheel.HLinq.Reflection;
using HamsterWheel.HLinq.Tokenizer;

namespace HamsterWheel.HLinq.UnitTests;

public partial class ExpressionBuilderUnitTests
{
    private readonly ExpressionBuilder _sut;
    private static readonly HLinqCore HLinqCore = new();
    private readonly HLinqTokenizer _tokenizer = new(HLinqCore.TokenPossibilities);
    private readonly HLinqParser _parser = new(HLinqCore.Parsers);
    private readonly ParametersConverter _parametersConverter = new();

    public ExpressionBuilderUnitTests()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        var propertiesCache = new PropertiesCache();
        var methodsCache = new MethodsCache();
        _sut = new ExpressionBuilder(propertiesCache, methodsCache, HLinqCore.ConverterFactory, _parametersConverter,
            [new EntityFrameworkStaticMethodProvider()]);
    }
}
using System.Globalization;
using HamsterWheel.HLinq.PgSql;
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
        _sut = new ExpressionBuilder(HLinqServices.PropertiesCache, HLinqServices.MethodsCache, HLinqServices.ConverterFactory, _parametersConverter,
            [new EntityFrameworkStaticMethodProvider()]);
    }
}
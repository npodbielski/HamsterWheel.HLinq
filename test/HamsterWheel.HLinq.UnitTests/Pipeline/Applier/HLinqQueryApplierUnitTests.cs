using HamsterWheel.HLinq.Pipeline.Applier;
using HamsterWheel.HLinq.Pipeline.Parser;
using HamsterWheel.HLinq.Pipeline.Tokenizer;
using HamsterWheel.HLinq.Reflection;
using HamsterWheel.HLinq.Request;
using HamsterWheel.HLinq.UnitTests.TestUtils;
using Microsoft.Extensions.DependencyInjection;

namespace HamsterWheel.HLinq.UnitTests.Pipeline.Applier;

public partial class HLinqQueryApplierUnitTests
{
    private readonly HLinqTokenizer _tokenizer = new(ServicesCollection.TokenPossibilities);
    private readonly HLinqParser _parser = new(ServicesCollection.Parsers);
    private static readonly TestServicesCollection ServicesCollection = new();

    private readonly HLinqQuery<object>.HLinqQueryApplier _sut =
        new(ServicesCollection.Provider.GetRequiredService<IElementApplierFactory>(), ServicesCollection.Provider.GetRequiredService<IMethodsCache>());
}
using HamsterWheel.HLinq.Parsers;
using HamsterWheel.HLinq.Request;
using HamsterWheel.HLinq.Tokenizer;

namespace HamsterWheel.HLinq.UnitTests;

public partial class HLinqQueryApplierUnitTests
{
    private readonly HLinqTokenizer _tokenizer = new(ServicesCollection);
    private readonly HLinqParser _parser = new(ServicesCollection);
    private static readonly HLinqCore HLinqCore = new();
    private static readonly HLinqServicesCollection ServicesCollection = new(HLinqCore);
    private readonly HLinqQuery<object>.HLinqQueryApplier _sut = (HLinqQuery<object>.HLinqQueryApplier)HLinqCore.QueryApplier;
}
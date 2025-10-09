using HamsterWheel.HLinq.Parsers;
using HamsterWheel.HLinq.Request;
using HamsterWheel.HLinq.Tokenizer;

namespace HamsterWheel.HLinq.UnitTests;

public partial class HLinqQueryApplierUnitTests
{
    private readonly HLinqTokenizer _tokenizer = new(HLinqCore.TokenPossibilities);
    private readonly HLinqParser _parser = new(HLinqCore.Parsers);
    private static readonly HLinqCore HLinqCore = new();
    private readonly HLinqQuery<object>.HLinqQueryApplier _sut = (HLinqQuery<object>.HLinqQueryApplier)HLinqCore.QueryApplier;
}
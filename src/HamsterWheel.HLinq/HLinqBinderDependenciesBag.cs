using HamsterWheel.HLinq.Appliers;
using HamsterWheel.HLinq.Parsers;
using HamsterWheel.HLinq.Reflection;
using HamsterWheel.HLinq.Tokens;

namespace HamsterWheel.HLinq;

public class HLinqBinderDependenciesBag(
    IHLinqQueryApplier queryApplier,
    IHLinqOptions options,
    IHLinqParser parser,
    IHLinqTokenizer tokenizer,
    IMethodsCache methodsCache)
{
    public IHLinqQueryApplier QueryApplier => queryApplier;
    public IHLinqOptions Options => options;
    public IHLinqParser HLinqParser => parser;
    public IHLinqTokenizer Tokenizer => tokenizer;
    public IMethodsCache MethodsCache => methodsCache;
}
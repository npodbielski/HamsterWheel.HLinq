using HamsterWheel.HLinq.Pipeline.Applier;
using HamsterWheel.HLinq.Pipeline.Parser;
using HamsterWheel.HLinq.Pipeline.Tokenizer;
using HamsterWheel.HLinq.Reflection;

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
using HamsterWheel.HLinq.Appliers;
using HamsterWheel.HLinq.Builders;
using HamsterWheel.HLinq.Parsers;
using HamsterWheel.HLinq.Reflection;
using HamsterWheel.HLinq.Tokens;
namespace HamsterWheel.HLinq;

public interface IHLinqCore
{
    IElementParser[] Parsers { get; }
    IHLinqTokenPossibility[] TokenPossibilities { get; }
    IElementToExpressionConverter[] ExpressionConverters { get; }
    IElementToMemberAssignmentConverter[] AssignmentConverters { get; }
    IHLinqQueryApplier QueryApplier { get; }
    IHLinqOptions Options { get; }
    IHLinqParser HLinqParser { get; }
    IHLinqTokenizer Tokenizer { get; }
    IMethodsCache MethodsCache { get; }
    T[] GetFromAssemblyWith<TSource, T>();
}
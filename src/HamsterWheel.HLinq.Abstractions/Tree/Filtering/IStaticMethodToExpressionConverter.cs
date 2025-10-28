using System.Linq.Expressions;
using HamsterWheel.HLinq.Pipeline.Applier.Builders;

namespace HamsterWheel.HLinq.Tree.Filtering;

public interface IStaticMethodToExpressionConverter
{
    Expression BuildStatic(IBuilderContext context, IMethod method, IParametersConverter parametersConverter);
}
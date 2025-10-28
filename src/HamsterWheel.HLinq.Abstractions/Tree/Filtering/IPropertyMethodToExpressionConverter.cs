using System.Linq.Expressions;
using HamsterWheel.HLinq.Pipeline.Applier.Builders;

namespace HamsterWheel.HLinq.Tree.Filtering;

public interface IPropertyMethodToExpressionConverter
{
    Expression BuildInstance(IBuilderContext context, IMethod method, IParametersConverter parametersConverter);
}
using System.Linq.Expressions;
using HamsterWheel.HLinq.Builders;

namespace HamsterWheel.HLinq.Tree.Filtering;

public interface IPropertyMethodToExpressionConverter
{
    Expression BuildInstance(IBuilderContext context, IMethod method, IParametersConverter parametersConverter);
}
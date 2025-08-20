using System.Linq.Expressions;
using System.Reflection;
using HamsterWheel.HLinq.Parsers;
using HamsterWheel.HLinq.Tree;
using HamsterWheel.HLinq.Tree.Filter;
using HamsterWheel.HLinq.Tree.Select;

namespace HamsterWheel.HLinq.Builders;

public interface IExpressionBuilder
{
    LambdaExpression GetFilter(Type source, IWhereRoot query, string hLinqQuery);
    Expression BuildBody(IBuilderContext context, ITreeBranch root);
    (LambdaExpression Expression, Type ResultType) GetSelect(Type source, ISelectRoot query, string hLinqQuery);
    (LambdaExpression Expression, Type PropType) GetProperty(Type source, ITreeRoot query, string hLinqQuery);
    Expression ToExpression<T>(IBuilderContext context, T element) where T : ITreeElement;

    MemberAssignment ToMemberAssignment<T>(IBuilderContext context, T element, Type destinationType)
        where T : ITreeElement;

    IElementToMemberAssignmentConverter GetToMemberBindingConverter<T>(T element) where T : ITreeElement;

    IParametersConverter GetParametersConverter();

    (MethodInfo method, IBindingParameterInfo[] parameters)[] GetMostProbableMethods(string? name,
        (string value, MemberExpression? expression)[] parameters);

    MethodInfo[] GetMostProbableMethods(Type propType, string name,
        (string value, MemberExpression? expression)[] parameters);

    IPropertyContext GetPropertyWithType(Type sourceType, Expression source, string[] path);
}
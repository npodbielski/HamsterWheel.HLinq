using System.Linq.Expressions;
using System.Reflection;
using DynamicAnonymousType;
using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Pipeline.Applier.Builders;
using HamsterWheel.HLinq.Pipeline.Parser;

namespace HamsterWheel.HLinq.Tree.Selecting;

/// <summary>
/// Builds expression for nested selects.
/// </summary>
public sealed class NestedSelectBuilder : INestedSelectBuilder
{
    private static readonly MethodInfo SelectMany = typeof(Enumerable)
        .GetMethods(BindingFlags.Static | BindingFlags.Public)
        .Single(m => m.Name == nameof(Enumerable.Select)
                     && m.GetParameters().Length == 2
                     && m.GetParameters()[1].ParameterType is
                     {
                         IsGenericType: true, GenericTypeArguments.Length: 2
                     });

    private static readonly MethodInfo ToList = typeof(Enumerable)
        .GetMethods(BindingFlags.Static | BindingFlags.Public)
        .Single(m => m.Name == nameof(Enumerable.ToList) && m.GetParameters().Length == 1);

    public Type GetResultType(ITreeElement assignmentElement, ITreeElement nestedElement,
        IBuilderContext context) =>
        GetResultType((PropertyAssignment)assignmentElement, (NestedSelect)nestedElement, context);

    private static Type GetResultType(PropertyAssignment assignment, NestedSelect nested,
        IBuilderContext context)
    {
        var source = GetSourceExpression(assignment, context);
        return IsCollection(source.Type)
            ? typeof(List<>).MakeGenericType(CreateTypeForNestedSelect(nested, context, source))
            : CreateTypeForNestedSelect(nested, context, source);
    }

    public Expression Build(ITreeElement assignmentElement, ITreeElement nestedElement, IBuilderContext context) =>
        Build((PropertyAssignment)assignmentElement, (NestedSelect)nestedElement, context);

    private static Expression Build(PropertyAssignment assignment, NestedSelect nested,
        IBuilderContext context)
    {
        var source = GetSourceExpression(assignment, context);
        var shape = CreateTypeForNestedSelect(nested, context, source);

        if (!IsCollection(source.Type))
        {
            var referenceScope = new NestedScopeBuilderContext(context, source, source.Type);
            var shaped = Projection(shape, nested, referenceScope);
            //the related row may be missing, and a shape of the missing row is no shape at all -
            //the same null a hand-written member path answers with
            return source.Type.IsValueType
                ? shaped
                : Expression.Condition(
                    Expression.NotEqual(source, Expression.Constant(null, source.Type)),
                    shaped,
                    Expression.Default(shape));
        }

        var innerParam = Expression.Parameter(ElementTypeOf(source.Type), "e");
        var scope = new NestedScopeBuilderContext(context, innerParam, innerParam.Type);
        var projection = Projection(shape, nested, scope);
        var lambda = Expression.Lambda(
            typeof(Func<,>).MakeGenericType(innerParam.Type, shape), projection, innerParam);
        var project = SelectMany.MakeGenericMethod(innerParam.Type, shape);
        var list = ToList.MakeGenericMethod(shape);
        return Expression.Call(list, Expression.Call(project, source, lambda));
    }

    private static MemberInitExpression Projection(Type shape, NestedSelect nested,
        INestedScopeBuilderContext scope) =>
        Expression.MemberInit(Expression.New(shape),
            nested.Members.Select(m => scope.ToMemberAssignment(m, shape)));

    private static Type CreateTypeForNestedSelect(NestedSelect nested, IBuilderContext context, Expression source)
    {
        var element = IsCollection(source.Type) ? ElementTypeOf(source.Type) : source.Type;
        var scope = new NestedScopeBuilderContext(context, Expression.Parameter(element), element);
        return DynamicFactory.CreateType(nested.Members.Select(m =>
        {
            var prop = scope.ToPropInfo(m);
            return (prop.Name, prop.Type);
        }));
    }

    private static MemberExpression GetSourceExpression(PropertyAssignment assignment, IBuilderContext context)
    {
        if (assignment.NestedSelectProperty is { } property)
        {
            var path = property.GetPath(context.HLinqQuery);
            return context.Builder.GetProperty(context.Type, context.Param, path, context).Member;
        }

        if (assignment.NestedSelectInnerProperty is { } inner)
        {
            var scope = context as INestedScopeBuilderContext ??
                        throw new InnerProperty.Converter.InnerPropertyOutsideOfNestedSelectException();
            var path = inner.GetPath(context.HLinqQuery);
            return context.Builder.GetProperty(scope.InnerType, scope.InnerSource, path, context).Member;
        }

        throw new NestedSelectHasNoNavigationException();
    }

    private static bool IsCollection(Type type) =>
        type != typeof(string) &&
        (type.IsArray || typeof(System.Collections.IEnumerable).IsAssignableFrom(type));

    private static Type ElementTypeOf(Type collection) =>
        collection.IsArray
            ? collection.GetElementType()!
            : collection.GetInterfaces().Concat([collection])
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                .Select(i => i.GenericTypeArguments[0])
                .FirstOrDefault() ?? collection;

    private sealed class NestedSelectHasNoNavigationException()
        : HLinqQueryException(
            "A nested select needs the navigation its call hangs on; write it as x.Navigation.select(...).");
}
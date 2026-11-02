using System.Linq.Expressions;
using System.Reflection;
using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Pipeline.Applier.Builders;
using HamsterWheel.HLinq.Reflection;

namespace HamsterWheel.HLinq.Tree.Filtering;

/// <summary>
/// Translates an aggregate method call of a query to an <see cref="Enumerable"/> call.
/// The name of an aggregate is resolved from Enumerable's own signatures rather than a list kept
/// here, so what a query may name changes only when LINQ does.
/// </summary>
public sealed partial class AggregateMethodToExpressionConverter(IAggregatesCache cache) : IAggregateMethodToExpressionConverter
{
    public Expression BuildOverEntity(IBuilderContext context, IMethod method)
    {
        var name = method.GetName(context.HLinqQuery);
        if (!cache.IsAggregateName(name))
        {
            throw new NotAnAggregateException(name, cache.Names());
        }

        if (cache.ItemOf(context.Type) is not { } item)
        {
            throw new EntityAggregateNeedsGroupException(name);
        }

        var (candidate, selector) = Resolve(context, method, item);
        return selector is null
            ? Call(candidate, context.Param)
            : Call(candidate, context.Param, selector.Value.Row, selector.Value.Member);
    }

    public Expression? BindOverMember(IBuilderContext context, IMethod method, Expression source)
    {
        var name = method.GetName(context.HLinqQuery);
        if (!cache.IsAggregateName(name))
        {
            return null;
        }

        if (cache.ItemOf(source.Type) is not { } item)
        {
            throw new AggregateArgumentsException(name,
                $"'{name}' aggregates a collection, but the property the call hangs on is a " +
                $"'{source.Type.Name}', not a collection.");
        }

        var (candidate, selector) = Resolve(context, method, item);
        return selector is null
            ? Call(candidate, source)
            : Call(candidate, source, selector.Value.Row, selector.Value.Member);
    }

    /// <summary>
    /// The type the aggregate answers, without building the call.
    /// </summary>
    public Type ReturnType(IBuilderContext context, IMethod method, Type sourceType)
    {
        var name = method.GetName(context.HLinqQuery);
        if (!cache.IsAggregateName(name))
        {
            throw new NotAnAggregateException(name, cache.Names());
        }

        if (cache.ItemOf(sourceType) is not { } item)
        {
            throw new AggregateArgumentsException(name,
                $"'{name}' aggregates a collection, but what it hangs on is a " +
                $"'{sourceType.Name}', not a collection.");
        }

        return Resolve(context, method, item).Method.ReturnType;
    }

    private (MethodInfo Method, (ParameterExpression Row, MemberExpression Member)? Value) Resolve(
        IBuilderContext context, IMethod method, Type item)
    {
        var name = method.GetName(context.HLinqQuery);
        var arguments = method.Children.OfType<IMethodParamElement>().ToArray();
        var argumentPath = method.ArgumentPath(context.HLinqQuery);

        if (arguments.Length > 0 &&
            (argumentPath is null || arguments.Length != argumentPath.Length
                                    || arguments.Any(a => a is not Property)))
        {
            throw new AggregateArgumentsException(name,
                argumentPath is null
                    ? $"'{name}()' binds a member to aggregate on (i.e. {name}(a.Age)), not what was written between its brackets."
                    : $"'{name}()' binds one member to aggregate on, not {arguments.Length}.");
        }

        if (argumentPath is null)
        {
            var (bare, needsComparable) = cache.Resolve(name, item, value: null);
            return bare is null ? throw Refused(name, item, value: null, needsComparable) : (bare, null);
        }

        //the selector only needs the member's type to bind, the expression of the path is made
        //when the call itself is built
        var rowParam = Expression.Parameter(item);
        var member = context.Builder
            .GetProperty(item, rowParam, argumentPath, context)
            .Member;

        var (selected, needs) = cache.Resolve(name, item, member.Type);
        return selected is null ? throw Refused(name, item, member.Type, needs) : (selected, (rowParam, member));
    }

    private static AggregateArgumentsException Refused(string name, Type item, Type? value, bool needsComparable)
        => needsComparable
            ? new AggregateArgumentsException(name,
                $"'{name}()' over '{item.Name}' values has to name a member to fold " +
                $"(i.e. {name}(a.Age)); on its own it folds only values that compare to each other " +
                "- numbers, dates, text.")
            : new AggregateArgumentsException(name,
                $"no Enumerable overload of {name} binds a source of {item.Name}"
                + (value is null ? "." : $" and values of {value.Name}."));

    private static MethodCallExpression Call(MethodInfo made, Expression source) =>
        Expression.Call(null, made, source);

    private static MethodCallExpression Call(MethodInfo made, Expression source,
        ParameterExpression rowParam, MemberExpression member)
    {
        var funcType = made.GetParameters()[1].ParameterType;
        var valueType = funcType.GetGenericArguments()[1];
        var body = valueType == member.Type
            ? (Expression)member
            : Expression.Convert(member, valueType);
        var lambda = Expression.Lambda(funcType, body, rowParam);
        return Expression.Call(null, made, source, lambda);
    }
}

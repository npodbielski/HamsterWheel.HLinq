using System.Linq.Expressions;
using System.Reflection;
using HamsterWheel.HLinq.Data.Converters;

namespace HamsterWheel.HLinq.Client;

public class HLinqClientQueryBuilder<T> : ResponseHLinqClientQueryBuilder<T[]>
{
    public UnorderedHLinqClientQueryBuilder<TResult> Select<TResult>(Expression<Func<T, TResult>> selector)
    {
        switch (selector.Body)
        {
            case MemberExpression me:
                AddDotIfNecessary();
                Query.Append("select[").Append(me).Append(']');
                break;
            case NewExpression ne:
            {
                var m = ne.Members;
                if (m is not null)
                {
                    AddDotIfNecessary();
                    Query.Append("select[").Append(ne.ToString().Split('(', ')')[1]).Append(']');
                }

                break;
            }
        }

        return Next<TResult>(this);
    }

    public UnorderedHLinqClientQueryBuilder<T> Where(Expression<Func<T, bool>> predicate)
    {
        if (predicate.Body is MethodCallExpression cd &&
            cd.Method.GetParameters().FirstOrDefault()?.ParameterType.FullName ==
            "Microsoft.EntityFrameworkCore.DbFunctions")
        {
            AddDotIfNecessary();
            Query.Append($"where[{predicate.Body.ToString().Replace("EF.Functions.", "")}]");
        }
        else if (predicate.Body is BinaryExpression be)
        {
            var left = be.Left is UnaryExpression lue ? lue.Operand : be.Left;
            var right = be.Right is UnaryExpression rue ? rue.Operand : be.Right;

            object? value = null;
            if (right is ConstantExpression ce)
            {
                value = ce.Value ?? "null";
            }

            if (value is null && right is MemberExpression { Expression: ConstantExpression mce } me)
            {
                value = me.Member is FieldInfo fi ? fi.GetValue(mce.Value) ?? "null" : null;
            }

            if (value is not null)
            {
                AddDotIfNecessary();
                Query.Append("where[")
                    .Append(
                        be.ToString().Replace(be.Left.ToString(), left.ToString()).TrimStart('(')
                            .Replace(be.Right.ToString(), DefaultConverter.Instance.ConvertTo<string>(value)).TrimEnd(')')
                    )
                    .Append(']');
            }
        }
        else
        {
            AddDotIfNecessary();
            Query.Append($"where[{predicate.Body.ToString()}]");
        }
        
        return Next<T>(this);
    }

    public UnorderedHLinqClientQueryBuilder<T> Skip(int skip)
    {
        AddDotIfNecessary();
        Query.Append($"skip[{skip}]");
        return Next<T>(this);
    }

    public UnorderedHLinqClientQueryBuilder<T> Take(int skip)
    {
        AddDotIfNecessary();
        Query.Append($"take[{skip}]");
        return Next<T>(this);
    }

    public ResponseHLinqClientQueryBuilder<int> Count()
    {
        AddDotIfNecessary();
        Query.Append("count[]");
        return NextCounted(this);
    }
}
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using HamsterWheel.HLinq.Data;
using HamsterWheel.HLinq.Data.Converters;
using HamsterWheel.HLinq.Reflection;

namespace HamsterWheel.HLinq.Client.RootBuilders;

public class WhereRootBuilder<T>(StringBuilder query)
{
    public void Build(Expression<Func<T, bool>> predicate)
    {
        query.Append("where[");

        if (IsEfMethodCall(predicate.Body))
        {
            GetStringForWhereMethodCall(predicate.Body);
        }
        else if (predicate.Body is BinaryExpression be)
        {
            GetStringForBinaryExpression(be, false);
        }
        else
        {
            query.Append(predicate.Body);
        }

        query.Append(']');
    }

    private void GetStringForWhereMethodCall(Expression expression) =>
        query.Append(expression.ToString().Replace("EF.Functions.", "").Replace("\"", ""));

    private void GetStringForBinaryExpression(BinaryExpression be, bool nested = true)
    {
        var left = be.Left is UnaryExpression lue ? lue.Operand : be.Left;
        var right = be.Right is UnaryExpression rue ? rue.Operand : be.Right;

        if (left is BinaryExpression leftBe && right is BinaryExpression rightBe)
        {
            if (nested)
            {
                query.Append('(');
            }
            GetStringForBinaryExpression(leftBe);
            AppendLogicalOperator(be);
            GetStringForBinaryExpression(rightBe);
            if (nested)
            {
                query.Append(')');
            }
        }
        if (right is MethodCallExpression rmce && left is MethodCallExpression lmce)
        {
            if (nested)
            {
                query.Append('(');
            }
            query.Append(lmce);
            AppendLogicalOperator(be);
            query.Append(rmce);
            if (nested)
            {
                query.Append(')');
            }
        }
        else
        {
            object? value = null;

            if (right is ConstantExpression ce)
            {
                value = ce.Value ?? "null";
            }

            if (value is null && right is MemberExpression { Expression: ConstantExpression mece } me)
            {
                value = me.Member is FieldInfo fi ? fi.GetValue(mece.Value) ?? "null" : null;
            }

            if (value is not null)
            {
                query.Append(
                    be.ToString().Replace(be.Left.ToString(), left.ToString()).TrimStart('(')
                        .Replace(be.Right.ToString(), DefaultConverter.Instance.ConvertTo<string?>(value) ?? NullKeyword.Keyword)
                        .TrimEnd(')')
                );
            }
        }
    }

    private void AppendLogicalOperator(BinaryExpression be) =>
        query.Append(
            $" {be.NodeType switch { ExpressionType.AndAlso => "&&", ExpressionType.OrElse => "||", _ => " " }} ");

    private static bool IsEfMethodCall(Expression expression) =>
        expression is MethodCallExpression cd && EfDbFunctionsMatcher.IsEfDbFunction(cd.Method);
}
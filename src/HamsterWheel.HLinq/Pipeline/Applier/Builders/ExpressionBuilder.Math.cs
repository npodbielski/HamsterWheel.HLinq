using System.Linq.Expressions;
using System.Reflection;
using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Pipeline.Parser;
using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Selecting;
using HamsterWheel.HLinq.Tree;
using HamsterWheel.HLinq.Tree.Filtering;
using HamsterWheel.HLinq.Tree.Selecting;

namespace HamsterWheel.HLinq.Pipeline.Applier.Builders;

//The C# compiler's own decisions about operators - promotion, meaning, refusal - made on the
//expression tree here, because an expression tree gets no compiler to make them.
public sealed partial class ExpressionBuilder
{
    public Expression FoldMath(IBuilderContext context, IReadOnlyList<ITreeElement> children)
    {
        var values = new List<Expression>();
        var operators = new List<IMathOperatorToken>();

        //the rename names the member, it is not one of its values
        values.Add(BuildMathValue(context, FirstValue(children)));

        foreach (var term in children.OfType<MathTerm>())
        {
            operators.Add(term.Operator);
            values.Add(BuildMathValue(context, [.. term.Children]));
        }

        return FoldAdditive(FoldMultiplicative(values, operators));
    }

    /// <summary>
    /// The elements of the value written before the first operator, without the rename.
    /// </summary>
    private static ITreeElement[] FirstValue(IReadOnlyList<ITreeElement> children) =>
    [
        .. children
            .TakeWhile(child => child is not MathTerm)
            .Where(child => child is not IInitializerPropertyName)
    ];

    //a call answers with what it returns; only a plain member or a constant answers with itself
    private Expression BuildMathValue(IBuilderContext context, ITreeElement[] elements)
    {
        if (elements.OfType<Method>().SingleOrDefault() is not { } call)
        {
            return elements.Length == 1
                ? context.ToExpression(elements[0])
                : throw new MathValueHoldsNothingException();
        }

        var source = CallSourceOf(elements);
        var callContext = new MethodCallConditionBuilderContext(context)
        {
            MethodSource = source is null ? null : GetCallMember(context, source)
        };

        return callContext.ToExpression(call);
    }

    private static ITreeElement? CallSourceOf(ITreeElement[] elements)
    {
        //the source of a call is the member written in front of it
        ITreeElement? inner = elements.OfType<IInnerProperty>().SingleOrDefault();
        ITreeElement? property = elements.TakeWhile(element => element is not Method)
            .OfType<IProperty>().SingleOrDefault();
        return inner ?? property;
    }

    private static MemberExpression? GetCallMember(IBuilderContext context, ITreeElement source)
    {
        var path = source switch
        {
            IInnerProperty inner => inner.GetPath(context.HLinqQuery),
            IProperty property => property.GetPath(context.HLinqQuery),
            _ => null
        };

        if (path is null)
        {
            return null;
        }

        //an inner property reads the scope reached through a property, a member reads the row
        return source is IInnerProperty && context is INestedScopeBuilderContext scope
            ? scope.Builder.GetProperty(scope.InnerType, scope.InnerSource, path, scope).Member
            : context.Builder.GetProperty(context.Type, context.Param, path, context).Member;
    }

    private sealed class MathValueHoldsNothingException()
        : HLinqQueryException("A value of an expression has to be a call or an element, this one holds neither.");

    /// <summary>
    /// One value per additive operator, each remembering the operator written in front of it.
    /// </summary>
    private static List<(Expression Value, IMathOperatorToken? AdditiveBefore)>
        FoldMultiplicative(List<Expression> values, List<IMathOperatorToken> operators)
    {
        var terms = new List<(Expression Value, IMathOperatorToken? AdditiveBefore)> { (values[0], null) };

        for (var index = 0; index < operators.Count; index++)
        {
            var op = operators[index];
            var value = values[index + 1];
            if (IsMultiplicative(op))
            {
                var (fold, additiveBefore) = terms[^1];
                terms[^1] = (Operate(fold, op, value), additiveBefore);
            }
            else
            {
                terms.Add((value, op));
            }
        }

        return terms;
    }

    private static Expression FoldAdditive(
        List<(Expression Value, IMathOperatorToken? AdditiveBefore)> terms)
    {
        var fold = terms[0].Value;

        foreach (var (value, op) in terms.Skip(1))
        {
            fold = Operate(fold, op!, value);
        }

        return fold;
    }

    private static Expression Operate(Expression left, IMathOperatorToken op, Expression right)
    {
        if (IsEnumType(left.Type) || IsEnumType(right.Type))
        {
            //an engine answers no operation for an enumeration: two columns, and no exception
            throw new PropertyAssignment.MathOperandIsAnEnumerationException(
                NameOf(left) ?? NameOf(right) ?? "value");
        }

        if (op is Plus && (left.Type == typeof(string) || right.Type == typeof(string)))
        {
            try
            {
                //C# binds string addition to this operator; every engine reads it as its own
                //concatenation
                return Expression.Add(AsText(left), AsText(right), StringConcat);
            }
            catch (InvalidOperationException)
            {
                throw new PropertyAssignment.MathOperandTypesCannotBeOperatedException(left.Type, right.Type, "+");
            }
        }

        try
        {
            return op switch
            {
                //an expression tree gets no compiler to promote mixed widths first
                Plus => NumericOperate(left, right, Expression.Add),
                Minus => NumericOperate(left, right, Expression.Subtract),
                Multiply => NumericOperate(left, right, Expression.Multiply),
                Divide => NumericOperate(left, right, Expression.Divide),
                _ => throw new ArgumentOutOfRangeException(nameof(op), op, null)
            };
        }
        catch (InvalidOperationException)
        {
            //the pair has no operator of C#, i.e. two dates - hand-written Linq's own refusal
            throw new PropertyAssignment.MathOperandTypesCannotBeOperatedException(left.Type, right.Type,
                KeywordOf(op));
        }
    }

    private static Expression NumericOperate(Expression left, Expression right,
        Func<Expression, Expression, Expression> operate)
    {
        if (CommonNumericType(left.Type, right.Type) is { } common)
        {
            return operate(ConvertTo(left, common), ConvertTo(right, common));
        }

        return operate(left, right);
    }

    private static string KeywordOf(IMathOperatorToken op) => op switch
    {
        Plus => "+",
        Minus => "-",
        Multiply => "*",
        Divide => "/",
        _ => op.GetType().Name
    };

    private static bool IsMultiplicative(IMathOperatorToken op) => op is Multiply or Divide;

    private static bool IsEnumType(Type type) => (Nullable.GetUnderlyingType(type) ?? type).IsEnum;

    private static string? NameOf(Expression value) => (value as MemberExpression)?.Member.Name;

    private static Expression ConvertTo(Expression value, Type type) =>
        value.Type == type ? value : Expression.Convert(value, type);

    /// <summary>
    /// The number that covers both values, or null when the two are no numbers at all.
    /// </summary>
    private static Type? CommonNumericType(Type left, Type right)
    {
        var lifted = Nullable.GetUnderlyingType(left) ?? left;
        var other = Nullable.GetUnderlyingType(right) ?? right;
        if (!lifted.IsNumeric() || !other.IsNumeric())
        {
            return null;
        }

        var common = FindCommonNumericType(lifted, other);
        if (common is null)
        {
            return null;
        }

        var nullable = left.IsNullable() || right.IsNullable();

        return nullable ? typeof(Nullable<>).MakeGenericType(common) : common;
    }

    /// <summary>
    /// The smallest number holding every value of both lifted types, or null when none does.
    /// </summary>
    private static Type? FindCommonNumericType(Type lifted, Type other)
    {
        if (IsDecimalAndBinaryFloatingPoint(lifted, other) || IsDecimalAndBinaryFloatingPoint(other, lifted) ||
            lifted == typeof(double) || other == typeof(double))
        {
            return typeof(double);
        }

        if (lifted == typeof(float) || other == typeof(float))
        {
            return typeof(float);
        }

        if (lifted == typeof(decimal) || other == typeof(decimal))
        {
            return typeof(decimal);
        }

        if (lifted == typeof(ulong) || other == typeof(ulong))
        {
            //a signed number has values a ulong cannot hold, and no number of C# holds both
            //ranges, so only the unsigned widths meet in a ulong
            var otherOne = lifted == typeof(ulong) ? other : lifted;
            return otherOne.IsSignedIntegral()
                ? null
                : typeof(ulong);
        }

        if (lifted == typeof(long) || other == typeof(long))
        {
            return typeof(long);
        }

        if (lifted == typeof(uint) || other == typeof(uint))
        {
            //a signed number has negatives a uint cannot hold, so only the unsigned widths
            //narrower than a uint meet in a uint
            var otherOne = lifted == typeof(uint) ? other : lifted;
            return otherOne == typeof(byte) || otherOne == typeof(ushort)
                ? typeof(uint)
                : typeof(long);
        }

        return typeof(int);
    }

    private static Expression AsText(Expression value) =>
        value.Type == typeof(string)
            ? value
            : Expression.Call(value, TextOf(value.Type));

    private static MethodInfo TextOf(Type type) =>
        type.GetMethod(nameof(ToString), Type.EmptyTypes) ??
        throw new PropertyAssignment.MathOperandTypesCannotBeOperatedException(type, typeof(string), "+");

    private static readonly MethodInfo StringConcat =
        typeof(string).GetMethod(nameof(string.Concat), [typeof(string), typeof(string)])!;
}
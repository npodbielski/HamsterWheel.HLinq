using System.Linq.Expressions;
using HamsterWheel.HLinq.Tree.Filtering;
using HamsterWheel.HLinq.Tokens.Filtering;

namespace HamsterWheel.HLinq.Pipeline.Applier.Builders;

public sealed partial class ExpressionBuilder
{
    public Expression BuildComparison(IComparisonToken comparison, Expression left, Expression right)
    {
        if (left.Type != right.Type)
        {
            if (ComparisonCommonType(left.Type, right.Type) is not { } common)
            {
                throw new Condition.ComparisonOperandTypesCannotBeComparedException(left.Type, right.Type);
            }

            left = ConvertToCommon(left, common);
            right = ConvertToCommon(right, common);
        }

        try
        {
            return comparison switch
            {
                Equality => Expression.Equal(left, right),
                GreaterOrEqualThan => Expression.GreaterThanOrEqual(left, right),
                GreaterThan => Expression.GreaterThan(left, right),
                Inequality => Expression.NotEqual(left, right),
                LessThan => Expression.LessThan(left, right),
                LessOrEqualThan => Expression.LessThanOrEqual(left, right),
                _ => throw new Condition.ComparisonConditionMustHaveComparisonTokenException()
            };
        }
        catch (Exception e) when (e is InvalidOperationException or ArgumentException)
        {
            //two expressions of one type with no comparison operator of C# - the same refusal
            throw new Condition.ComparisonOperandTypesCannotBeComparedException(left.Type, right.Type);
        }
    }

    /// <summary>
    /// The type a comparison of the two types runs in, or null when they have no common type.
    /// </summary>
    private static Type? ComparisonCommonType(Type left, Type right)
    {
        var lifted = Nullable.GetUnderlyingType(left) ?? left;
        var other = Nullable.GetUnderlyingType(right) ?? right;
        var nullable = lifted != left || other != right;

        if ((lifted.IsEnum && other == typeof(string)) || (other.IsEnum && lifted == typeof(string)))
        {
            return typeof(string);
        }

        if (!lifted.IsValueType || !other.IsValueType)
        {
            return null;
        }

        var common = lifted == other
            ? lifted
            : lifted.IsNumeric() && other.IsNumeric()
                ? FindCommonNumericType(lifted, other)
                : OtherComparisonMeeting(lifted, other);

        if (common is null)
        {
            return null;
        }

        return nullable ? typeof(Nullable<>).MakeGenericType(common) : common;
    }

    /// <summary>
    /// The meeting of two different value types when at least one of them is no number.
    /// </summary>
    private static Type? OtherComparisonMeeting(Type lifted, Type other)
    {
        if (lifted.IsEnum != other.IsEnum)
        {
            //an enum stands for the integer it is declared with, so the pair is that number's
            //question; a `bool` or a `Guid` has no such number and meets in nothing
            var underlying = Enum.GetUnderlyingType(lifted.IsEnum ? lifted : other);
            var number = lifted.IsEnum ? other : lifted;

            return underlying == number
                ? underlying
                : CommonComparisonOperator(underlying, number);
        }

        return CommonComparisonOperator(lifted, other);
    }

    private static bool IsDecimalAndBinaryFloatingPoint(Type one, Type other) =>
        one == typeof(decimal) && (other == typeof(double) || other == typeof(float));

    /// <summary>
    /// The side of a comparison stated as the common type of both sides.
    /// </summary>
    private static Expression ConvertToCommon(Expression value, Type common)
    {
        if (common == typeof(string) && (Nullable.GetUnderlyingType(value.Type) ?? value.Type).IsEnum)
        {
            //EF translates this ToString to the pack's `CASE` over the mapped values
            var toString = typeof(object).GetMethod(nameof(ToString), Type.EmptyTypes)!;
            return Expression.Call(value, toString);
        }

        return ConvertTo(value, common);
    }

    /// <summary>
    /// The type C# binds a comparison operator in for the two types, or null for no operator.
    /// </summary>
    private static Type? CommonComparisonOperator(Type left, Type right)
    {
        //the narrowest candidate both sides convert to; anything else is C#'s refusal
        var candidates = new[]
        {
            typeof(int), typeof(uint), typeof(long), typeof(ulong),
            typeof(float), typeof(double), typeof(decimal)
        }.Where(candidate => ImplicitlyConverts(left, candidate) && ImplicitlyConverts(right, candidate)).ToList();

        return candidates.SingleOrDefault(candidate =>
            candidates.All(other => other == candidate || ImplicitlyConverts(candidate, other)));
    }

    /// <summary>
    /// Whether C# widens one type to the other without a cast.
    /// </summary>
    private static bool ImplicitlyConverts(Type type, Type target)
    {
        if (type == target)
        {
            return true;
        }

        return ImplicitTargets.TryGetValue(type, out var targets) && Array.IndexOf(targets, target) >= 0;
    }

    //C#'s implicit numeric conversions, measured against the compiler
    private static readonly Dictionary<Type, Type[]> ImplicitTargets = new()
    {
        [typeof(sbyte)] = [typeof(short), typeof(int), typeof(long), typeof(float), typeof(double), typeof(decimal)],
        [typeof(byte)] =
        [
            typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float),
            typeof(double), typeof(decimal)
        ],
        [typeof(short)] = [typeof(int), typeof(long), typeof(float), typeof(double), typeof(decimal)],
        [typeof(ushort)] =
            [typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal)],
        [typeof(char)] =
        [
            typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double),
            typeof(decimal)
        ],
        [typeof(int)] = [typeof(long), typeof(float), typeof(double), typeof(decimal)],
        [typeof(uint)] = [typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal)],
        [typeof(long)] = [typeof(float), typeof(double), typeof(decimal)],
        [typeof(ulong)] = [typeof(float), typeof(double), typeof(decimal)],
        [typeof(float)] = [typeof(double)],
        [typeof(double)] = [],
        [typeof(decimal)] = []
    };
}
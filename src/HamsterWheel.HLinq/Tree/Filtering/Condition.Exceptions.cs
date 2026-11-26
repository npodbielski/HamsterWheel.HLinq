using HamsterWheel.HLinq.Diagnostics;
using HamsterWheel.HLinq.Exceptions;

namespace HamsterWheel.HLinq.Tree.Filtering;

partial class Condition
{
    public sealed class ConditionElementMustHaveLeftAndRightException()
        : HLinqQueryException(
            "Cannot build comparison expression if right or left expression is null. At least one side of a comparison has to be a property, 'where[1==1]' compares two values.")
    {
        public override HLinqErrorCode Code => HLinqErrorCode.ConstantOnlyComparison;
    }

    public sealed class ComparisonConditionMustHaveComparisonTokenException()
        : HLinqQueryException("Condition that is comparison need to have comparison token.")
    {
        public override HLinqErrorCode Code => HLinqErrorCode.InvalidQuery;
    }

    public sealed class NotSupportedConditionException()
        : HLinqQueryException("Condition need to be either method call: 'x.Name.StartsWith(abc)' or comparison 'x.Name==abc'")
    {
        public override HLinqErrorCode Code => HLinqErrorCode.NotACondition;
    }

    public sealed class ConditionMethodChildElementMissingException()
        : HLinqQueryException("Condition is method call condition but method child does not exits")
    {
        public override HLinqErrorCode Code => HLinqErrorCode.NotACondition;
    }

    public sealed class ComparisonOperandTypesCannotBeComparedException(Type left, Type right)
        : HLinqQueryException(
            $"Cannot compare a value of type '{left.Name}' with a value of type '{right.Name}' - use properties of the same type, or compare a property with a value of its type.")
    {
        public override HLinqErrorCode Code => HLinqErrorCode.InvalidComparison;
        public override string InvalidQueryChunk => $"{left.Name} vs {right.Name}";
    }
}
using HamsterWheel.HLinq.Exceptions;

namespace HamsterWheel.HLinq.Tree.Filter;

partial class Condition
{
    public sealed class ConditionElementMustHaveLeftAndRightException()
        : HLinqQueryException(
            "Cannot build comparison expression if right or left expression is null. If you used 'where[2==x.Int]' it is not supported. Use 'where[x.Int==2]' instead.");

    public sealed class ComparisonConditionMustHaveComparisonTokenException()
        : HLinqQueryException("Condition that is comparison need to have comparison token.");

    public sealed class NotSupportedConditionException()
        : HLinqQueryException("Condition need to be either method call: 'x.Name.StartsWith(abc)' or comparison 'x.Name==abc'");

    public sealed class ConditionMethodChildElementMissingException()
        : HLinqQueryException( "Condition is method call condition but method child does not exits");
}
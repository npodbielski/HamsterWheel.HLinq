using HamsterWheel.HLinq.Exceptions;

namespace HamsterWheel.HLinq.Tree.Filtering;

partial class AggregateMethodToExpressionConverter
{
    public sealed class NotAnAggregateException(string name, IEnumerable<string> aggregates) : HLinqQueryException(
        $"'{name}' is not an aggregate over a group. Aggregates a query may name are: " +
        $"'{string.Join("', '", aggregates)}'.");

    public sealed class AggregateArgumentsException(string name, string reason) : HLinqQueryException(
        $"Invalid arguments of the aggregate '{name}'. {reason}");

    public sealed class AggregateNameCollisionException(string name)
        : HLinqQueryException(
            $"The answer already names '{name}' - a group answers Key and Rows beside its aggregates, so an " +
            "aggregate cannot take that name; give it another, i.e. C=x.Count().");
}
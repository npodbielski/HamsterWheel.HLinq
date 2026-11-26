using HamsterWheel.HLinq.Diagnostics;
using HamsterWheel.HLinq.Exceptions;

namespace HamsterWheel.HLinq.Tree.Filtering;

partial class AggregateMethodToExpressionConverter
{
    public sealed class NotAnAggregateException(string name, IReadOnlyList<string> aggregates) : HLinqQueryException(
        $"'{name}' is not an aggregate over a group. Aggregates a query may name are: " +
        $"'{string.Join("', '", aggregates)}'.")
    {
        public override HLinqErrorCode Code => HLinqErrorCode.NotAnAggregate;
        public override string InvalidQueryChunk => name;
        public override IReadOnlyList<string> Examples => aggregates;
    }

    public sealed class AggregateArgumentsException(string name, string reason) : HLinqQueryException(
        $"Invalid arguments of the aggregate '{name}'. {reason}")
    {
        public override HLinqErrorCode Code => HLinqErrorCode.InvalidMethodParameters;
        public override string InvalidQueryChunk => name;
    }
}

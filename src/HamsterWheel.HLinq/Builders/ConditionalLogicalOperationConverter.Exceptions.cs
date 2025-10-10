using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Filtering;
using HamsterWheel.HLinq.Tree.Filtering;

namespace HamsterWheel.HLinq.Builders;

public sealed class ConditionalLogicalOperationTokenCannotBeFirstException()
    : HLinqQueryException(
        $"{nameof(ILogicalOperatorToken)} (i.e. {nameof(And)} or {nameof(Or)}) need to be provided in any {nameof(Condition)} or {nameof(ConditionGroup)} beside the first one");

public sealed class InvalidConditionalLogicalOperationException(ILogicalOperatorToken logicalOpToken) :
    HLinqQueryException($"{logicalOpToken} is not supported!");
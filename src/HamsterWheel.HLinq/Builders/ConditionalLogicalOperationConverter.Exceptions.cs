using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Filter;
using HamsterWheel.HLinq.Tree.Filter;

namespace HamsterWheel.HLinq.Builders;

public sealed class ConditionalLogicalOperationTokenCannotBeFirstException()
    : HLinqQueryException(
        $"{nameof(IConditionalLogicalOperationToken)} (i.e. {nameof(And)} or {nameof(Or)}) need to be provided in any {nameof(Condition)} or {nameof(ConditionGroup)} beside the first one");

public sealed class InvalidConditionalLogicalOperationException(IConditionalLogicalOperationToken operationToken) :
    HLinqQueryException($"{operationToken} is not supported!");
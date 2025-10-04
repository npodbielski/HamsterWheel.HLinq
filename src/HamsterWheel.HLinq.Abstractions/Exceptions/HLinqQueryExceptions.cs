using HamsterWheel.HLinq.Appliers;

namespace HamsterWheel.HLinq.Exceptions;

public abstract class HLinqQueryException(string message) : Exception(message);

public sealed class InvalidTypeOfTreeRoot<TExpected>(object actual) : HLinqQueryException(
    $"Tree root: '{actual}' is of type: '{actual.GetType()}' but type of {typeof(TExpected)} was expected.");

public sealed class HLinqQueryQueryApplierNullException(IHLinqQuery query)
    : HLinqQueryException($"{nameof(IHLinqQuery)} instance does not have {nameof(IHLinqQueryApplier)} assigned to it. If you created this instance manually, use {nameof(IHLinqQueryApplier)}.{nameof(IHLinqQueryApplier.Apply)} instead.");
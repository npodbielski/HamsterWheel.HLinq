using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Pipeline.Applier;

namespace HamsterWheel.HLinq.Request;

partial class HLinqQuery<T>
{
    public sealed class HLinqQueryQueryApplierNullException()
        : HLinqQueryException(
            $"{nameof(IHLinqQuery)} instance does not have {nameof(IHLinqQueryApplier)} assigned to it. If you created this instance manually, use {nameof(IHLinqQueryApplier)}.{nameof(IHLinqQueryApplier.Apply)} instead.");

    public class InvalidApplierResultException() : HLinqQueryException(
        $"'{nameof(IHLinqQueryApplier)}' returned '{nameof(IResult)}' with '{nameof(IResult.Data)}' and '{nameof(IResult.Count)}' null");
}
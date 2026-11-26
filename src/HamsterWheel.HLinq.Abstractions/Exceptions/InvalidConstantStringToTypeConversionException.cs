using HamsterWheel.HLinq.Diagnostics;

namespace HamsterWheel.HLinq.Exceptions;

public class InvalidConstantStringToTypeConversionException(object? value, Type destination)
    : HLinqQueryException($"Constant value of '{value ?? "null"}' can not be converted to type '{destination}'")
{
    public override HLinqErrorCode Code => HLinqErrorCode.ImpossibleConversion;
    public override string InvalidQueryChunk => value?.ToString() ?? "null";
    public override IReadOnlyList<string> Examples { get; } = [destination.ToString()];
};

public sealed class InvalidConstantStringToTypeConversionException<TTarget>(string value) :
    InvalidConstantStringToTypeConversionException(value, typeof(TTarget));
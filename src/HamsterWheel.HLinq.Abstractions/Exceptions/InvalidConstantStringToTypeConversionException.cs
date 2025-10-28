namespace HamsterWheel.HLinq.Exceptions;

public class InvalidConstantStringToTypeConversionException(object? value, Type destination)
    : HLinqQueryException($"Constant value of '{value ?? "null"}' can not be converted to type '{destination}'");

public sealed class InvalidConstantStringToTypeConversionException<TTarget>(string value) :
    InvalidConstantStringToTypeConversionException(value, typeof(TTarget));
namespace HamsterWheel.HLinq.Exceptions;

public sealed class InvalidConstantStringToTypeConversionException(string value, Type destination)
    : HLinqQueryException($"Constant value of '{value}' can not be converted to type '{destination}'");
namespace HamsterWheel.HLinq.Exceptions;

public sealed class UnknownCultureException(string culture)
    : HLinqQueryException($"'{culture}' is not a culture this runtime knows a value to read in.");

using HamsterWheel.HLinq.Diagnostics;

namespace HamsterWheel.HLinq.Exceptions;

public sealed class UnknownCultureException(string culture)
    : HLinqQueryException($"'{culture}' is not a culture this runtime knows a value to read in.")
{
    public override HLinqErrorCode Code => HLinqErrorCode.UnknownCulture;
    public override string InvalidQueryChunk => culture;
}

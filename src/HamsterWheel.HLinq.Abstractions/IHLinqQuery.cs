using HamsterWheel.HLinq.Diagnostics;
using HamsterWheel.HLinq.Pipeline.Parser;

namespace HamsterWheel.HLinq;

public interface IHLinqQuery : ITreeBranch
{
    string SourceQueryString { get; }

    /// <summary>
    /// Query warnings
    /// </summary>
    IHLinqWarnings Warnings { get; set; }

    /// <summary>
    /// User requested culture to be applied to the query
    /// </summary>
    string? Culture { get; }
}

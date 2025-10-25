using HamsterWheel.HLinq.Pipeline.Parser;

namespace HamsterWheel.HLinq;

public interface IHLinqQuery : ITreeBranch
{
    string SourceQueryString { get; }
}
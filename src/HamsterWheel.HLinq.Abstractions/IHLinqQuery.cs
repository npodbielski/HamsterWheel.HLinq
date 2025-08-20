using HamsterWheel.HLinq.Parsers;

namespace HamsterWheel.HLinq;

public interface IHLinqQuery : ITreeBranch
{
    string SourceQueryString { get; }
}
using HamsterWheel.HLinq.Parsers;

namespace HamsterWheel.HLinq.Appliers;

public interface IApplierFactory
{
    IApplier Get<T>(T root) where T : ITreeBranch;
}
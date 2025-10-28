using HamsterWheel.HLinq.Pipeline.Parser;

namespace HamsterWheel.HLinq.Pipeline.Applier;

public interface IElementApplierFactory
{
    IElementApplier Get<T>(T root) where T : ITreeBranch;
}
using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Pipeline.Parser;

namespace HamsterWheel.HLinq.Pipeline.Applier;

public sealed class ApplierFactory(IEnumerable<IElementApplier> appliers) : IElementApplierFactory
{
    public IElementApplier Get<T>(T root) where T : ITreeBranch
    {
        foreach (var applier in appliers)
        {
            if (applier.CanApply(root))
            {
                return applier;
            }
        }

        throw new MissingApplierException(root);
    }

    private sealed class MissingApplierException(ITreeBranch root)
        : HLinqQueryException($"No applier for {root.GetType()} was found");
}
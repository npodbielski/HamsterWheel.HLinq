using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Parsers;

namespace HamsterWheel.HLinq.Appliers;

public sealed class ApplierFactory(IEnumerable<IApplier> appliers) : IApplierFactory
{
    public IApplier Get<T>(T root) where T : ITreeBranch
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
using HamsterWheel.HLinq.Appliers;
using HamsterWheel.HLinq.Parsers;

namespace HamsterWheel.HLinq.Applier;

public sealed class ApplierFactory(IEnumerable<IApplier> appliers) : IApplierFactory
{
    public IApplier Get<T>(T root) where T : ITreeBranch
    {
        foreach (var applier in appliers)
            if (applier.CanApply(root))
                return applier;

        throw new InvalidOperationException($"No applier for {root.GetType()} was found");
    }
}
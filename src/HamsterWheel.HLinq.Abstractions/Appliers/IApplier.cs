using HamsterWheel.HLinq.Parsers;

namespace HamsterWheel.HLinq.Appliers;

public interface IApplier
{
    bool CanApply(ITreeBranch root);

    IQueryableContext Apply(IQueryableContext context, ITreeBranch root, string hLinqQuery,
        CancellationToken cancellationToken = default);
}
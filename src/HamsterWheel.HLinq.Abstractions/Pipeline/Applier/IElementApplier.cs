using HamsterWheel.HLinq.Pipeline.Parser;

namespace HamsterWheel.HLinq.Pipeline.Applier;

public interface IElementApplier
{
    bool CanApply(ITreeBranch root);

    IQueryableContext Apply(IQueryableContext context, ITreeBranch root, string hLinqQuery,
        CancellationToken cancellationToken = default);
}
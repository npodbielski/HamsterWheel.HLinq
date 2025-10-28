using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Pipeline.Parser;

namespace HamsterWheel.HLinq.Pipeline.Applier;

public abstract class RootApplierBase<TRoot> : IElementApplier where TRoot : class, ITreeBranch
{
    public bool CanApply(ITreeBranch root) => root is TRoot;

    public IQueryableContext Apply(IQueryableContext context, ITreeBranch root, string hLinqQuery,
        CancellationToken cancellationToken = default)
    {
        var targetRoot = root as TRoot ?? throw new InvalidTypeOfTreeRoot<TRoot>(root);

        cancellationToken.ThrowIfCancellationRequested();

        return ApplyImpl(context, targetRoot, hLinqQuery);
    }

    protected abstract IQueryableContext ApplyImpl(IQueryableContext context, TRoot root, string hLinqQuery);
}
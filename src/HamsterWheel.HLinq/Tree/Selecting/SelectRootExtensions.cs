namespace HamsterWheel.HLinq.Tree.Selecting;

/// <summary>
/// The members a select reads through the interface, so no applier has to know the concrete tree.
/// </summary>
public static class SelectRootExtensions
{
    extension(ISelectRoot select)
    {
        public IReadOnlyList<PropertyAssignment> MemberSources => [.. select.Children.OfType<PropertyAssignment>()];

        public IReadOnlyList<PropertyAssignment> AggregateAssignments =>
            [.. select.MemberSources.Where(a => a.EntityAggregate is not null)];

        public IReadOnlyList<PropertyAssignment> RowMemberSources =>
            [.. select.MemberSources.Where(a => a.EntityAggregate is null)];

        public bool NeedsObjectInitializer => select.MemberSources.Count > 1 ||
                                              select.MemberSources.Any(c => c.NeedsObjectInitializer);
    }
}
namespace HamsterWheel.HLinq.Tree.Selecting;

/// <summary>
/// The members a select reads through the interface, so no applier has to know the concrete tree.
/// </summary>
public static class SelectRootExtensions
{
    extension(SelectRoot select)
    {
        public IReadOnlyList<PropertyAssignment> MemberSources => [.. select.Children.OfType<PropertyAssignment>()];

        /// <summary>
        /// The members that are an aggregate of the group, empty before a grouping was committed.
        /// </summary>
        public IReadOnlyList<PropertyAssignment> AggregateAssignments =>
            select.StandsOnGroups
                ? [.. select.MemberSources.Where(a => a.GroupAggregate is not null)]
                : [];

        public IReadOnlyList<PropertyAssignment> RowMemberSources =>
            select.StandsOnGroups
                ? [.. select.MemberSources.Where(a => a.GroupAggregate is null)]
                : [.. select.MemberSources];

        public bool NeedsObjectInitializer => select.MemberSources.Count > 1 ||
                                              select.MemberSources.Any(c => c.NeedsObjectInitializer);
    }
}
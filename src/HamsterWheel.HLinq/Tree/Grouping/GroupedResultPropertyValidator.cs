using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Pipeline.Applier;
using HamsterWheel.HLinq.Pipeline.Parser;

namespace HamsterWheel.HLinq.Tree.Grouping;

/// <summary>
/// What a clause written after a <c>groupBy</c> may name. Only the key of the group orders or filters
/// groups - the provider refuses a condition or an ordering over the rows of a group - so a clause
/// naming <c>Rows</c> there is refused here, with the reason, rather than leaking the provider's
/// failure to the caller.
/// </summary>
internal static class GroupedResultPropertyValidator
{
    public static void CheckIfPropertyAvailable(ITreeBranch root, string hLinqQuery)
    {
        foreach (var property in root.Properties())
        {
            var path = property.GetPath(hLinqQuery);
            if (path.Length > 0
                && string.Equals(path[0], GroupingState.RowsName, StringComparison.OrdinalIgnoreCase))
            {
                throw new RowsCannotBeOrderedOrFilteredException();
            }
        }
    }
}

using HamsterWheel.HLinq.Pipeline.Parser;
using HamsterWheel.HLinq.Tree.Filtering;

namespace HamsterWheel.HLinq.Tree.Selecting;

public interface IPropertyAssignment : ITreeElement
{
    bool NeedsObjectInitializer { get; }
    IProperty? NestedSelectProperty { get; }
    IInnerProperty? NestedSelectInnerProperty { get; }

    /// <summary>
    /// The call standing on the entity of the query itself - after a grouping, the aggregate of the group.
    /// </summary>
    IMethod? GroupAggregate { get; }

    INestedSelect? NestedSelect { get; }
    ITreeElement[] Children { get; }

    /// <summary>
    /// The name the query wrote for this member, null when it wrote none.
    /// </summary>
    string? ExplicitPropertyName(string hLinqQuery);

    /// <summary>
    /// The name a call answers under: the one the query wrote, or the method's own.
    /// </summary>
    string? FinalPropertyName(string hLinqQuery);
}
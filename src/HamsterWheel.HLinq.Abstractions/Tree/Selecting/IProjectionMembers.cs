using HamsterWheel.HLinq.Pipeline.Parser;

namespace HamsterWheel.HLinq.Tree.Selecting;

/// <summary>
/// A clause that names members of the row it reads - <c>select</c> and <c>groupBy</c> - so one
/// builder makes the expression of both.
/// </summary>
public interface IProjectionMembers : ITreeBranch
{
    /// <summary>
    /// The assignments whose bindings make the shape, in the order the query wrote them.
    /// </summary>
    IReadOnlyList<ITreeElement> MemberSources { get; }

    /// <summary>
    /// Whether the shape needs an object initializer rather than being one member as it is.
    /// </summary>
    bool NeedsObjectInitializer { get; }
}

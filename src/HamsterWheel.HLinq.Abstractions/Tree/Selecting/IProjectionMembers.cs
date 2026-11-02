using HamsterWheel.HLinq.Pipeline.Parser;

namespace HamsterWheel.HLinq.Tree.Selecting;

/// <summary>
/// A clause that names members of the row it reads - <c>select</c> and <c>groupBy</c> - so one
/// builder makes the expression of both.
/// </summary>
public interface IProjectionMembers : ITreeBranch;

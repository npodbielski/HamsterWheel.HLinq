namespace HamsterWheel.HLinq.Parsers;

public interface ITreeBranch : ITreeElement
{
    public ITreeElement[] Children { get; }
}
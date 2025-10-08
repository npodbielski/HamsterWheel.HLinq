using HamsterWheel.HLinq.Tokens;

namespace HamsterWheel.HLinq.Parsers;

public interface ITreeElement
{
    IToken[] Tokens { get; }
    public bool Finished { get; }
    bool NoChildren { get; }
    public IEnumerable<T> GetAll<T>() where T : ITreeElement;
}
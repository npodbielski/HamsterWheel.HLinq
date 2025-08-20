namespace HamsterWheel.HLinq.Parsers;

public interface IGrowingElementContext
{
    ITreeElement Element { get; }
    List<ITreeElement> Children { get; }
}
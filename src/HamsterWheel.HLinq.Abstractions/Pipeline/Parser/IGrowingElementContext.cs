namespace HamsterWheel.HLinq.Pipeline.Parser;

public interface IGrowingElementContext
{
    ITreeElement Element { get; }
    List<ITreeElement> Children { get; }
}
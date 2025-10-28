namespace HamsterWheel.HLinq.Pipeline.Parser;

public sealed record GrowingElementContext(ITreeElement Element, List<ITreeElement> Children) : IGrowingElementContext;
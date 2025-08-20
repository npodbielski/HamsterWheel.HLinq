namespace HamsterWheel.HLinq.Parsers;

public sealed record GrowingElementContext(ITreeElement Element, List<ITreeElement> Children) : IGrowingElementContext;
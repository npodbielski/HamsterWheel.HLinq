using HamsterWheel.HLinq.Tokens;

namespace HamsterWheel.HLinq.Pipeline.Parser;

public interface IElementParser
{
    IToken[] ExampleTokens { get; }
    bool IsRoot { get; }

    /// <summary>
    /// Which elements this one is parsed as a child of; empty for a root.
    /// </summary>
    Type[] ValidParents { get; }
    bool ChildOf<T>(T branch) where T : ITreeElement;
    Type ForElement();
    bool TryBuildElement(IParsingContext context);
    void Finish(IParsingContext context);
}

using HamsterWheel.HLinq.Tokens;

namespace HamsterWheel.HLinq.Parsers;

public interface IElementParser
{
    IToken[] ExampleTokens { get; }
    bool IsRoot { get; }
    bool ChildOf<T>(T branch) where T : ITreeElement;

    Type ForElement();

    bool TryBuildElement(IParsingContext context);
    void Finish(IParsingContext context);
}

namespace HamsterWheel.HLinq.Parsers;

public interface IElementParser
{
    bool IsRoot { get; }
    bool ChildOf<T>(T branch) where T : ITreeElement;

    Type ForElement();

    bool TryBuildElement(IParsingContext context);
    void Finish(IParsingContext context);
}

//TODO: should this also have Analyzers? to validate whole query for errors? i.e.
//-if all properties names and paths are valid? 
//-if all methods names and method parameters are valid?
//-does all constant values for comparison are comparable and/or convertible to props types? i.e. person.DateOfBirth=12312 is most probably invalid
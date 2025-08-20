using HamsterWheel.HLinq.Tokens;

namespace HamsterWheel.HLinq.Parsers;

public interface IHLinqParsersCollection
{
    //TODO: it would be good to disable extensions parsers that throws errors very often
    IElementParser[] Parsers { get; }
    IHLinqTokenPossibility[] Tokens { get; }

    //TODO: allow to extend Hlinq this way
    void AddExtensionFromAssemblyWith<T>(string nameOfExtensions);
    void Reset();
}
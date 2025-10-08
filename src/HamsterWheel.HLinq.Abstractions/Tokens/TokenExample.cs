using HamsterWheel.HLinq.Exceptions;

namespace HamsterWheel.HLinq.Tokens;

/// <summary>
/// Allow for providing an example of a token in <see cref="InvalidTokenCollectionException"/>
/// </summary>
/// <param name="example">Token string value example</param>
public class TokenExample(string example) : IToken
{
    public Range Range => default;

    public string GetValue(string str) => example;
}
namespace HamsterWheel.HLinq.Tokens;

public class TokenExample(string example) : IToken
{
    public Range Range => default;

    public string GetValue(string str) => example;
}
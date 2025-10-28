namespace HamsterWheel.HLinq.Tokens;

public interface IToken
{
    Range Range { get; }
    string GetValue(string str);
}
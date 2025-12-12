namespace HamsterWheel.HLinq.Tokens;

partial class TokenBase
{
    public string GetValue(string str) => str[Range.Start.Value..Range.End.Value];
}
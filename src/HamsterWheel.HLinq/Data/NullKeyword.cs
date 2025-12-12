namespace HamsterWheel.HLinq.Data;

public class NullKeyword : INullKeyword
{
    public const string Keyword = "null";
    public string Value => Keyword;
}
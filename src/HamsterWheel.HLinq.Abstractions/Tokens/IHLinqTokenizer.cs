namespace HamsterWheel.HLinq.Tokens;

public interface IHLinqTokenizer
{
    public IToken[] Tokenize(string hLinqQuery);
}
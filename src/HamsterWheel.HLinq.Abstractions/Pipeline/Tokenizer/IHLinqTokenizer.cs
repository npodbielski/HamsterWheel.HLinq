using HamsterWheel.HLinq.Tokens;

namespace HamsterWheel.HLinq.Pipeline.Tokenizer;

public interface IHLinqTokenizer
{
    public IToken[] Tokenize(string hLinqQuery);
}
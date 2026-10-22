using System.Globalization;
using HamsterWheel.HLinq.Tokens;

namespace HamsterWheel.HLinq.Pipeline.Tokenizer;

public interface IHLinqTokenizer
{
    /// <summary>
    /// Processes a query into a sequence of tokens according to <see cref="IGrammar"/> rules. 
    /// </summary>
    IToken[] Tokenize(string hLinqQuery, CultureInfo? culture = null);
}
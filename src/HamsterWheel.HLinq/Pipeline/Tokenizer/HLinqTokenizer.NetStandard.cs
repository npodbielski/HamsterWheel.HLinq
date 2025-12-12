using System.Runtime.CompilerServices;

namespace HamsterWheel.HLinq.Pipeline.Tokenizer;

partial class HLinqTokenizer
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Range GetRange(System.Range range) => 
        new(new Index(range.Start.Value, range.Start.IsFromEnd), new Index(range.End.Value, range.End.IsFromEnd));
}
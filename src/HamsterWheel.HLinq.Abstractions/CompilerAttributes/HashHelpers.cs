#if NETSTANDARD2_0
using System.Diagnostics.CodeAnalysis;

namespace System;

//"This is added just to enable range syntax for .net standard version of the library and this code is not actually used."
[ExcludeFromCodeCoverage]
internal static class HashHelpers
{
    public static readonly int RandomSeed = Guid.NewGuid().GetHashCode();

    public static int Combine(int h1, int h2)
    {
        unchecked
        {
            var rol5 = ((uint)h1 << 5) | ((uint)h1 >> 27);
            return ((int)rol5 + h1) ^ h2;
        }
    }
}
#endif
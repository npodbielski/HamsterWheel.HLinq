using System.Runtime.CompilerServices;
using static System.Enum;

namespace HamsterWheel.HLinq;

partial class StringExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static (bool convertible, object? enumValue) TryParseEnum(this string str, Type type) =>
        (TryParse(type, str, true, out var value), value);
}
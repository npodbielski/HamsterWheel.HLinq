using System.Runtime.CompilerServices;

namespace HamsterWheel.HLinq;

partial class StringExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static (bool convertible, object? enumValue) TryParseEnum(this string str, Type type) =>
        (Enum.TryParse(type, str, true, out var value), value);
}
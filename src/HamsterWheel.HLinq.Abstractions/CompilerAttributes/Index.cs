#if NETSTANDARD2_0
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System;

//"This is added just to enable range syntax for .net standard version of the library and this code is not actually used."
[ExcludeFromCodeCoverage]
public readonly struct Index : IEquatable<Index>
{
    private readonly int _value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Index(int value, bool fromEnd = false)
    {
        if (value < 0)
        {
            ThrowValueArgumentOutOfRange_NeedNonNegNumException();
        }

        if (fromEnd)
            _value = ~value;
        else
            _value = value;
    }

    private Index(int value)
    {
        _value = value;
    }

    public static Index Start => new Index(0);

    public static Index End => new Index(~0);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Index FromStart(int value)
    {
        if (value < 0)
        {
            ThrowValueArgumentOutOfRange_NeedNonNegNumException();
        }

        return new Index(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Index FromEnd(int value)
    {
        if (value < 0)
        {
            ThrowValueArgumentOutOfRange_NeedNonNegNumException();
        }

        return new Index(~value);
    }

    public int Value
    {
        get
        {
            if (_value < 0)
                return ~_value;
            else
                return _value;
        }
    }

    public bool IsFromEnd => _value < 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetOffset(int length)
    {
        var offset = _value;
        if (IsFromEnd)
        {
            offset += length + 1;
        }

        return offset;
    }

    public override bool Equals([NotNullWhen(true)] object? value) => value is Index index && _value == index._value;

    public bool Equals(Index other) => _value == other._value;

    public override int GetHashCode() => _value;

    public static implicit operator Index(int value) => FromStart(value);

    public override string ToString() => IsFromEnd ? ToStringFromEnd() : ((uint)Value).ToString();

    private static void ThrowValueArgumentOutOfRange_NeedNonNegNumException() =>
        throw new ArgumentOutOfRangeException("value", "value must be non-negative");

    private string ToStringFromEnd()
    {
#if (!NETSTANDARD2_0 && !NETFRAMEWORK)
            Span<char> span = stackalloc char[11];
            var formatted = ((uint)Value).TryFormat(span.Slice(1), out var charsWritten);
            Debug.Assert(formatted);
            span[0] = '^';
            return new string(span.Slice(0, charsWritten + 1));
#else
        return '^' + Value.ToString();
#endif
    }
}

#endif
using System.Globalization;
using System.Text.Json;
using HamsterWheel.HLinq.Data;
using HamsterWheel.HLinq.Data.Converters;

namespace HamsterWheel.HLinq.UnitTests.Data.Converter;

public partial class DefaultConverterUnitTests
{
    private readonly DefaultConverter _sut = GetSut();

    private static DefaultConverter GetSut(IFallbackConverter? converter = null) =>
        new(DefaultConverter.GetDefaultConverters(), new NullKeyword(), converter);
}

public class TestsFallbackConverter : FallbackConverterBase
{
    public bool Called { get; set; }

    public override (bool success, object? result) TryConvert(object? value, Type to,
        CultureInfo? culture = null)
    {
        Called = true;
        return (true, JsonSerializer.Deserialize(value!.ToString()!, to));
    }
}
//an int conversion of a host, ahead of the engine's own reading of an int
public sealed class ShiftedIntConverter : IConfigurableValueConverter
{
    public const int Shift = 1979;

    public int Priority => 999;

    public (bool success, object? result) TryConvert(object? value, Type to, CultureInfo? culture = null)
    {
        if (to != typeof(int) || value is not string)
        {
            return (false, null);
        }

        return (true, Shift);
    }
}

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
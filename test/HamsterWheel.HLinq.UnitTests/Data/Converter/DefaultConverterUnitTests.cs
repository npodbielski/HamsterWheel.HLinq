using System.Text.Json;
using HamsterWheel.HLinq.Data;
using HamsterWheel.HLinq.Data.Converters;

namespace HamsterWheel.HLinq.UnitTests.Data.Converter;

public partial class DefaultConverterUnitTests
{
    private readonly DefaultConverter _sut = GetSut();

    private static DefaultConverter GetSut(IFallbackConverter? converter = null) => new(converter);
}

public class FallbackConverter : IFallbackConverter
{
    public bool Called { get; set; }

    public (bool, object?) ConvertTo(Type targetType, object? value)
    {
        Called = true;
        return (true, JsonSerializer.Deserialize(value!.ToString()!, targetType));
    }
}
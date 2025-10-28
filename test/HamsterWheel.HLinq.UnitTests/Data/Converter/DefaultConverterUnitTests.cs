using System.Text.Json;
using HamsterWheel.HLinq.Data;
using HamsterWheel.HLinq.Data.Converters;

namespace HamsterWheel.HLinq.UnitTests.Data.Converter;

public partial class DefaultConverterUnitTests
{
    private readonly DefaultConverter _sut = GetSut();

    private static DefaultConverter GetSut(IFallbackConverter? converter = null) =>
        new(
        [
            new FromStringConverter(new NullKeyword()), new FromFormattableConverter(), new EnumValueConverter(),
            new NullableEnumValueConverter(new NullKeyword(), new EnumValueConverter()), new FromConvertibleConverter(),
            new ToInterfaceConverter(), new ViaSerializationConverter()
        ], new NullKeyword(), converter);
}

public class FallbackConverter : FallbackConverterBase
{
    public bool Called { get; set; }

    public override object? ConvertTo(object? value, Type to)
    {
        Called = true;
        return JsonSerializer.Deserialize(value!.ToString()!, to);
    }
}
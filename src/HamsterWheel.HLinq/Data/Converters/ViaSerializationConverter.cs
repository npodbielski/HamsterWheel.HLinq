using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HamsterWheel.HLinq.Data.Converters;

public class ViaSerializationConverter : IConfigurableValueConverter
{
    public int Priority => 2000;

    private static JsonSerializerOptions DefaultJsonSerializerOptions { get; } = new(JsonSerializerDefaults.Web)
    {
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        Converters = { new JsonStringEnumConverter() }
    };

    public (bool success, object? result) TryConvert(object? value, Type targetType,
        CultureInfo? culture = null)
    {
        //last attempt to serialize value: via JSON
        var jsonString = value as string ?? JsonSerializer.Serialize(value, DefaultJsonSerializerOptions);

        return (true, JsonSerializer.Deserialize(jsonString, targetType, DefaultJsonSerializerOptions));
    }
}
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

    public bool CanConvert(object? value, Type to) => true;

    public object? ConvertTo(object? value, Type targetType)
    {
        string jsonString;
        if (value is string str)
        {
            jsonString = str;
        }
        else
        {
            jsonString = JsonSerializer.Serialize(value, DefaultJsonSerializerOptions);
        }

        return JsonSerializer.Deserialize(jsonString, targetType, DefaultJsonSerializerOptions);
    }
}
using System.Text.Json;

namespace HamsterWheel.HLinq.Data.Converters;

public class ViaSerializationConverter : IConfigurableValueConverter
{
    public int Priority => 2000;

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
            jsonString = JsonSerializer.Serialize(value);
        }

        return JsonSerializer.Deserialize(jsonString, targetType);
    }
}
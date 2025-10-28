namespace HamsterWheel.HLinq.Data.Converters;

public class ToInterfaceConverter : IConfigurableValueConverter
{
    public int Priority => 1004;

    public bool CanConvert(object? value, Type to) =>
        to.IsInterface && value?.GetType().GetInterface(to.Name) is not null;

    public object? ConvertTo(object? value, Type to) =>
        //if object implements interface, just return it
        value;
}
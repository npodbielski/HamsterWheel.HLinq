using System.Globalization;
using System.Xml;
using HamsterWheel.HLinq.Exceptions;

namespace HamsterWheel.HLinq.Data.Converters;

/// <summary>
/// Reads an ISO-8601 duration ("P1DT2H3M4S") as a TimeSpan.
/// </summary>
public class Iso8601TimeSpanConverter : IConfigurableValueConverter
{
    //before FromStringConverter, whose TimeSpan parse would refuse an ISO duration;
    //texts without a P prefix pass through untouched
    public int Priority => 900;

    public (bool success, object? result) TryConvert(object? value, Type to, CultureInfo? culture = null)
    {
        if (value is not string text
            || to != typeof(TimeSpan) && to != typeof(TimeSpan?))
        {
            return (false, null);
        }

        if (!IsIsoDuration(text))
        {
            return (false, null);
        }

        try
        {
            return (true, XmlConvert.ToTimeSpan(text));
        }
        catch (FormatException)
        {
            throw new InvalidConstantStringToTypeConversionException(value, to);
        }
    }

    //an ISO-8601 duration starts with P and carries at least one time-or-day designator
    private static bool IsIsoDuration(string text) =>
        text.Length > 1
        && (text[0] == 'P' || text[0] == 'p')
        && text.AsSpan(1).IndexOfAny("DHMSdhms".ToCharArray()) >= 0;
}

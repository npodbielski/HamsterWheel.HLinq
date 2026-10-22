using System.Globalization;

namespace HamsterWheel.HLinq.Data.Converters;

public class FromFormattableConverter : IConfigurableValueConverter
{
    public int Priority => 1001;

    private const string Iso8601 = "yyyy-MM-ddTHH:mm:ss.FFFFFFF";

    private static readonly Dictionary<Type, Func<IFormattable, object>> FormattableMethods =
        new()
        {
            { typeof(DateTime), c => DateTimeText((DateTime)c) },
            { typeof(DateTimeOffset), c => ((DateTimeOffset)c).ToString(Iso8601 + "zzz", CultureInfo.InvariantCulture) }
        };

    public (bool success, object? result) TryConvert(object? value, Type to, CultureInfo? culture = null) =>
        to == typeof(string)
        && value is IFormattable formattable
        && FormattableMethods.TryGetValue(value.GetType(), out var format)
            ? (true, format(formattable))
            : (false, null);

    private static string DateTimeText(DateTime value) =>
        value.Kind switch
        {
            DateTimeKind.Utc => value.ToString(Iso8601 + "'Z'", CultureInfo.InvariantCulture),
            DateTimeKind.Local => new DateTimeOffset(value)
                .ToString(Iso8601 + "zzz", CultureInfo.InvariantCulture),
            _ => value.ToString(Iso8601, CultureInfo.InvariantCulture)
        };
}

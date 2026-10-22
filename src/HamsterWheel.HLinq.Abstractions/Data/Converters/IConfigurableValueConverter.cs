using System.Globalization;

namespace HamsterWheel.HLinq.Data.Converters;

public interface IConfigurableValueConverter
{
    int Priority { get; }

    /// <summary>
    /// Reads a value into <paramref name="to" />, or hands the pair to the next converter.
    /// </summary>
    /// <returns>
    /// <c>(true, value)</c>, or <c>(false, null)</c> when this converter does not read that value into
    /// that type - which is what sends the pair on to the converter of the next
    /// <see cref="Priority" />.
    /// </returns>
    /// <param name="to">Destination type of conversion</param>
    /// <param name="culture">
    /// The culture the text of the value is read in, the invariant one when it is <c>null</c> - which
    /// is what a request that named no culture asks for. A converter that turns text into a number, a
    /// date or a time reads with it; one that moves a value between types without reading text has no
    /// use for it.
    /// </param>
    /// <param name="value">Value to convert</param>
    /// <exception cref="HamsterWheel.HLinq.Exceptions.InvalidConstantStringToTypeConversionException">
    /// Thrown when the pair is this converter's to read and the value cannot be read into it - which is
    /// the query's error, and not a reason to ask a converter that reads text by other rules.
    /// </exception>
    (bool success, object? result) TryConvert(object? value, Type to, CultureInfo? culture = null);
}

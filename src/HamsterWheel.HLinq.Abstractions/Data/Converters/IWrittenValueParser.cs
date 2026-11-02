using System.Globalization;

namespace HamsterWheel.HLinq.Data.Converters;

/// <summary>
/// Checks if text can be parsed as a specified type in a specified culture
/// </summary>
public interface IWrittenValueParser
{
    bool Parses(string text, Type destination, CultureInfo? culture = null);
}

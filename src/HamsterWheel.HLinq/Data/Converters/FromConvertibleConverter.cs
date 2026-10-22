using System.Globalization;

namespace HamsterWheel.HLinq.Data.Converters;

public class FromConvertibleConverter : IConfigurableValueConverter
{
    public int Priority => 1002;

    private static readonly Dictionary<Type, Func<IConvertible, object>> ConvertibleMethods =
        new()
        {
            {
                typeof(int), c =>
                {
                    try
                    {
                        return c.ToInt32(CultureInfo.InvariantCulture);
                    }
                    catch (Exception)
                    {
                        switch (c.ToDouble(CultureInfo.InvariantCulture))
                        {
                            case < int.MinValue:
                                return int.MinValue;
                            case > int.MaxValue:
                                return int.MaxValue;
                            default:
                                throw;
                        }
                    }
                }
            },
            { typeof(double), c => c.ToDouble(CultureInfo.InvariantCulture) },
            {
                typeof(decimal), c =>
                {
                    try
                    {
                        return c.ToDecimal(CultureInfo.InvariantCulture);
                    }
                    catch (OverflowException)
                    {
                        switch (c.ToDouble(CultureInfo.InvariantCulture))
                        {
                            case < (double)decimal.MinValue:
                                return decimal.MinValue;
                            case > (double)decimal.MaxValue:
                                return decimal.MaxValue;
                            default:
                                throw;
                        }
                    }
                }
            },
            { typeof(string), c => c.ToString(CultureInfo.InvariantCulture) },
            {
                typeof(byte), c =>
                {
                    try
                    {
                        return c.ToByte(CultureInfo.InvariantCulture);
                    }
                    catch (OverflowException)
                    {
                        switch (c.ToDouble(CultureInfo.InvariantCulture))
                        {
                            case < byte.MinValue:
                                return byte.MinValue;
                            case > byte.MaxValue:
                                return byte.MaxValue;
                            default:
                                throw;
                        }
                    }
                }
            },
            { typeof(char), c => c.ToChar(CultureInfo.InvariantCulture) },
            {
                typeof(short), c =>
                {
                    try
                    {
                        return c.ToInt16(CultureInfo.InvariantCulture);
                    }
                    catch (OverflowException)
                    {
                        switch (c.ToDouble(CultureInfo.InvariantCulture))
                        {
                            case < short.MinValue:
                                return short.MinValue;
                            case > short.MaxValue:
                                return short.MaxValue;
                            default:
                                throw;
                        }
                    }
                }
            },
            {
                typeof(long), c =>
                {
                    try
                    {
                        return c.ToInt64(CultureInfo.InvariantCulture);
                    }
                    catch (OverflowException)
                    {
                        switch (c.ToDouble(CultureInfo.InvariantCulture))
                        {
                            case < long.MinValue:
                                return long.MinValue;
                            case > long.MaxValue:
                                return long.MaxValue;
                            default:
                                throw;
                        }
                    }
                }
            },
            { typeof(DateTime), c => c.ToDateTime(CultureInfo.InvariantCulture) },
            {
                typeof(sbyte), c =>
                {
                    try
                    {
                        return c.ToSByte(CultureInfo.InvariantCulture);
                    }
                    catch (OverflowException)
                    {
                        switch (c.ToDouble(CultureInfo.InvariantCulture))
                        {
                            case < sbyte.MinValue:
                                return sbyte.MinValue;
                            case > sbyte.MaxValue:
                                return sbyte.MaxValue;
                            default:
                                throw;
                        }
                    }
                }
            },
            { typeof(ushort), c =>
                {
                    try
                    {
                        return c.ToUInt16(CultureInfo.InvariantCulture);
                    }
                    catch (OverflowException)
                    {
                        switch (c.ToDouble(CultureInfo.InvariantCulture))
                        {
                            case < ushort.MinValue:
                                return ushort.MinValue;
                            case > ushort.MaxValue:
                                return ushort.MaxValue;
                            default:
                                throw;
                        }
                    }
                }
            },
            { typeof(uint), c =>
                {
                    try
                    {
                        return c.ToUInt32(CultureInfo.InvariantCulture);
                    }
                    catch (OverflowException)
                    {
                        switch (c.ToDouble(CultureInfo.InvariantCulture))
                        {
                            case < uint.MinValue:
                                return uint.MinValue;
                            case > uint.MaxValue:
                                return uint.MaxValue;
                            default:
                                throw;
                        }
                    }
                }
            }
        };

    public (bool success, object? result) TryConvert(object? value, Type to, CultureInfo? culture = null) =>
        value is IConvertible convertible && ConvertibleMethods.TryGetValue(to, out var convert)
            ? (true, convert(convertible))
            : (false, null);
}
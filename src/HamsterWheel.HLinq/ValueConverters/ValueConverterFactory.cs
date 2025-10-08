namespace HamsterWheel.HLinq.ValueConverters;

public sealed class ValueConverterFactory : IValueConverterFactory
{
    private readonly IEnumerable<IConfigurableValueConverter> _configurableValueConverters;
    private readonly Dictionary<Type, IValueConverter> _dic;
    private readonly EnumValueConverter _enumValueConverter;
    private readonly NullableEnumValueConverter _nullableEnumValueConverter;

    public ValueConverterFactory(IEnumerable<IValueConverter> converters,
        IEnumerable<IConfigurableValueConverter> configurableValueConverters)
    {
        _configurableValueConverters = configurableValueConverters;
        var valueConverters = _configurableValueConverters as IConfigurableValueConverter[] ??
                              _configurableValueConverters.ToArray();

        _enumValueConverter = valueConverters.OfType<EnumValueConverter>().Single();
        _nullableEnumValueConverter = valueConverters.OfType<NullableEnumValueConverter>().Single();
        _dic = converters.ToDictionary(c => c.For(), c => c);
    }

    public IValueConverter GetConverterFor(Type type)
    {
        if (_dic.TryGetValue(type, out var converterFor))
        {
            return converterFor;
        }

        if (_enumValueConverter.CanConvert(type))
        {
            return new ConfigurableToPlainValueConverter(type, _enumValueConverter);
        }

        if (_nullableEnumValueConverter.CanConvert(type))
        {
            return new ConfigurableToPlainValueConverter(type, _nullableEnumValueConverter);
        }

        foreach (var configurableValueConverter in _configurableValueConverters)
        {
            if (configurableValueConverter.CanConvert(type))
            {
                return new ConfigurableToPlainValueConverter(type, configurableValueConverter);
            }
        }

        //TODO: improve this error message to include reason for Hlinq query trying to do this and why it does not work and how this could be fixed by the user
        // i.e. doing where[x.createdBy=userName] will throw here because there is no conversion between NamedReference and string
        throw new InvalidOperationException($"No converter for type {type} was found!");
    }
}
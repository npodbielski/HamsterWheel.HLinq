using System.Globalization;
using FluentAssertions;
using HamsterWheel.HLinq.Data.Converters;
using HamsterWheel.HLinq.Exceptions;

namespace HamsterWheel.HLinq.UnitTests.Data.ValueConverters;

public class BaseConfigurableValueConverterUnitTests
{
    [Fact]
    public void TryConvert_WhenTheConverterFailedOnAValueItTook_ThenNamesTheValueAndItsType()
    {
        //arrange
        var sut = new TestConfigurableValueConverter();
        var action = () => sut.TryConvert("test", typeof(int));

        //act && assert - a converter that took the pair reports the failure itself, instead of letting
        //the next converter in the chain answer with its own rules
        var actual = action.Should().Throw<InvalidConstantStringToTypeConversionException>();
        actual.WithMessage("* can not be converted to type *");
    }
}

public class TestConfigurableValueConverter : BaseConfigurableValueConverter
{
    public override int Priority => 100000;

    public override (bool success, object? result) TryConvert(object? value, Type to,
        CultureInfo? culture = null) =>
        throw CannotConvert(value, to);
}
using FluentAssertions;
using HamsterWheel.HLinq.Data;
using HamsterWheel.HLinq.Data.Converters;
using HamsterWheel.HLinq.Tree.Filtering;
using HamsterWheel.HLinq.UnitTests.TestUtils;
using Microsoft.Extensions.DependencyInjection;

namespace HamsterWheel.HLinq.UnitTests.Data.Converter;

partial class DefaultConverterUnitTests
{
    [Fact]
    public void GetDefaultConverters_WhenComparedWithTheContainer_ThenHandsOutTheSameConverters()
    {
        //arrange
        var fromContainer = new TestServicesCollection().Provider
            .GetServices<IConfigurableValueConverter>()
            .Select(c => c.GetType())
            .OrderBy(t => t.Name);

        //act
        var standardSet = DefaultConverter.GetDefaultConverters()
            .Select(c => c.GetType())
            .OrderBy(t => t.Name);

        //assert
        standardSet.Should().BeEquivalentTo(fromContainer);
    }

    [Fact]
    public void Instance_WhenHostConvertsAQuotedChar_ThenReadsItAsAFilterDoes()
    {
        //act
        var host = DefaultConverter.Instance.ConvertTo<char>("\"a\"");
        var filter = _sut.ConvertTo<char>("\"a\"");

        //assert
        host.Should().Be('a');
        host.Should().Be(filter);
    }

    [Fact]
    public void Instance_WhenHostConvertsAnEnumName_ThenReadsItAsAFilterDoes()
    {
        //act
        var host = DefaultConverter.Instance.ConvertTo<MethodSource>(nameof(MethodSource.Static));
        var filter = _sut.ConvertTo<MethodSource>(nameof(MethodSource.Static));

        //assert
        host.Should().Be(MethodSource.Static);
        host.Should().Be(filter);
    }

    [Fact]
    public void ComposedConverter_WhenHostAddsToTheStandardSet_ThenReadsWhatAFilterReads()
    {
        //arrange
        //so a converter the library adds to the defaults reaches the host without a recompile of its copy
        var composed = new DefaultConverter(DefaultConverter.GetDefaultConverters(), new NullKeyword());

        //act
        var host = composed.ConvertTo<TimeSpan>("PT5M");
        var filter = _sut.ConvertTo<TimeSpan>("PT5M");

        //assert
        host.Should().Be(TimeSpan.FromMinutes(5));
        host.Should().Be(filter);
    }
}

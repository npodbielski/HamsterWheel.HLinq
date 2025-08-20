using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using HamsterWheel.HLinq.Client;
using HamsterWheel.HLinq.Demo.Data;

namespace HamsterWheel.HLinq.IntegrationTests;

partial class MemoryDataTests
{
    [Fact]
    public async Task WhenEmptyWhere_ThenReturnsNotFilteredCollection()
    {
        //act
        var response = await fixture.Client.GetAsync("/demo/memory?where[]");

        //assert
        var data = await response.Content.ReadFromJsonAsync<Superhero[]>();
        data.Should().BeEquivalentTo(Superhero.Superheroes);
    }

    [Fact]
    public async Task WhenILikeUsedOnInMemoryConnection_ThenBadRequest()
    {
        //act
        var response = await fixture.Client.GetAsync("/demo/memory?where[ilike(x.name, billy)]");

        //assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task WhenStringContains_ThenCanFilter()
    {
        //act
        var response = await fixture.Client.GetWithHLinq("/demo/memory",
            q => q.For<Superhero>().Where(x => x.Name.Contains("Billy")));

        //assert
        response.Should().BeEquivalentTo(Superhero.Superheroes.Where(x => x.Name.Contains("Billy")));
    }

    [Fact]
    public async Task WhenStringContainsIgnoreCase_ThenCanFilter()
    {
        //act
        var response = await fixture.Client.GetWithHLinq("/demo/memory",
            q => q.For<Superhero>().Where(x => x.Name.Contains("Billy", StringComparison.InvariantCultureIgnoreCase)));

        //assert
        response.Should().BeEquivalentTo(Superhero.Superheroes.Where(x =>
            x.Name.Contains("Billy", StringComparison.InvariantCultureIgnoreCase)));
    }

    [Fact]
    public async Task WhenStringPropertyEquals_ThenCanFilter()
    {
        //act
        var response =
            await fixture.Client.GetWithHLinq("/demo/memory", q => q.For<Superhero>().Where(x => x.Name == "Billy"));

        //assert
        response.Should().BeEquivalentTo(Superhero.Superheroes.Where(x => x.Name == "Billy"));
    }

    [Fact]
    public async Task WhenStringPropertyNotEquals_ThenCanFilter()
    {
        //act
        var response =
            await fixture.Client.GetWithHLinq("/demo/memory", q => q.For<Superhero>().Where(x => x.Name != "Billy"));

        //assert
        response.Should().BeEquivalentTo(Superhero.Superheroes.Where(x => x.Name != "Billy"));
    }

    [Fact]
    public async Task WhenStringPropertyEqualsToNull_ThenCanFilter()
    {
        //act
        var response =
            await fixture.Client.GetWithHLinq("/demo/memory", q => q.For<Superhero>().Where(x => x.RealName == null));

        //assert
        response.Should().BeEquivalentTo(Superhero.Superheroes.Where(x => x.RealName == null));
    }

    [Fact]
    public async Task WhenGuidProperty_ThenCanFilter()
    {
        //act
        var guid = RandomData.Get().First().Id;
        var response =
            await fixture.Client.GetWithHLinq("/demo/random", q => q.For<RandomData>().Where(x => x.Id == guid));

        //assert
        response.Should().BeEquivalentTo(RandomData.Get().Where(x => x.Id == guid));
    }

    [Fact]
    public async Task WhenBoolPropertyWithoutValue_ThenCanFilter()
    {
        //act
        var response =
            await fixture.Client.GetWithHLinq("/demo/random", q => q.For<RandomData>().Where(x => x.Flag));

        //assert
        response.Should().BeEquivalentTo(RandomData.Get().Where(x => x.Flag));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task WhenBoolPropertyWithValue_ThenCanFilter(bool flag)
    {
        //act
        var response =
            await fixture.Client.GetWithHLinq("/demo/random", q => q.For<RandomData>().Where(x => x.Flag == flag));

        //assert
        response.Should().BeEquivalentTo(RandomData.Get().Where(x => x.Flag == flag));
    }

    [Fact]
    public async Task WhenBoolPropertyViaConstantTrue_ThenCanFilter()
    {
        //act
        var response =
            await fixture.Client.GetWithHLinq("/demo/random", q => q.For<RandomData>().Where(x => x.Flag == true));

        //assert
        // ReSharper disable once RedundantBoolCompare -> point of test
        response.Should().BeEquivalentTo(RandomData.Get().Where(x => x.Flag == true));
    }

    [Fact]
    public async Task WhenBoolPropertyViaConstantFalse_ThenCanFilter()
    {
        //act
        var response =
            await fixture.Client.GetWithHLinq("/demo/random", q => q.For<RandomData>().Where(x => x.Flag == false));

        //assert
        response.Should().BeEquivalentTo(RandomData.Get().Where(x => x.Flag == false));
    }

    [Theory]
    [MemberData(nameof(ByteData))]
    public async Task WhenBytePropertyEquals_ThenCanFilter(byte value)
    {
        //act
        var response =
            await fixture.Client.GetWithHLinq("/demo/random", q => q.For<RandomData>().Where(x => x.Byte == value));

        //assert
        response.Should().BeEquivalentTo(RandomData.Get().Where(x => x.Byte == value));
    }

    [Theory]
    [MemberData(nameof(NullableByteData))]
    public async Task WhenNullableBytePropertyEquals_ThenCanFilter(byte? value)
    {
        //act
        var response =
            await fixture.Client.GetWithHLinq("/demo/random",
                q => q.For<RandomData>().Where(x => x.NullableByte == value));

        //assert
        response.Should().BeEquivalentTo(RandomData.Get().Where(x => x.NullableByte == value));
    }

    [Theory]
    [MemberData(nameof(ShortData))]
    public async Task WhenShortPropertyEquals_ThenCanFilter(short value)
    {
        //act
        var response =
            await fixture.Client.GetWithHLinq("/demo/random", q => q.For<RandomData>().Where(x => x.Short == value));

        //assert
        response.Should().BeEquivalentTo(RandomData.Get().Where(x => x.Short == value));
    }

    [Theory]
    [MemberData(nameof(NullableShortData))]
    public async Task WhenNullableShortPropertyEquals_ThenCanFilter(short? value)
    {
        //act
        var response =
            await fixture.Client.GetWithHLinq("/demo/random",
                q => q.For<RandomData>().Where(x => x.NullableShort == value));

        //assert
        response.Should().BeEquivalentTo(RandomData.Get().Where(x => x.NullableShort == value));
    }

    [Theory]
    [MemberData(nameof(IntData))]
    public async Task WhenIntPropertyEquals_ThenCanFilter(int value)
    {
        //act
        var response =
            await fixture.Client.GetWithHLinq("/demo/random", q => q.For<RandomData>().Where(x => x.Int == value));

        //assert
        response.Should().BeEquivalentTo(RandomData.Get().Where(x => x.Int == value));
    }

    [Theory]
    [MemberData(nameof(NullableIntData))]
    public async Task WhenNullableIntPropertyEquals_ThenCanFilter(int? value)
    {
        //act
        var response =
            await fixture.Client.GetWithHLinq("/demo/random",
                q => q.For<RandomData>().Where(x => x.NullableInt == value));

        //assert
        response.Should().BeEquivalentTo(RandomData.Get().Where(x => x.NullableInt == value));
    }

    [Theory]
    [MemberData(nameof(LongData))]
    public async Task WhenLongPropertyEquals_ThenCanFilter(long value)
    {
        //act
        var response =
            await fixture.Client.GetWithHLinq("/demo/random", q => q.For<RandomData>().Where(x => x.Long == value));

        //assert
        response.Should().BeEquivalentTo(RandomData.Get().Where(x => x.Long == value));
    }

    [Theory]
    [MemberData(nameof(NullableLongData))]
    public async Task WhenNullableLongPropertyEquals_ThenCanFilter(long? value)
    {
        //act
        var response =
            await fixture.Client.GetWithHLinq("/demo/random",
                q => q.For<RandomData>().Where(x => x.NullableLong == value));

        //assert
        response.Should().BeEquivalentTo(RandomData.Get().Where(x => x.NullableLong == value));
    }

    [Theory]
    [MemberData(nameof(EnumData))]
    public async Task WhenEnumPropertyEquals_ThenCanFilter(RandomEnum value)
    {
        //act
        var response =
            await fixture.Client.GetWithHLinq("/demo/random", q => q.For<RandomData>().Where(x => x.Enum == value));

        //assert
        response.Should().BeEquivalentTo(RandomData.Get().Where(x => x.Enum == value));
    }

    [Theory]
    [MemberData(nameof(NullableEnumData))]
    public async Task WhenNullableEnumPropertyEquals_ThenCanFilter(RandomEnum? value)
    {
        //act
        var response =
            await fixture.Client.GetWithHLinq("/demo/random",
                q => q.For<RandomData>().Where(x => x.NullableEnum == value));

        //assert
        response.Should().BeEquivalentTo(RandomData.Get().Where(x => x.NullableEnum == value));
    }

    [Theory]
    [MemberData(nameof(FloatData))]
    public async Task WhenFloatPropertyEquals_ThenCanFilter(float value)
    {
        //act
        var response =
            await fixture.Client.GetWithHLinq("/demo/random", q => q.For<RandomData>().Where(x => x.Float == value));

        //assert
        response.Should().BeEquivalentTo(RandomData.Get().Where(x => x.Float == value));
    }

    [Theory]
    [MemberData(nameof(NullableFloatData))]
    public async Task WhenNullableFloatPropertyEquals_ThenCanFilter(float? value)
    {
        //act
        var response =
            await fixture.Client.GetWithHLinq("/demo/random",
                q => q.For<RandomData>().Where(x => x.NullableFloat == value));

        //assert
        response.Should().BeEquivalentTo(RandomData.Get().Where(x => x.NullableFloat == value));
    }

    [Theory]
    [MemberData(nameof(TimeOnlyData))]
    public async Task WhenTimeOnlyPropertyEquals_ThenCanFilter(TimeOnly value)
    {
        //act
        var response =
            await fixture.Client.GetWithHLinq("/demo/random", q => q.For<RandomData>().Where(x => x.Time == value));

        //assert
        response.Should().BeEquivalentTo(RandomData.Get().Where(x => x.Time == value));
    }

    [Theory]
    [MemberData(nameof(NullableTimeOnlyData))]
    public async Task WhenNullableTimeOnlyPropertyEquals_ThenCanFilter(TimeOnly? value)
    {
        //act
        var response =
            await fixture.Client.GetWithHLinq("/demo/random",
                q => q.For<RandomData>().Where(x => x.NullableTime == value));

        //assert
        response.Should().BeEquivalentTo(RandomData.Get().Where(x => x.NullableTime == value));
    }

    [Theory]
    [MemberData(nameof(DateTimeData))]
    public async Task WhenDateTimePropertyEquals_ThenCanFilter(DateTime value)
    {
        //act
        var response =
            await fixture.Client.GetWithHLinq("/demo/random", q => q.For<RandomData>().Where(x => x.DateTime == value));

        //assert
        response.Should().BeEquivalentTo(RandomData.Get().Where(x => x.DateTime == value));
    }

    [Theory]
    [MemberData(nameof(NullableDateTimeData))]
    public async Task WhenNullableDateTimePropertyEquals_ThenCanFilter(DateTime? value)
    {
        //act
        var response =
            await fixture.Client.GetWithHLinq("/demo/random",
                q => q.For<RandomData>().Where(x => x.NullableDateTime == value));

        //assert
        response.Should().BeEquivalentTo(RandomData.Get().Where(x => x.NullableDateTime == value));
    }

    [Theory]
    [MemberData(nameof(DateTimeOffsetData))]
    public async Task WhenDateTimeOffsetPropertyEquals_ThenCanFilter(DateTimeOffset value)
    {
        //act
        var response =
            await fixture.Client.GetWithHLinq("/demo/random",
                q => q.For<RandomData>().Where(x => x.DateTimeOffset == value));

        //assert
        response.Should().BeEquivalentTo(RandomData.Get().Where(x => x.DateTimeOffset == value));
    }

    [Theory]
    [MemberData(nameof(NullableDateTimeOffsetData))]
    public async Task WhenNullableDateTimeOffsetPropertyEquals_ThenCanFilter(DateTimeOffset? value)
    {
        //act
        var response =
            await fixture.Client.GetWithHLinq("/demo/random",
                q => q.For<RandomData>().Where(x => x.NullableDateTimeOffset == value));

        //assert
        response.Should().BeEquivalentTo(RandomData.Get().Where(x => x.NullableDateTimeOffset == value));
    }

    public static TheoryData<byte> ByteData =>
    [
        byte.MinValue,
        byte.MaxValue,
        1,
        10,
        100,
        233,
    ];

    public static TheoryData<byte?> NullableByteData => [null, ..ByteData];

    public static TheoryData<short> ShortData =>
    [
        short.MinValue,
        short.MaxValue,
        (short)-1000,
        (short)0,
        (short)1,
        (short)10,
        (short)100,
        (short)233,
        (short)32323,
    ];

    public static TheoryData<short?> NullableShortData => [null, ..ShortData];

    public static TheoryData<int> IntData =>
    [
        int.MinValue,
        int.MaxValue,
        -1,
        0,
        1,
        10,
        1000,
        323123,
    ];

    public static TheoryData<int?> NullableIntData => [null, ..IntData];

    public static TheoryData<long> LongData =>
    [
        long.MinValue,
        long.MaxValue,
        -1L,
        0L,
        1L,
        10L,
        1000L,
        323123L,
        323123321312L
    ];

    public static TheoryData<long?> NullableLongData => [null, ..LongData];

    public static TheoryData<float> FloatData =>
    [
        float.MaxValue,
        float.MinValue,
        (float)-1.0,
        (float)0.0,
        (float)1.21,
        (float)10.32132312,
        (float)1000.3213213,
        (float)323123.897979,
        (float)323123321312.897878d
    ];

    public static TheoryData<float?> NullableFloatData => [null, ..FloatData];

    public static TheoryData<RandomEnum> EnumData =>
    [
        (RandomEnum)0,
        (RandomEnum)1,
        (RandomEnum)2,
        (RandomEnum)30,
        (RandomEnum)(-2),
        RandomEnum.Zero,
        RandomEnum.One,
        RandomEnum.Two,
        RandomEnum.Longer,
        RandomEnum.Negative,
    ];

    public static TheoryData<RandomEnum?> NullableEnumData => [null, ..EnumData];

    public static TheoryData<TimeOnly> TimeOnlyData =>
    [
        TimeOnly.MinValue,
        TimeOnly.MaxValue,
        TimeOnly.Parse("00:00:00"),
        TimeOnly.Parse("23:59:59.99999999"),
        TimeOnly.Parse("15:13:23.000"),
        TimeOnly.Parse("23:59:33.999"),
        TimeOnly.Parse("01:45:21.321"),
        TimeOnly.Parse("15:55:40.433")
    ];

    public static TheoryData<TimeOnly?> NullableTimeOnlyData => [null, ..TimeOnlyData];

    public static TheoryData<DateTime> DateTimeData =>
    [
        DateTime.MinValue,
        DateTime.MaxValue,
        DateTime.Parse("0001-01-01 00:00:00"),
        DateTime.Parse("9999-01-01 00:00"),
        DateTime.Parse("2000-02-27 15:13:23.000"),
        DateTime.Parse("2024-10-11 23:59:33.999"),
        DateTime.Parse("2010-08-31 01:45:21.321"),
        DateTime.Parse("2030-12-22 15:55:40.433")
    ];

    public static TheoryData<DateTime?> NullableDateTimeData => [null, ..DateTimeData];

    public static TheoryData<DateTimeOffset> DateTimeOffsetData =>
    [
        DateTimeOffset.MinValue,
        DateTimeOffset.MaxValue,
        DateTimeOffset.Parse("0002-01-01 00:00:00"),
        DateTimeOffset.Parse("9999-01-01 00:00"),
        DateTimeOffset.Parse("2000-02-27 15:13:23.000"),
        DateTimeOffset.Parse("2024-10-11 23:59:33.999"),
        DateTimeOffset.Parse("2010-08-31 01:45:21.321"),
        DateTimeOffset.Parse("2030-12-22 15:55:40.433"),
        DateTimeOffset.Parse("2024-05-31 01:45:21.321+00:00"),
        DateTimeOffset.Parse("2011-01-30 15:55:20.433+01:00"),
        DateTimeOffset.Parse("2023-07-19 01:45:21.321-01:30"),
        DateTimeOffset.Parse("2012-12-31 15:55:32.433+04:00"),
    ];

    public static TheoryData<DateTimeOffset?> NullableDateTimeOffsetData => [null, .. DateTimeOffsetData];
}
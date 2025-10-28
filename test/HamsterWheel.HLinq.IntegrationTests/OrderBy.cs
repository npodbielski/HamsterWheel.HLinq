using System.Net;
using FluentAssertions;
using HamsterWheel.HLinq.Client;
using HamsterWheel.HLinq.Demo.Data;

namespace HamsterWheel.HLinq.IntegrationTests;

partial class MemoryDataTests
{
    [Fact]
    public async Task WhenEmptyOrderBy_ThenReturns400()
    {
        //act
        var response = await fixture.Client.GetAsync("/demo/memory?orderBy[]");

        //assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task WhenOrderByString_ThenReturnsCorrectData()
    {
        //act
        var response = await fixture.Client.GetWithHLinq("/demo/random", q => q.For<RandomData>().OrderBy(x => x.Name));

        //assert
        var expected = RandomData.Get().OrderBy(x => x.Name).ToArray();
        for (int i = 0; i < response.Length; i++)
        {
            response[i].Should().BeEquivalentTo(expected[i]);
        }
    }

    [Fact]
    public async Task WhenOrderByFlag_ThenReturnsCorrectData()
    {
        //act
        var response = await fixture.Client.GetWithHLinq("/demo/random", q => q.For<RandomData>().OrderBy(x => x.Flag));

        //assert
        var expected = RandomData.Get().OrderBy(x => x.Flag).ToArray();
        for (var i = 0; i < response.Length; i++)
        {
            response[i].Should().BeEquivalentTo(expected[i]);
        }
    }


    [Fact]
    public async Task WhenOrderByEnum_ThenReturnsCorrectData()
    {
        //act
        var response = await fixture.Client.GetWithHLinq("/demo/random", q => q.For<RandomData>().OrderBy(x => x.Enum));

        //assert
        var expected = RandomData.Get().OrderBy(x => x.Enum).ToArray();
        for (var i = 0; i < response.Length; i++)
        {
            response[i].Should().BeEquivalentTo(expected[i]);
        }
    }

    [Fact]
    public async Task WhenOrderByDateTime_ThenReturnsCorrectData()
    {
        //act
        var response =
            await fixture.Client.GetWithHLinq("/demo/random", q => q.For<RandomData>().OrderBy(x => x.DateTime));

        //assert
        var expected = RandomData.Get().OrderBy(x => x.DateTime).ToArray();
        for (var i = 0; i < response.Length; i++)
        {
            response[i].Should().BeEquivalentTo(expected[i]);
        }
    }

    [Fact]
    public async Task WhenOrderByNullableDateTimeOffset_ThenReturnsCorrectData()
    {
        //act
        var response =
            await fixture.Client.GetWithHLinq("/demo/random",
                q => q.For<RandomData>().OrderBy(x => x.NullableDateTimeOffset));

        //assert
        var expected = RandomData.Get().OrderBy(x => x.NullableDateTimeOffset).ToArray();
        for (var i = 0; i < response.Length; i++)
        {
            response[i].Should().BeEquivalentTo(expected[i]);
        }
    }

    [Fact]
    public async Task WhenOrderByInt_ThenReturnsCorrectData()
    {
        //act
        var response =
            await fixture.Client.GetWithHLinq("/demo/random", q => q.For<RandomData>().OrderBy(x => x.Int));

        //assert
        var expected = RandomData.Get().OrderBy(x => x.Int).ToArray();
        for (var i = 0; i < response.Length; i++)
        {
            response[i].Should().BeEquivalentTo(expected[i]);
        }
    }

    [Fact]
    public async Task WhenOrderByDescendingInt_ThenReturnsCorrectData()
    {
        //act
        var response =
            await fixture.Client.GetWithHLinq("/demo/random",
                q => q.For<RandomData>().OrderByDescending(x => x.Int));

        //assert
        var expected = RandomData.Get().OrderByDescending(x => x.Int).ToArray();
        for (var i = 0; i < response.Length; i++)
        {
            response[i].Should().BeEquivalentTo(expected[i]);
        }
    }

    [Fact]
    public async Task WhenOrderByEnumThenInt_ThenReturnsCorrectData()
    {
        //act
        var response =
            await fixture.Client.GetWithHLinq("/demo/random",
                q => q.For<RandomData>().OrderBy(x => x.Enum).ThenBy(x => x.Int));

        //assert
        var expected = RandomData.Get().OrderBy(x => x.Enum).ThenByDescending(x => x.Int).ToArray();
        for (var i = 0; i < response.Length; i++)
        {
            response[i].Should().BeEquivalentTo(expected[i]);
        }
    }

    [Fact]
    public async Task WhenOrderByEnumThenByDescendingInt_ThenReturnsCorrectData()
    {
        //act
        var response = await fixture.Client.GetWithHLinq("/demo/random",
            q => q.For<RandomData>().OrderBy(x => x.Enum).ThenByDescending(x => x.Int));

        //assert
        response.Should().BeEquivalentTo(RandomData.Get());
        var expected = RandomData.Get().OrderBy(x => x.Enum).ThenByDescending(x => x.Int).ToArray();
        for (var i = 0; i < response.Length; i++)
        {
            response[i].Should().BeEquivalentTo(expected[i]);
        }
    }
}
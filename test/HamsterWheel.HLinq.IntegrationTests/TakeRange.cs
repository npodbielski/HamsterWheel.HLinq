using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using HamsterWheel.HLinq.Client;
using HamsterWheel.HLinq.Demo.Data;

namespace HamsterWheel.HLinq.IntegrationTests;

partial class MemoryDataTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private async Task<Superhero[]> GetSuperheroesAsync(string query)
    {
        var response = await fixture.Client.GetAsync($"/demo/memory?{query}");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<Superhero[]>(JsonOptions))!;
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(3, 7)]
    [InlineData(10, 20)]
    [InlineData(0, 40)]
    [InlineData(39, 40)]
    [InlineData(1, 1000)] // big numbers, uncapped (MaxTake is 1000)
    [InlineData(1000, 1010)] // beyond data
    [InlineData(676766, 66878878)] // beyond data, huge range
    [InlineData(35, 50)] // page of ten over 40 rows, data exhausted -> 5 rows, no throw
    public async Task WhenTakeRange_ThenReturnsSameRowsAsSkipThenTake(int start, int end)
    {
        //act
        var actual = await GetSuperheroesAsync($"take[{start}..{end}]");

        //assert
        actual.Should().BeEquivalentTo(Superhero.Superheroes.Skip(start).Take(end - start));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    [InlineData(39)]
    public async Task WhenTakeRangeOpenEnd_ThenReturnsSameRowsAsSkip(int start)
    {
        //act
        var actual = await GetSuperheroesAsync($"take[{start}..]");

        //assert
        actual.Should().BeEquivalentTo(Superhero.Superheroes.Skip(start));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(20)]
    public async Task WhenTakeRangeOpenStart_ThenReturnsSameRowsAsTake(int end)
    {
        //act
        var actual = await GetSuperheroesAsync($"take[..{end}]");

        //assert
        actual.Should().BeEquivalentTo(Superhero.Superheroes.Take(end));
    }

    [Fact]
    public async Task WhenTakeRangeEquivalentToSkipTakeChain_ThenReturnsSameRows()
    {
        //act
        var range = await GetSuperheroesAsync("skip[5].take[10..20]");
        var chained = await GetSuperheroesAsync("skip[5].skip[10].take[10]");

        //assert
        range.Should().BeEquivalentTo(chained);
    }

    [Fact]
    public async Task WhenClientBuilderTakeRange_ThenReturnsSameRowsAsSkipThenTake()
    {
        //act - round trip: client Take(Range) over the wire to the server range parser
        var response = await fixture.Client.GetWithHLinq("/demo/memory", q => q.For<Superhero>().Take(3..7));

        //assert
        response.Should().BeEquivalentTo(Superhero.Superheroes.Skip(3).Take(4));
    }

    [Theory]
    [InlineData("take[..]")] // takes nothing, means nothing
    [InlineData("take[5..2]")] // start after end
    [InlineData("take[3..3]")] // empty range
    [InlineData("take[-1..2]")] // negative start
    [InlineData("take[a..b]")] // not a number
    [InlineData("skip[10..20]")] // skip keeps its single value
    [InlineData("take[10..2..3]")] // chained ranges
    [InlineData("take[10..20")] // not closed
    public async Task WhenInvalidTakeRange_ThenReturns400(string query)
    {
        //act
        var response = await fixture.Client.GetAsync($"/demo/memory?{query}");

        //assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}

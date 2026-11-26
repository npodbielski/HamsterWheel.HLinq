using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace HamsterWheel.HLinq.IntegrationTests;

/// <summary>
/// A computation in a filter or an ordering key, asked of the live Postgres over encoded operators.
/// </summary>
partial class DbDataTests
{
    [Fact]
    public async Task WhenFilterComparesADifference_ThenServerSubtractsAndCompares()
    {
        //act
        var response = await fixture.Client.GetAsync("/demo/db?where[x.Id-2>3].take[10]");
        response.EnsureSuccessStatusCode();

        //assert
        Json(await response.Content.ReadFromJsonAsync<JsonElement>()).Should().Be(
            Json(Persons.Where(x => x.Id - 2 > 3).Take(10).ToList()));
    }

    [Fact]
    public async Task WhenFilterComparesAGroupOfWrittenNumbers_ThenServerComparesTheComputedOne()
    {
        //act
        var response = await fixture.Client.GetAsync("/demo/db?where[x.Id>(2*3)].take[10]");
        response.EnsureSuccessStatusCode();

        //assert
        Json(await response.Content.ReadFromJsonAsync<JsonElement>()).Should().Be(
            Json(Persons.Where(x => x.Id > 2 * 3).Take(10).ToList()));
    }

    [Fact]
    public async Task WhenKeyIsASum_ThenServerOrdersByTheComputedKey()
    {
        //act
        var response = await fixture.Client.GetAsync(
            "/demo/db?orderBy[x.Id%2B1].take[10]");
        response.EnsureSuccessStatusCode();

        //assert
        Json(await response.Content.ReadFromJsonAsync<JsonElement>()).Should().Be(
            Json(Persons.OrderBy(x => x.Id + 1).Take(10).ToList()));
    }

    [Fact]
    public async Task WhenKeyOpensWithAGroup_ThenServerComputesTheGroupFirst()
    {
        //act
        var response = await fixture.Client.GetAsync(
            "/demo/db?orderBy[(x.Id%2Bx.Id)*2].take[10]");
        response.EnsureSuccessStatusCode();

        //assert
        Json(await response.Content.ReadFromJsonAsync<JsonElement>()).Should().Be(
            Json(Persons.OrderBy(x => (x.Id + x.Id) * 2).Take(10).ToList()));
    }
}

using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using HamsterWheel.HLinq.Client;
using HamsterWheel.HLinq.Demo.Data;
using HamsterWheel.HLinq.IntegrationTests.Fixtures;

namespace HamsterWheel.HLinq.IntegrationTests;

/// <summary>
///     The parameters of the request go out before the query is read - the endpoint's are left to the
///     endpoint, and the one culture parameter HLinq reads is read here, wherever in the request it
///     stood. A request that carries text the query does not consist of is answered with an error rather
///     than with a query that quietly means something else.
/// </summary>
[Collection(nameof(IntegrationCollectionDefinition))]
public class CultureParameterTests(DemoFixture fixture)
{
    [Theory]
    [InlineData("/demo/memory?where[x.Name==Superman]&culture=pl-PL")]
    [InlineData("/demo/memory?culture=pl-PL&where[x.Name==Superman]")]
    [InlineData("/demo/memory?culture=pl-PL&where[x.Name==Superman].take[1]&culture=pl-GB")]
    [InlineData("/demo/memory?culture=pl&culture=en&where[x.Name==Superman]")]
    public async Task WhenTheCultureParameterStandsBesideAClause_ThenFilters(string url)
    {
        //act
        var response = await fixture.Client.GetAsync(url);

        //assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await Read(response)).Should().BeEquivalentTo(Superhero.Superheroes.Where(x => x.Name == "Superman"));
    }

    [Theory]
    //the parameters of the endpoint go out before the query is read: HLinq neither applies them nor
    //complains about them
    [InlineData("/demo/memory?page=2&where[x.Name==Superman]")]
    [InlineData("/demo/memory?where[x.Name==Superman]&page=2&sort=asc")]
    [InlineData("/demo/memory?person.name=John&where[x.Name==Superman]")]
    public async Task WhenParametersOfTheEndpointStandBesideAClause_ThenFilters(string url)
    {
        //act
        var response = await fixture.Client.GetAsync(url);

        //assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await Read(response)).Should().BeEquivalentTo(Superhero.Superheroes.Where(x => x.Name == "Superman"));
    }

    [Theory]
    [InlineData("/demo/memory?page=2&sort=asc")]
    [InlineData("/demo/memory?culture=pl-PL")]
    public async Task WhenQueryIsOnlyParameters_ThenReturnsEverything(string url)
    {
        //act
        var response = await fixture.Client.GetAsync(url);

        //assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await Read(response)).Should().BeEquivalentTo(Superhero.Superheroes);
    }

    [Theory]
    [InlineData("/demo/memory?where[x.Name==Superman]&take[1]")]
    [InlineData("/demo/memory?where[x.Name==Superman]&culture=pl-PL&take[1]")]
    [InlineData("/demo/memory?wher[x.Name==Superman]&culture=pl-PL")]
    [InlineData("/demo/memory?filter[x.Name]==Superman&take[1]")]
    public async Task WhenQueryCarriesWhatIsNotAClause_ThenReturns400(string url)
    {
        //act
        var response = await fixture.Client.GetAsync(url);

        //assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private static async Task<Superhero[]> Read(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<Superhero[]>(
            ResponseHLinqClientQueryBuilder.DefaultJsonSerializerOptions);
}

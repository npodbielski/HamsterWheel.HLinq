using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using HamsterWheel.HLinq.Client;
using HamsterWheel.HLinq.Demo.Data;
using HamsterWheel.HLinq.IntegrationTests.Fixtures;

namespace HamsterWheel.HLinq.IntegrationTests;

/// <summary>
/// The parameters of the request go out before the query is read - the endpoint's are left to the
/// endpoint, and the one culture parameter HLinq reads is read here, wherever in the request it
/// stood. A request that carries text the query does not consist of is answered with an error rather
/// than with a query that quietly means something else.
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

    [Fact]
    public async Task WhenValuesAreAskedForInAnAllowedCulture_ThenTheyAreReadInIt()
    {
        //act
        var response = await fixture.Client.GetAsync("""/demo/random?culture=pl-PL&where[x.Decimal>"1,2"]""");

        //assert - there the comma is where the fraction starts, so this is one and a fraction
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadRandom(response)).Should().BeEquivalentTo(RandomData.Get().Where(x => x.Decimal > 1.2m));
    }

    [Fact]
    public async Task WhenTheSameQueryNamesNoCulture_ThenItsValuesAreReadInInvariantCulture()
    {
        //act
        var response = await fixture.Client.GetAsync("""/demo/random?where[x.Decimal>"1,2"]""");

        //assert - here the same characters group thousands, so this is twelve
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadRandom(response)).Should().BeEquivalentTo(RandomData.Get().Where(x => x.Decimal > 12m));
    }

    [Fact]
    public async Task WhenValuesAreAskedForInAnotherKnownCulture_ThenTheyAreReadInIt()
    {
        //act - no endpoint config is involved: a culture the runtime knows is a culture to read a value in
        var response = await fixture.Client.GetAsync("""/demo/random?culture=de-DE&where[x.Decimal>"1,2"]""");

        //assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadRandom(response)).Should().BeEquivalentTo(RandomData.Get().Where(x => x.Decimal > 1.2m));
    }

    [Fact]
    public async Task WhenAClauseCarriesItsOwnAmpersands_ThenTheRequestIsOneClause()
    {
        //act - `&&` is HLinq's operator, sent as it is written: a url reads it as the separator of two
        //parameters and the query reads it as a condition
        var response = await fixture.Client
            .GetAsync("/demo/memory?where[(x.Name==Superman||x.Name==Batman)&&x.Name!=Batman]");

        //assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await Read(response)).Should()
            .BeEquivalentTo(Superhero.Superheroes.Where(x => x.Name == "Superman"));
    }

    [Fact]
    public async Task WhenAQueryIsWrittenTheWayItReads_ThenTheEndpointAnswersIt()
    {
        //act - spaces, the case of the properties as the entity spells them in json, a method call and
        //the two ampersands of a condition: everything a query is written with, sent as it is written
        var response = await fixture.Client.GetAsync(
            "/demo/memory?where[ ( x.name == Superman || x.name == Batman) && x.realName.contains(Clark) ]"
                .Replace(" ", "%20"));

        //assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await Read(response)).Should()
            .BeEquivalentTo(Superhero.Superheroes.Where(x => x.Name == "Superman"));
    }

    [Theory]
    //`te` is Telugu, `test` is a name that is not a culture
    [InlineData("/demo/random?culture=test&where[x.Decimal>1]")]
    [InlineData("/demo/random?culture=notaculture!&where[x.Decimal>1]")]
    public async Task WhenRequestNamesSomethingThatIsNotACulture_ThenReturns400(string url)
    {
        //act
        var response = await fixture.Client.GetAsync(url);

        //assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private static Task<RandomData[]?> ReadRandom(HttpResponseMessage response) =>
        response.Content.ReadFromJsonAsync<RandomData[]>(
            ResponseHLinqClientQueryBuilder.DefaultJsonSerializerOptions);

    private static async Task<Superhero[]?> Read(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<Superhero[]>(
            ResponseHLinqClientQueryBuilder.DefaultJsonSerializerOptions);
}

using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using HamsterWheel.HLinq.Client;
using HamsterWheel.HLinq.Demo.Data;
using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Filtering;
using HamsterWheel.HLinq.IntegrationTests.Fixtures;
using HamsterWheel.HLinq.Pipeline.Tokenizer;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace HamsterWheel.HLinq.IntegrationTests;

/// <summary>
/// An endpoint answers a request in the language the request names: its clauses are spelled with the
/// words of that culture, beside the invariant words it always answered - and the words of a culture
/// the request does not name are no words at all.
/// </summary>
[Collection(nameof(IntegrationCollectionDefinition))]
public class TranslatedKeywordsTests(DemoFixture fixture)
{
    [Theory]
    //the demo endpoint is asked in pl, and answers in pl without giving up the words it always had
    [InlineData("/demo/memory?culture=pl&gdzie[x.Name==Superman]")]
    [InlineData("/demo/memory?culture=pl&where[x.Name==Superman]")]
    [InlineData("/demo/memory?gdzie[x.Name==Superman]&culture=pl")]
    public async Task WhenAClauseIsSpelledInTheLanguageOfTheRequest_ThenItFilters(string url)
    {
        //act
        var response = await fixture.Client.GetAsync(url);

        //assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await Read(response)).Should().BeEquivalentTo(Superhero.Superheroes.Where(x => x.Name == "Superman"));
    }

    [Theory]
    //the words belong to the culture that registered them: `gdzie` is no word for an endpoint asked in
    //no culture, and no culture means the invariant words
    [InlineData("/demo/memory?gdzie[x.Name==Superman]")]
    [InlineData("/demo/memory?culture=de-DE&gdzie[x.Name==Superman]")]
    public async Task WhenAClauseIsSpelledInALanguageTheRequestDoesNotSpeak_ThenReturns400(string url)
    {
        //act
        var response = await fixture.Client.GetAsync(url);

        //assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    //`sortujPo` is the beginning of `sortujPoMalejaco`, and the shorter one is a clause of its own
    [InlineData("sortujPo", true)]
    [InlineData("sortujPoMalejaco", false)]
    public async Task WhenWordsOfALanguageDifferByASuffix_ThenEachOrdersItsOwnWay(string word, bool ascending)
    {
        //act
        var response = await fixture.Client.GetAsync($"/demo/memory?culture=pl&{word}[x.Name].take[3]");

        //assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var expected = ascending
            ? Superhero.Superheroes.OrderBy(x => x.Name).Take(3)
            : Superhero.Superheroes.OrderByDescending(x => x.Name).Take(3);
        (await Read(response)).Should().BeEquivalentTo(expected, o => o.WithStrictOrdering());
    }

    [Fact]
    public async Task WhenARequestAsksForTheWordsOfACulture_ThenItsValuesAreReadInThatCultureToo()
    {
        //act - the clauses of pl and the fraction comma of pl, in one request
        var response = await fixture.Client.GetAsync("""/demo/random?culture=pl&gdzie[x.Decimal>"1,2"]""");

        //assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadRandom(response)).Should().BeEquivalentTo(RandomData.Get().Where(x => x.Decimal > 1.2m));
    }

    [Fact]
    public void WhenTheApiKeepsATable_ThenTheLanguageItSpellsIsValid()
    {
        //assert - the words of a language are assembled and self-checked while the API is composed: a
        //word swallowing a longer one is refused here, not on a request written in it
        var act = () => fixture.DemoTestHost.Services.GetRequiredService<IHLinqTokenizer>();

        act.Should().NotThrow();
    }

    [Fact]
    public void WhenAWordOfTheTableSwallowsAWordItCompetesWith_ThenTheApiDoesNotStart()
    {
        //arrange - `wh` completes where `where` could still continue and both are allowed in the same
        //place and clause, so the words do not make a language worth answering in
        using var host = fixture.DemoTestHost.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
                services.AddSingleton<IHLinqWord>(new SwallowingWhere())));

        //act
        var act = () => host.CreateClient();

        //assert - refused while the API starts, so no request written in those words is ever answered
        var failure = Record.Exception(act);
        while (failure!.InnerException is not null)
        {
            failure = failure.InnerException;
        }

        failure.Should().BeOfType<GrammarValidationException>()
            .Which.Message.Should().Contain("'wh' completes where 'where' could still continue");
    }

    private sealed class SwallowingWhere() : TokenWord<Where>("wh", CultureInfo.GetCultureInfo("pl"));

    private static Task<RandomData[]?> ReadRandom(HttpResponseMessage response) =>
        response.Content.ReadFromJsonAsync<RandomData[]>(
            ResponseHLinqClientQueryBuilder.DefaultJsonSerializerOptions);

    private static async Task<Superhero[]?> Read(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<Superhero[]>(
            ResponseHLinqClientQueryBuilder.DefaultJsonSerializerOptions);
}

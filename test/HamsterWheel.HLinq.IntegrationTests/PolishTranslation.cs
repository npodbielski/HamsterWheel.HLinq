using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using HamsterWheel.HLinq.Demo.Data;
using HamsterWheel.HLinq.IntegrationTests.Fixtures;

namespace HamsterWheel.HLinq.IntegrationTests;

/// <summary>
/// The words of a language installed as a package: the demo answers them to a request that names the
/// culture, and answers nothing else to a request that does not - a word of `pl` is no word at all to a
/// query written in the invariant language.
/// </summary>
[Collection(nameof(IntegrationCollectionDefinition))]
public class PolishTranslationTests(DemoFixture fixture)
{
    [Theory]
    [InlineData("/demo/memory?culture=pl&gdzie[x.Name==Superman]")]
    [InlineData("/demo/memory?culture=pl&gdzie[x.Name==Superman].sortujPo[x.Name].wez[1]")]
    [InlineData("/demo/memory?culture=pl&wybierz[x.Name].pomin[1].wez[1]")]
    public async Task WhenAClauseIsSpelledInTheLanguageOfTheRequest_ThenTheQueryIsAnswered(string url)
    {
        //act
        var response = await fixture.Client.GetAsync(url);

        //assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task WhenAQueryCountsInPolish_ThenItCounts()
    {
        //act
        var response = await fixture.Client.GetAsync("/demo/memory?culture=pl&zlicz[]");

        //assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<int>()).Should().Be(Superhero.Superheroes.Length);
    }

    [Fact]
    public async Task WhenAQueryIsSpelledInPolishAndNamesNoCulture_ThenItIsRefused()
    {
        //act
        var response = await fixture.Client.GetAsync("/demo/memory?gdzie[x.Name==Superman]");

        //assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}

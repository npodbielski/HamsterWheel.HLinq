using System.Net;
using FluentAssertions;
using HamsterWheel.HLinq.Client;
using HamsterWheel.HLinq.Demo.Data;

namespace HamsterWheel.HLinq.IntegrationTests;

partial class MemoryDataTests
{
    [Fact]
    public async Task WhenEmptySelect_ThenReturns400()
    {
        //act
        var response = await fixture.Client.GetAsync("/demo/memory?select[]");

        //assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task WhenEmptyQuery_ThenReturnsCollectionAsIs()
    {
        //act
        var response = await fixture.Client.GetWithHLinq("/demo/memory", q => q.For<Superhero>());

        //assert
        response.Should().BeEquivalentTo(Superhero.Superheroes);
    }

    [Fact]
    public async Task WhenSinglePropertySelect_ThenReturnsCorrectResult()
    {
        //act
        var response = await fixture.Client.GetWithHLinq("/demo/memory", q => q.For<Superhero>().Select(x => x.Name));

        //assert
        response.Should().BeEquivalentTo(Superhero.Superheroes.Select(x => x.Name));
    }

    [Fact]
    public async Task WhenSinglePropertySelectRemapped_ThenReturnsCorrectData()
    {
        //act
        var response =
            await fixture.Client.GetWithHLinq("/demo/memory", q => q.For<Superhero>().Select(x => new { Mode = x.Name }));

        //assert
        response.Should().BeEquivalentTo(Superhero.Superheroes.Select(x => new { Mode = x.Name }));
    }

    [Fact]
    public async Task WhenNestedPropertySelectRemapped_ThenReturnsCorrectData()
    {
        //act
        var response =
            await fixture.Client.GetWithHLinq("/demo/memory", q => q.For<Superhero>().Select(x => x.Look.Colors));

        //assert
        response.Should().BeEquivalentTo(Superhero.Superheroes.Select(x => x.Look.Colors));
    }

    [Fact]
    public async Task WhenTwoProps_ThenReturnsCorrectData()
    {
        //act
        var response =
            await fixture.Client.GetWithHLinq("/demo/memory",
                q => q.For<Superhero>().Select(x => new { x.Name, x.Look.Colors }));

        //assert
        response.Should().BeEquivalentTo(Superhero.Superheroes.Select(x => new { x.Name, x.Look.Colors }));
    }

    [Fact]
    public async Task WhenThreeProps_ThenReturnsCorrectData()
    {
        //act
        var response =
            await fixture.Client.GetWithHLinq("/demo/memory",
                q => q.For<Superhero>().Select(x => new { x.Name, x.Look.Colors, x.RealName }));

        //assert
        response.Should().BeEquivalentTo(Superhero.Superheroes.Select(x => new { x.Name, x.Look.Colors, x.RealName }));
    }

    [Fact]
    public async Task When6Props_ThenReturnsCorrectData()
    {
        //act
        var response =
            await fixture.Client.GetWithHLinq("/demo/memory",
                q => q.For<Superhero>().Select(x => new
                {
                    x.Name,
                    x.Look,
                    x.Look.Colors,
                    x.RealName,
                    SuperheroName = x.Name,
                    Alias = x.RealName
                }));

        //assert
        response.Should().BeEquivalentTo(Superhero.Superheroes.Select(x => new
        {
            x.Name,
            x.Look,
            x.Look.Colors,
            x.RealName,
            SuperheroName = x.Name,
            Alias = x.RealName
        }));
    }
}
using System.Net;
using FluentAssertions;
using HamsterWheel.HLinq.Client;
using HamsterWheel.HLinq.Demo.Data;

namespace HamsterWheel.HLinq.IntegrationTests;

partial class DbDataTests
{
    [Fact]
    public async Task WhenEmptySelect_ThenReturns400()
    {
        //act
        var response = await fixture.Client.GetAsync("/demo/db?select[]");

        //assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task WhenEmptyQuery_ThenReturnsCollectionAsIs()
    {
        //act
        var response = await fixture.Client.GetWithHLinq("/demo/db", q => q.For<Person>());

        //assert
        response.Should().BeEquivalentTo(Persons);
    }

    [Fact]
    public async Task WhenSinglePropertySelect_ThenReturnsCorrectResult()
    {
        //act
        var response = await fixture.Client.GetWithHLinq("/demo/db", q => q.For<Person>().Select(x => x.FirstName));

        //assert
        response.Should().BeEquivalentTo(Persons.Select(x => x.FirstName));
    }

    [Fact]
    public async Task WhenSinglePropertySelectRemapped_ThenReturnsCorrectData()
    {
        //act
        var response =
            await fixture.Client.GetWithHLinq("/demo/db",
                q => q.For<Person>().Select(x => new { N = x.FirstName }));

        //assert
        response.Should().BeEquivalentTo(Persons.Select(x => new { N = x.FirstName }));
    }

    [Fact]
    public async Task WhenTwoProps_ThenReturnsCorrectData()
    {
        //act
        var response =
            await fixture.Client.GetWithHLinq("/demo/db",
                q => q.For<Person>().Select(x => new { x.FirstName, x.Email }));

        //assert
        response.Should().BeEquivalentTo(Persons.Select(x => new { x.FirstName, x.Email }));
    }

    [Fact]
    public async Task WhenThreeProps_ThenReturnsCorrectData()
    {
        //act
        var response =
            await fixture.Client.GetWithHLinq("/demo/db",
                q => q.For<Person>().Select(x => new { x.FirstName, x.Email, x.LastName }));

        //assert
        response.Should().BeEquivalentTo(Persons.Select(x => new { x.FirstName, x.Email, x.LastName }));
    }

    [Fact]
    public async Task When6Props_ThenReturnsCorrectData()
    {
        //act
        var response =
            await fixture.Client.GetWithHLinq("/demo/db",
                q => q.For<Person>().Select(x => new
                {
                    x.Id,
                    x.FirstName,
                    x.Email,
                    x.IpAddress,
                    x.LastName,
                    Name = x.FirstName,
                    SurName = x.LastName
                }));

        //assert
        response.Should().BeEquivalentTo(Persons.Select(x => new
        {
            x.Id,
            x.FirstName,
            x.Email,
            x.IpAddress,
            x.LastName,
            Name = x.FirstName,
            SurName = x.LastName
        }));
    }
}
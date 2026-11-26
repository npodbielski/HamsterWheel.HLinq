using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using HamsterWheel.HLinq.Client;
using HamsterWheel.HLinq.Demo.Data;
using Microsoft.AspNetCore.Mvc;

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
        var response = await fixture.Client.GetWithHLinq("/demo/db", q => q.For<Person>().Take(100));

        //assert
        response.Should().BeEquivalentTo(Persons.Take(100));
    }

    [Fact]
    public async Task WhenSinglePropertySelect_ThenReturnsCorrectResult()
    {
        //act
        var response = await fixture.Client.GetWithHLinq("/demo/db", q => q.For<Person>().Select(x => x.FirstName).Take(100));

        //assert
        response.Should().BeEquivalentTo(Persons.Select(x => x.FirstName).Take(100));
    }

    [Fact]
    public async Task WhenSinglePropertySelectRemapped_ThenReturnsCorrectData()
    {
        //act
        var response =
            await fixture.Client.GetWithHLinq("/demo/db",
                q => q.For<Person>().Select(x => new { N = x.FirstName }).Take(100));

        //assert
        response.Should().BeEquivalentTo(Persons.Select(x => new { N = x.FirstName }).Take(100));
    }

    [Fact]
    public async Task WhenTwoProps_ThenReturnsCorrectData()
    {
        //act
        var response =
            await fixture.Client.GetWithHLinq("/demo/db",
                q => q.For<Person>().Select(x => new { x.FirstName, x.Email }).Take(100));

        //assert
        response.Should().BeEquivalentTo(Persons.Select(x => new { x.FirstName, x.Email }).Take(100));
    }

    [Fact]
    public async Task WhenThreeProps_ThenReturnsCorrectData()
    {
        //act
        var response =
            await fixture.Client.GetWithHLinq("/demo/db",
                q => q.For<Person>().Select(x => new { x.FirstName, x.Email, x.LastName }).Take(100));

        //assert
        response.Should().BeEquivalentTo(Persons.Select(x => new { x.FirstName, x.Email, x.LastName }).Take(100));
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
                }).Take(100));

        //assert
        response.Should().BeEquivalentTo([
            .. Persons.Select(x => new
            {
                x.Id,
                x.FirstName,
                x.Email,
                x.IpAddress,
                x.LastName,
                Name = x.FirstName,
                SurName = x.LastName
            }).Take(100)
        ]);
    }

    [Theory]
    [InlineData("select[x.FirstName,x.FirstName]")]
    [InlineData("select[Z=x.FirstName,Z=x.LastName]")]
    [InlineData("select[Z=x.FirstName,Z=x.Id]")]
    [InlineData("select[S=x.Addresses.select(Z=a.Street,Z=a.City)]")]
    public async Task WhenTwoMembersAnswerUnderOneName_ThenReturns400WithTheReason(string query)
    {
        //act
        //database is never asked which of the two values it should answer
        var response = await fixture.Client.GetAsync($"/demo/db?{query}.take[10]");

        //assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = (await response.Content.ReadFromJsonAsync<ProblemDetails>()).Should().NotBeNull()
            .And.BeOfType<ProblemDetails>().Which;
        error.Status.Should().Be(400);
        error.Title.Should().MatchEquivalentOf("*answers two values*");
    }

    [Fact]
    public async Task WhenSelectCallRenamed_ThenServerAnswersTheCall()
    {
        //act
        var response = await fixture.Client.GetAsync("/demo/db?select[U=x.FirstName.ToUpper()].take[10]");
        response.EnsureSuccessStatusCode();

        //assert
        Json(await response.Content.ReadFromJsonAsync<JsonElement>()).Should().Be(
            Json(Persons.Select(x => new { U = x.FirstName.ToUpper() }).Take(10).ToList()));
    }

    [Fact]
    public async Task WhenSelectCallUnnamed_ThenAnswersUnderTheMethodName()
    {
        //act
        var response = await fixture.Client.GetAsync("/demo/db?select[x.FirstName.ToUpper()].take[10]");
        response.EnsureSuccessStatusCode();

        //assert
        Json(await response.Content.ReadFromJsonAsync<JsonElement>()).Should().Be(
            Json(Persons.Select(x => new { ToUpper = x.FirstName.ToUpper() }).Take(10).ToList()));
    }

    [Fact]
    public async Task WhenSelectCallWithWrittenArguments_ThenTheArgumentsReachTheServer()
    {
        //act
        var response = await fixture.Client.GetAsync("/demo/db?select[R=x.FirstName.Substring(0,2)].take[10]");
        response.EnsureSuccessStatusCode();

        //assert
        Json(await response.Content.ReadFromJsonAsync<JsonElement>()).Should().Be(
            Json(Persons.Select(x => new { R = x.FirstName.Substring(0, 2) }).Take(10).ToList()));
    }

    private static string Json(object value) =>
        JsonSerializer.Serialize(value, new JsonSerializerOptions(JsonSerializerDefaults.Web));
}

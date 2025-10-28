using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;

namespace HamsterWheel.HLinq.IntegrationTests;

partial class DbDataTests
{
    [Fact]
    public async Task WhenUnknownTokens_ThenReturns400()
    {
        //act
        var query = "foo[]";
        var response = await fixture.Client.GetAsync($"/demo/db?{query}");

        //assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = (await response.Content.ReadFromJsonAsync<ProblemDetails>()).Should().NotBeNull()
            .And.BeOfType<ProblemDetails>().Which;
        error.Status.Should().Be(400);
        error.Title.Should().MatchEquivalentOf($"HLinq query '{query}' is invalid *");
    }

    [Fact]
    public async Task WhenInvalidPropertyPath_ThenReturns400()
    {
        //act
        var query = "select[x.x.x]";
        var response = await fixture.Client.GetAsync($"/demo/db?{query}");

        //assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = (await response.Content.ReadFromJsonAsync<ProblemDetails>()).Should().NotBeNull()
            .And.BeOfType<ProblemDetails>().Which;
        error.Status.Should().Be(400);
        error.Title.Should().MatchEquivalentOf("Invalid property path 'x.x' for entity *");
    }
}
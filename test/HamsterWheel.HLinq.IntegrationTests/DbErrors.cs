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

    /// <summary>
    /// A call of one argument more than its word takes is a query there is no answer to, not a call over
    /// the arguments that happened to have a slot - and the refusal says what was written and what the word
    /// takes.
    /// </summary>
    [Fact]
    public async Task WhenFunctionCalledWithMoreArgumentsThanItTakes_ThenReturns400()
    {
        //act
        var response = await fixture.Client
            .GetAsync("/demo/db?where[TrigramsAreSimilar(x.FirstName, x.LastName, x.Email)]");

        //assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = (await response.Content.ReadFromJsonAsync<ProblemDetails>()).Should().NotBeNull()
            .And.BeOfType<ProblemDetails>().Which;
        error.Status.Should().Be(400);
        error.Title.Should().MatchEquivalentOf("Invalid static method in query 'TrigramsAreSimilar'*")
            .And.MatchEquivalentOf(
                "*Query provided following argument(s): FirstName, LastName, Email*TrigramsAreSimilar(String, String)*");
    }
}
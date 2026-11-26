using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;

namespace HamsterWheel.HLinq.IntegrationTests;

partial class DbDataTests
{
    [Fact]
    public async Task WhenJoinedMembers_ThenServerJoinsTheColumns()
    {
        //act
        //cannot carry does: the decoding of the wire turns a bare '+' into a space
        var response = await fixture.Client.GetAsync(
            "/demo/db?select[FullName=x.FirstName%2B%22%20%22%2Bx.LastName].take[10]");
        response.EnsureSuccessStatusCode();

        //assert
        Json(await response.Content.ReadFromJsonAsync<JsonElement>()).Should().Be(
            Json(Persons.Select(x => new { FullName = x.FirstName + " " + x.LastName }).Take(10).ToList()));
    }

    [Fact]
    public async Task WhenJoinedWithCall_ThenServerJoinsAndCallsInOneStatement()
    {
        //act
        var response = await fixture.Client.GetAsync(
            "/demo/db?select[U=x.FirstName.ToUpper()%2B%22!%22%2Bx.LastName].take[10]");
        response.EnsureSuccessStatusCode();

        //assert
        Json(await response.Content.ReadFromJsonAsync<JsonElement>()).Should().Be(
            Json(Persons.Select(x => new { U = x.FirstName.ToUpper() + "!" + x.LastName }).Take(10)
                .ToList()));
    }

    [Fact]
    public async Task WhenUnnamedJoin_ThenReturns400WithTheReason()
    {
        //act
        var response = await fixture.Client.GetAsync(
            "/demo/db?select[x.FirstName%2B%22%20%22%2Bx.LastName].take[1]");

        //assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = (await response.Content.ReadFromJsonAsync<ProblemDetails>()).Should().NotBeNull()
            .And.BeOfType<ProblemDetails>().Which;
        error.Status.Should().Be(400);
    }
}

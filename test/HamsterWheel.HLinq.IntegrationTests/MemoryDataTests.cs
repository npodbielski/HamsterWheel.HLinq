using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using HamsterWheel.HLinq.IntegrationTests.Fixtures;
using HamsterWheel.HLinq.Pipeline.Tokenizer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace HamsterWheel.HLinq.IntegrationTests;

partial class MemoryDataTests
{
    [Fact]
    public void WhenDemoGrammarValidated_ThenNoProblems()
    {
        //the demo app runs a localized, extended grammar - the self-check must
        //pass for the effective (as-configured) token set, not just the default one
        var grammar = fixture.DemoTestHost.Services.GetRequiredService<GrammarValidator>();

        var act = grammar.Validate;

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("where[>3]")]
    [InlineData("where[x.Int > 3 >]")]
    [InlineData("take[abc]")]
    [InlineData("skip[]extra")]
    [InlineData("select[Name]]")]
    [InlineData("orderBy[x][y]")]
    [InlineData("select[x.Name==test]")]
    [InlineData("where[1=1]")]
    [InlineData("where[x.FirstName==x.Id]")]
    public async Task WhenMalformedQuery_ThenReturns400(string query)
    {
        //act
        var response = await fixture.Client.GetAsync($"/demo/memory?{query}");

        //assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = (await response.Content.ReadFromJsonAsync<ProblemDetails>()).Should().NotBeNull()
            .And.BeOfType<ProblemDetails>().Which;
        error.Status.Should().Be(400);
    }
}

[Collection(nameof(IntegrationCollectionDefinition))]
public partial class MemoryDataTests(DemoFixture fixture);
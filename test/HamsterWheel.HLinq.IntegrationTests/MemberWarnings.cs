using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using HamsterWheel.HLinq.Client;
using HamsterWheel.HLinq.Demo.Data;
using HamsterWheel.HLinq.IntegrationTests.Fixtures;
using Microsoft.Extensions.Logging;

namespace HamsterWheel.HLinq.IntegrationTests;

/// <summary>
/// A query whose name two members answer to is answered by one of them, and the response says which -
/// a query that spelled one of them exactly is answered the same way and says nothing about it.
/// </summary>
[Collection(nameof(IntegrationCollectionDefinition))]
public class MemberWarningsTests(DemoFixture fixture)
{
    [Fact]
    public async Task WhenTwoMembersAnswerTheNameOfTheQuery_ThenTheResponseNamesTheOneItGot()
    {
        //act
        var response = await fixture.Client.GetAsync("/demo/sidekicks?where[x.name==Bucky]");

        //assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.GetHeaderWarnings().Single().Should().Be(
            "reason=\"ambiguous-name\", member=\"NAME\", type=\"HamsterWheel.HLinq.Demo.Data.Sidekick\", " +
            "competitors=\"NAME, Name\", query=\"where[x.name==Bucky]\"");
        (await response.Content.ReadFromJsonAsync<Sidekick[]>() ?? [])
            .Select(s => s.Name).Should().BeEquivalentTo(["Bucky"]);
    }

    [Fact]
    public async Task WhenAMemberIsSpelledAsTheQuerySpelledIt_ThenTheResponseSaysNothingAboutTheChoice()
    {
        //act
        var response = await fixture.Client.GetAsync("/demo/sidekicks?where[x.Name==Bucky]");

        //assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.GetHeaderWarnings().Should().BeEmpty();
    }

    [Fact]
    public async Task WhenANameOfTheQueryBelongsToASingleMember_ThenTheResponseSaysNothingAboutTheChoice()
    {
        //act
        var response = await fixture.Client.GetAsync("/demo/sidekicks?select[x.Name]");

        //assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.GetHeaderWarnings().Should().BeEmpty();
    }

    [Fact]
    public async Task WhenTheClientAsksAnAmbiguousQuestion_ThenItRepeatsWhatTheServerSaidAboutIt()
    {
        //arrange
        var log = new RecordedWarningsLog();

        //act
        var sidekicks = await fixture.Client.GetWithHLinq("/demo/sidekicks",
            q => q.For<SpelledByTheClient>().Where(x => x.name == "Bucky"), log);

        //assert
        sidekicks.Select(s => s.name).Should().BeEquivalentTo(["Bucky"]);
        log.Lines.Should().BeEquivalentTo(
            [
                "HLinq warning: reason=\"ambiguous-name\", member=\"NAME\", " +
                "type=\"HamsterWheel.HLinq.Demo.Data.Sidekick\", competitors=\"NAME, Name\", " +
                "query=\"where[x.name == Bucky]\""
            ]);
    }

    /// <summary>
    /// The client spells a name the way its own model spells it, so this is how a caller spells one that
    /// the type behind the endpoint answers twice: neither of the two spellings, and so a member chosen
    /// by an ordinal comparison.
    /// </summary>
    private class SpelledByTheClient
    {
        public string name { get; set; } = "";
    }
}

/// <summary>
/// What the client said to the operator, which is the half of a warning a test can check without
/// reading a console.
/// </summary>
internal class RecordedWarningsLog : ILogger
{
    public List<string> Lines { get; } = [];

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter) =>
        Lines.Add(formatter(state, exception));

    public bool IsEnabled(LogLevel logLevel) => true;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
}

internal static class HttpResponseWarnings
{
    /// <summary>
    /// What the response said about the choices its query made, one entry per warning line.
    /// </summary>
    public static string[] GetHeaderWarnings(this HttpResponseMessage response) =>
        response.Headers.TryGetValues("X-HLinq-Warning", out var values)
            ? [.. values]
            : [];
}

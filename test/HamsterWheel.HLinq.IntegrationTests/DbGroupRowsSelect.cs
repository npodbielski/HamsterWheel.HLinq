using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using HamsterWheel.HLinq.Demo.Data;
using HamsterWheel.HLinq.IntegrationTests.Fixtures;
using HamsterWheel.HLinq.Pipeline.Applier;
using HamsterWheel.HLinq.Pipeline.Parser;
using HamsterWheel.HLinq.Pipeline.Tokenizer;
using HamsterWheel.HLinq.Request;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HamsterWheel.HLinq.IntegrationTests;

/// <summary>
/// The rows of a <c>select</c> after a <c>groupBy</c>, asked of the real Postgres.
/// </summary>
[Collection(nameof(IntegrationCollectionDefinition))]
public class DbGroupRowsSelect(DemoFixture fixture) : IAsyncLifetime
{
    private DemoContext Context { get; set; } = null!;

    private IQueryable<Person> Persons => Context.Persons;

    public async ValueTask InitializeAsync()
    {
        Context = new DemoContext(
            new DbContextOptionsBuilder<DemoContext>().UseNpgsql(fixture.ConnectionString).Options);
        await Context.EnsureDbAndData();
    }

    public async ValueTask DisposeAsync()
    {
        await Context.DisposeAsync();
    }

    //a last name shared by the seeded people, so a group has rows to shape - picked from the names
    //read into memory, the same one the group aggregate tests use
    private string SharedLastName =>
        Persons.Select(p => p.LastName).ToList()
            .GroupBy(l => l)
            .OrderByDescending(g => g.Count())
            .First().Key;

    [Fact]
    public void WhenGroupByThenRowsSelectWithAggregate_ThenServerGroupsInOneStatement()
    {
        //act
        var (sql, results) = RunAndCapture(
            "where[x.LastName==\"" + SharedLastName + "\"]" +
            ".groupBy[x.FirstName].select[C=x.Count(),Rows=x.Rows.select(a.Id,a.FirstName)]");

        //assert
        //stand beside it, so the whole answer is one statement
        sql.Should().Contain("GROUP BY");
        sql.ToLowerInvariant().Should().Contain("count(");
        sql.Split(';').Count(s => s.Trim().Length > 0).Should().Be(1);
        results.Should().NotBeEmpty();
    }

    [Fact]
    public void WhenGroupByThenRowsSelect_ThenServerIsAskedWhatTheBareMembersAnswer()
    {
        //arrange
        //members written beside the aggregates
        var explicitRows = RunAndCapture(
            "where[x.LastName==\"" + SharedLastName + "\"]" +
            ".groupBy[x.FirstName].select[C=x.Count(),Rows=x.Rows.select(a.Id)]");
        var rowMembers = RunAndCapture(
            "where[x.LastName==\"" + SharedLastName + "\"]" +
            ".groupBy[x.FirstName].select[C=x.Count(),x.Id]");

        //assert
        explicitRows.Sql.Split("ms)")[1].Should().Be(rowMembers.Sql.Split("ms)")[1]);
        JsonSerializer.Serialize(explicitRows.Results).Should().Be(JsonSerializer.Serialize(rowMembers.Results));
    }

    [Fact]
    public async Task WhenGroupByThenRowsNamedByAnAggregate_ThenReturns400WithTheReason()
    {
        //act
        //cannot take it
        var query = "groupBy[x.FirstName].select[Rows=x.Count()]";
        var response = await fixture.Client.GetAsync($"/demo/db?{query}");

        //assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = (await response.Content.ReadFromJsonAsync<ProblemDetails>()).Should().NotBeNull()
            .And.BeOfType<ProblemDetails>().Which;
        error.Status.Should().Be(400);
        error.Title.Should().MatchEquivalentOf("*already names 'Rows'*");
    }

    /// <summary>
    /// The query through the endpoint's applier, with the statement text the server was asked.
    /// </summary>
    private (string Sql, object[] Results) RunAndCapture(string query)
    {
        var recorder = new SqlRecorder();
        using var recorded = new DemoContext(
            new DbContextOptionsBuilder<DemoContext>()
                .UseNpgsql(fixture.ConnectionString)
                .UseLoggerFactory(LoggerFactory.Create(b => b.AddProvider(recorder)))
                .Options);

        //the applier takes a query that has already been parsed; parse it the way the endpoint does
        var services = fixture.DemoTestHost.Services;
        var parsed = new HLinqQuery<Person>
        {
            SourceQueryString = query
        };
        services.GetRequiredService<IHLinqParser>().Parse(parsed,
            services.GetRequiredService<IHLinqTokenizer>().Tokenize(query));

        var results = (object[])Applier(fixture).Apply(recorded.Persons, typeof(Person), parsed);

        return (string.Join("\n", recorder.Statements), results);
    }

    /// <summary>
    /// The statements the server was actually asked, in the order it was asked them.
    /// </summary>
    private sealed class SqlRecorder : ILoggerProvider
    {
        public List<string> Statements { get; } = [];

        public ILogger CreateLogger(string categoryName) => new Recorder(this);

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        private sealed class Recorder(SqlRecorder owner) : ILogger
        {
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                if (eventId.Id == RelationalEventId.CommandExecuted.Id)
                {
                    owner.Statements.Add(formatter(state, exception));
                }
            }

            public bool IsEnabled(LogLevel logLevel) => true;

            public IDisposable BeginScope<TState>(TState state) where TState : notnull => new Scope();

            private sealed class Scope : IDisposable
            {
                public void Dispose()
                {
                    GC.SuppressFinalize(this);
                }
            }
        }
    }

    private static IHLinqQueryApplier Applier(DemoFixture fixture) =>
        fixture.DemoTestHost.Services.GetRequiredService<IHLinqQueryApplier>();
}

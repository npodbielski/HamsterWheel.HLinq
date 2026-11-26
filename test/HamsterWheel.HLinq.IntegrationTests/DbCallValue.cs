using FluentAssertions;
using HamsterWheel.HLinq.Demo.Data;
using HamsterWheel.HLinq.IntegrationTests.Fixtures;
using HamsterWheel.HLinq.Pipeline.Applier;
using HamsterWheel.HLinq.Pipeline.Parser;
using HamsterWheel.HLinq.Pipeline.Tokenizer;
using HamsterWheel.HLinq.Request;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HamsterWheel.HLinq.IntegrationTests;

/// <summary>
/// What a call over a member asks of the server, in the statement that narrows the rows.
/// </summary>
[Collection(nameof(IntegrationCollectionDefinition))]
public class DbCallValue(DemoFixture fixture) : IAsyncLifetime
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

    [Fact]
    public void WhenRelatedRowsAreCountedInWhereRuns_ThenServerIsAskedOneStatementWithCount()
    {
        //act
        var (sql, results) = RunAndCapture("where[x.Addresses.Count()>1]");

        //assert
        sql.ToLowerInvariant().Should().Contain("count(");
        sql.Split(';').Count(s => s.Trim().Length > 0).Should().Be(1);
        results.Should().NotBeEmpty();
    }

    [Fact]
    public void WhenRelatedRowsAreCountedInWhere_ThenTheAnswerIsTheSameLinqWrittenByHand()
    {
        //act
        var actual = RunAndCapture("where[x.Addresses.Count()>1]").Results;

        //assert
        actual.Should().BeEquivalentTo(Persons.Where(p => p.Addresses.Count > 1));
    }

    [Fact]
    public void WhenOrderedByTheCountOfRelatedRows_ThenServerOrdersByTheCount()
    {
        //act
        var (sql, results) = RunAndCapture("orderBy[x.Addresses.Count()].take[5]");

        //assert
        sql.Should().MatchRegex(@"ORDER BY[\s\S]*count\([\s\S]*LIMIT");
        sql.Split(';').Count(s => s.Trim().Length > 0).Should().Be(1);
        results.Should().HaveCount(5);
    }

    [Fact]
    public void WhenTextIsMeasuredByItsLength_ThenServerMeasuresIt()
    {
        //act
        var (sql, results) = RunAndCapture("where[x.FirstName.Length>4]");

        //assert
        sql.ToLowerInvariant().Should().Contain("length(");
        sql.Split(';').Count(s => s.Trim().Length > 0).Should().Be(1);
        results.Should().BeEquivalentTo(Persons.Where(p => p.FirstName.Length > 4));
    }

    /// <summary>
    /// The query through the endpoint's applier, with the statement text the server was asked.
    /// </summary>
    private (string Sql, Person[] Results) RunAndCapture(string query)
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

        var results = (object[])Applier().Apply(recorded.Persons, typeof(Person), parsed);

        return (string.Join("\n", recorder.Statements), [.. results.Cast<Person>()]);
    }

    /// <summary>
    /// The statements the server was actually asked, in the order it was asked them.
    /// </summary>
    private sealed class SqlRecorder : ILoggerProvider
    {
        public List<string> Statements { get; } = [];

        public ILogger CreateLogger(string categoryName) => new Recorder(this);

        //nothing to release: the recorder only holds the list of statements it saw
        public void Dispose()
        {
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

            //the scope of a log call carries nothing, so there is nothing to leave
            private sealed class Scope : IDisposable
            {
                public void Dispose()
                {
                }
            }
        }
    }

    private IHLinqQueryApplier Applier() =>
        fixture.DemoTestHost.Services.GetRequiredService<IHLinqQueryApplier>();
}

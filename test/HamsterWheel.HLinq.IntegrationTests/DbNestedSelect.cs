using System.Linq.Expressions;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using HamsterWheel.HLinq.Data.Converters;
using HamsterWheel.HLinq.Demo.Data;
using HamsterWheel.HLinq.IntegrationTests.Fixtures;
using HamsterWheel.HLinq.Pipeline.Applier.Builders;
using HamsterWheel.HLinq.Pipeline.Parser;
using HamsterWheel.HLinq.Pipeline.Tokenizer;
using HamsterWheel.HLinq.Reflection;
using HamsterWheel.HLinq.Request;
using HamsterWheel.HLinq.Tree.Selecting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HamsterWheel.HLinq.IntegrationTests;

/// <summary>
/// A nested select over the real Postgres: the answer is what the same Linq written by hand
/// answers, the server is asked in one statement (a `LEFT JOIN`, not N+1), and the statement
/// carries the shaped columns of the related rows.
/// </summary>
[Collection(nameof(IntegrationCollectionDefinition))]
public class DbNestedSelect(DemoFixture fixture) : IAsyncLifetime
{
    //a context of its own: the Includes below fill the change tracker of the context they run
    //on, and a shared one would hand other tests entities whose navigations are loaded while
    //the HTTP answer of the same row, shaped by select, has none
    private DemoContext Context { get; set; } = null!;

    private IQueryable<Person> Persons =>
        Context.Persons.Include(x => x.Addresses).ThenInclude(a => a.Country);

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
    public async Task WhenNestedSelectOverCollection_ThenAnswersShapedListPerRow()
    {
        //act
        var response = await fixture.Client.GetAsync(
            "/demo/db?where[x.Id==1].select[x.FirstName,addresses=x.Addresses.select(a.Street,a.City)]");
        response.EnsureSuccessStatusCode();

        //assert
        var expected = Json(new[]
        {
            new
            {
                Persons.First(x => x.Id == 1).FirstName,
                addresses = Persons.First(x => x.Id == 1).Addresses
                    .Select(a => new { a.Street, a.City }).ToList()
            }
        });
        Json(await response.Content.ReadFromJsonAsync<JsonElement>()).Should().Be(expected);
    }

    [Fact]
    public void WhenNestedSelectBuilt_ThenServerIsAskedOneStatementWithLeftJoin()
    {
        //arrange
        var select = SelectLambda("select[x.FirstName,addresses=x.Addresses.select(a.Street,a.City)]");

        //act
        var sql = Project(Context.Persons, select.Expression).ToQueryString();

        //assert - one statement, the related rows joined beside the person
        sql.Should().Contain("LEFT JOIN");
        sql.Split(';').Count(s => s.Trim().Length > 0).Should().Be(1);
        sql.Should().Contain("\"Street\"");
        sql.Should().Contain("\"City\"");
        sql.Should().NotContain("SELECT COUNT", "shaping a collection is not a count of anything");
    }

    [Fact]
    public void WhenNestedSelectRuns_ThenServerIsAskedOnceNotOncePerRow()
    {
        //arrange - the same statement over a context that counts the commands it executes
        var select = SelectLambda("select[x.FirstName,addresses=x.Addresses.select(a.Street,a.City)]");
        var counter = new CommandCounter();
        using var counted = new DemoContext(CountedOptions(counter));
        var query = Project(counted.Persons, select.Expression);

        //act
        var rows = query.Cast<object>().ToList();
        rows.Should().NotBeEmpty();

        //assert - more rows than one person, and still one round trip to the server
        rows.Should().HaveCountGreaterThan(1);
        counter.Commands.Should().Be(1);
    }

    [Fact]
    public async Task WhenNestedSelectRenamed_ThenAnswersUnderTheWrittenName()
    {
        //act
        var response = await fixture.Client.GetAsync(
            "/demo/db?where[x.Id==1].select[delivery=x.Addresses.select(city=a.City)]");
        response.EnsureSuccessStatusCode();

        //assert
        var expected = Json(new[]
        {
            new
            {
                delivery = Persons.First(x => x.Id == 1).Addresses
                    .Select(a => new { city = a.City }).ToList()
            }
        });
        Json(await response.Content.ReadFromJsonAsync<JsonElement>()).Should().Be(expected);
    }

    [Fact]
    public void WhenNestedSelectOverInnerReference_ThenJoinsItInsideTheSameStatement()
    {
        //arrange
        var select = SelectLambda("select[x.Addresses.select(a.Street,country=a.Country.select(c.Name))]");

        //act
        var sql = Project(Context.Persons, select.Expression).ToQueryString();

        //assert - the country join nests in the same one statement
        sql.Should().Contain("LEFT JOIN");
        sql.Split(';').Count(s => s.Trim().Length > 0).Should().Be(1);
        sql.Should().Contain("\"Name\"");
    }

    /// <summary>
    /// The select clause through the same machinery an endpoint runs: tokenize, parse, build the
    /// projection over the entity.
    /// </summary>
    private (LambdaExpression? Expression, Type ResultType) SelectLambda(string clause)
    {
        var services = fixture.DemoTestHost.Services;
        var builder = new ExpressionBuilder(
            services.GetRequiredService<IPropertiesCache>(),
            services.GetRequiredService<IMethodsCache>(),
            services.GetRequiredService<IConverterFactory>(),
            services.GetRequiredService<IParametersConverter>(),
            [],
            services.GetRequiredService<IWrittenValueParser>(), null!);
        var parser = services.GetRequiredService<IHLinqParser>();
        var tokens = services.GetRequiredService<IHLinqTokenizer>().Tokenize(clause);
        var query = new HLinqQuery<Person>
        {
            SourceQueryString = clause
        };
        parser.Parse(query, tokens);
        var root = query.Children.OfType<SelectRoot>().Single();
        return builder.GetSelect(typeof(Person), root, clause);
    }

    private static string Json(object value) =>
        JsonSerializer.Serialize(value, new JsonSerializerOptions(JsonSerializerDefaults.Web));

    /// <summary>
    /// The same Queryable.Select the applier invokes: the projection is built from the query text,
    /// so its type is only known at run time.
    /// </summary>
    private static IQueryable Project(IQueryable source, LambdaExpression selector)
    {
        var method = typeof(Queryable).GetMethods()
            .Single(m => m.Name == nameof(Queryable.Select) && m.GetParameters().Length == 2
                         && m.GetParameters()[1].ParameterType.GetGenericArguments()[0].GetGenericTypeDefinition()
                         == typeof(Func<,>))
            .MakeGenericMethod(source.ElementType, selector.ReturnType);
        return (IQueryable)method.Invoke(null, [source, selector])!;
    }

    private DbContextOptions<DemoContext> CountedOptions(CommandCounter counter) =>
        new DbContextOptionsBuilder<DemoContext>()
            .UseNpgsql(fixture.ConnectionString)
            .UseLoggerFactory(LoggerFactory.Create(b => b.AddProvider(counter)))
            .Options;

    /// <summary>
    /// The number of statements the server was actually asked - the count that distinguishes one
    /// joined statement from N+1 queries.
    /// </summary>
    private sealed class CommandCounter : ILoggerProvider
    {
        public int Commands { get; private set; }

        public ILogger CreateLogger(string categoryName) => new Counter(this);

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        private sealed class Counter(CommandCounter owner) : ILogger
        {
            public void Log<TState>(LogLevel logLevel,
                EventId eventId, TState state, Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                if (eventId.Id == RelationalEventId.CommandExecuted.Id)
                {
                    owner.Commands++;
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
}

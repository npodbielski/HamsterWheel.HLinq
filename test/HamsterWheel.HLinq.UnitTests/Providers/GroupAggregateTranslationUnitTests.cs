using System.Linq.Expressions;
using FluentAssertions;
using HamsterWheel.HLinq.Diagnostics;
using HamsterWheel.HLinq.Pipeline.Applier;
using HamsterWheel.HLinq.Pipeline.Applier.Builders;
using HamsterWheel.HLinq.Pipeline.Parser;
using HamsterWheel.HLinq.Request;
using HamsterWheel.HLinq.Tree.Selecting;
using HamsterWheel.HLinq.UnitTests.TestUtils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HamsterWheel.HLinq.UnitTests.Providers;

/// <summary>
/// What a grouping with aggregates asks of the engine: the aggregate is computed by the server in
/// the grouped subquery - a <c>GROUP BY</c> with <c>HAVING</c> and <c>ORDER BY agg</c> - never
/// client-side. The grouped queries below are query text through the whole pipeline - tokenizer,
/// parser, appliers - stopped at the statement the provider would be asked, in every provider
/// pack; the Postgres integration suite repeats the check against a live server.
/// </summary>
public class GroupAggregateTranslationUnitTests
{
    private static readonly TestServicesCollection Services = new();

    [Theory]
    [InlineData("npgsql")]
    [InlineData("sqlite")]
    [InlineData("mysql")]
    public void CountOfGroup_WhenTranslated_ThenServerCountsInGroupBy(string engine)
    {
        using var context = ContextOf(engine);

        var sql = ServerQueryOf(context, "groupBy[x.Name].select[C=x.Count()]").ToQueryString();

        sql.Should().Contain("GROUP BY");
        sql.ToLowerInvariant().Should().Contain("count(");
    }

    [Theory]
    [InlineData("npgsql")]
    [InlineData("sqlite")]
    [InlineData("mysql")]
    public void CountPredicate_WhenTranslated_ThenServerAnswersHaving(string engine)
    {
        using var context = ContextOf(engine);

        //the predicate stands on the grouping itself, so the server answers it as HAVING
        var sql = ServerQueryOf(context, "groupBy[x.Name].where[x.Count()>2]").ToQueryString();

        sql.Should().Contain("GROUP BY");
        sql.Should().Contain("HAVING");
    }

    [Theory]
    [InlineData("npgsql")]
    [InlineData("sqlite")]
    [InlineData("mysql")]
    public void AggregateOrder_WhenTranslated_ThenServerOrdersByTheAggregate(string engine)
    {
        using var context = ContextOf(engine);

        var sql = ServerQueryOf(context, "groupBy[x.Name].orderBy[x.Max(x.Int)]").ToQueryString();

        sql.Should().Contain("GROUP BY");
        sql.ToLowerInvariant().Should().Contain("max(");
        sql.ToLowerInvariant().Should().Contain("order by");
    }

    [Theory]
    [InlineData("npgsql")]
    [InlineData("sqlite")]
    [InlineData("mysql")]
    public void NavigationCount_WhenTranslated_ThenServerCountsRelatedRowsInTheSameStatement(string engine)
    {
        using var context = ContextOf(engine);

        var selector = SelectLambda("select[N=x.Children.Count()]");
        var method = typeof(Queryable).GetMethods()
            .Single(m => m.Name == nameof(Queryable.Select) && m.GetParameters().Length == 2
                                                            && m.GetParameters()[1].ParameterType.GetGenericArguments()[
                                                                    0]
                                                                .GetGenericTypeDefinition() == typeof(Func<,>))
            .MakeGenericMethod(typeof(Owner), selector.ReturnType);

        var sql = ((IQueryable)method.Invoke(null, [context.Set<Owner>(), selector])!)
            .ToQueryString();

        sql.Split(';').Count(s => s.Trim().Length > 0).Should().Be(1);
        sql.ToLowerInvariant().Should().Contain("count(");
    }

    [Fact]
    public void BareMaxOfNumberList_WhenTranslated_ThenServerAnswersMax()
    {
        using var context = ContextOf("npgsql");
        var sql = context.Set<Owner>()
            .Select(o => new { o.Name, H = o.Prices.Max() })
            .ToQueryString();

        sql.ToLowerInvariant().Should().Contain("max(");
        sql.Split(';').Count(s => s.Trim().Length > 0).Should().Be(1);
    }

    /// <summary>
    /// The query an HLinq query text becomes, before anyone enumerates it: every clause runs
    /// through the appliers the endpoint runs and the grouping's projection stands at the end, so
    /// <c>ToQueryString</c> shows exactly the statement the server would be asked. The applier
    /// itself cannot be used here - it materializes, and these contexts have no server.
    /// </summary>
    private IQueryable ServerQueryOf(DbContext context, string query)
    {
        var hlinqQuery = new HLinqQuery<Owner>
        {
            SourceQueryString = query
        };
        new HLinqParser(Services.Parsers).Parse(hlinqQuery, Services.Tokenizer.Tokenize(query));

        var appliers = Services.Provider.GetRequiredService<IElementApplierFactory>();
        IQueryableContext state = new QueryableContext(context.Set<Owner>(), typeof(Owner));
        foreach (var root in hlinqQuery.Children.OfType<ITreeBranch>())
        {
            state = appliers.Get(root).Apply(state, root, query);
        }

        if (state.Grouping is not { } grouping)
        {
            return state.Queryable!;
        }

        var projection = Services.Provider.GetRequiredService<IExpressionBuilder>().GroupProjection(grouping);
        var select = Services.MethodsCache.GetStaticGeneric(typeof(Queryable), nameof(Queryable.Select),
            infos => infos[1].ParameterType.IsGenericType &&
                     infos[1].ParameterType.GenericTypeArguments[0].GetGenericTypeDefinition() ==
                     typeof(Func<,>), grouping.GroupingItemType, projection.ResultType);
        return (IQueryable)select.Invoke(null, [state.Queryable!, projection.Expression])!;
    }

    private static LambdaExpression SelectLambda(string clause)
    {
        var builder = Builder();
        var tokens = Services.Tokenizer.Tokenize(clause);
        var tree = new HLinqParser(Services.Parsers).TestParseEntryPoint<Owner>(clause, tokens);
        return builder.GetSelect(typeof(Owner), tree.GetAll<SelectRoot>().First(), clause,
            warnings: new HLinqWarnings()).Expression;
    }

    /// <summary>
    /// The shape the navigation count walks: a row and the collection under it, with the members
    /// the query text names (`Children`, and `Name` for the other measurements).
    /// </summary>
    public class Owner
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public int Int { get; set; }
        public List<int> Prices { get; set; } = [];
        public List<Child> Children { get; set; } = [];
    }

    public class Child
    {
        public int Id { get; set; }
        public int OwnerId { get; set; }
    }

    private static DbContext ContextOf(string engine) => engine switch
    {
        "npgsql" => new PgContext(),
        "sqlite" => new SqliteContext(),
        "mysql" => new MySqlContext(),
        _ => throw new ArgumentOutOfRangeException(nameof(engine))
    };

    private abstract class TestContext : DbContext
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Owner>().HasMany(o => o.Children).WithOne()
                .HasForeignKey(c => c.OwnerId);
        }
    }

    private sealed class PgContext : TestContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder options) =>
            options.UseNpgsql("Host=localhost;Database=nowhere;Username=u;Password=p");
    }

    private sealed class SqliteContext : TestContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder options) =>
            options.UseSqlite("Data Source=:memory:");
    }

    private sealed class MySqlContext : TestContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder options) =>
            options.UseMySql("host=localhost;database=nowhere;uid=u;pwd=p",
                new MySqlServerVersion(new Version(8, 0, 0)));
    }

    private static ExpressionBuilder Builder() =>
        new(Services.PropertiesCache, Services.MethodsCache, Services.ConverterFactory,
            new ParametersConverter(Services.WrittenValues, Services.DefaultConverter),
            [], Services.WrittenValues, null!);
}
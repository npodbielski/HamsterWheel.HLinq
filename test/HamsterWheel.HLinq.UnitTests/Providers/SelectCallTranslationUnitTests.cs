using FluentAssertions;
using HamsterWheel.HLinq.Diagnostics;
using HamsterWheel.HLinq.Pipeline.Applier.Builders;
using HamsterWheel.HLinq.Pipeline.Parser;
using HamsterWheel.HLinq.Tree.Selecting;
using HamsterWheel.HLinq.UnitTests.TestUtils;
using HamsterWheel.HLinq.Pipeline.Applier;
using HamsterWheel.HLinq.Request;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HamsterWheel.HLinq.UnitTests.Providers;

/// <summary>
/// A call written in select is the same expression a hand-written Linq projection writes, so each
/// engine's own translator answers it - the projection built from the query text is asked what SQL
/// it would produce, in every provider pack, stopped at <c>ToQueryString</c> exactly as the group
/// aggregate tests do; the Postgres integration suite repeats the check against a live server.
/// </summary>
public class SelectCallTranslationUnitTests
{
    private static readonly TestServicesCollection Services = new();

    [Theory]
    [InlineData("npgsql")]
    [InlineData("sqlite")]
    [InlineData("mysql")]
    public void ToUpper_WhenTranslated_ThenServerAnswersTheCall(string engine)
    {
        using DbContext context = ContextOf(engine);

        var sql = Sql(context, "select[U=x.Name.ToUpper()]");

        sql.ToLowerInvariant().Should().Contain("upper(");
    }

    [Theory]
    [InlineData("npgsql")]
    [InlineData("sqlite")]
    [InlineData("mysql")]
    public void SubstringWithWrittenArguments_WhenTranslated_ThenArgumentsRideIntoTheSql(string engine)
    {
        using DbContext context = ContextOf(engine);

        var sql = Sql(context, "select[R=x.Name.Substring(0,2)]");

        sql.ToLowerInvariant().Should().Contain("substr");
        sql.Should().Contain("2");
    }

    [Theory]
    [InlineData("npgsql")]
    [InlineData("sqlite")]
    [InlineData("mysql")]
    public void CallInsideNestedSelect_WhenTranslated_ThenOneStatementWithTheCallInTheJoin(string engine)
    {
        using DbContext context = ContextOf(engine);

        var sql = Sql(context, "select[A=x.Addresses.select(City=a.City.ToUpper())]");

        sql.Should().Contain("LEFT JOIN");
        sql.ToLowerInvariant().Should().Contain("upper(");
        sql.Split(';').Count(s => s.Trim().Length > 0).Should().Be(1);
    }

    [Fact]
    public void CallBesideAggregateOfGroup_WhenTranslated_ThenBothAnswerTheServerStatement()
    {
        using DbContext context = ContextOf("npgsql");

        var sql = GroupedSql(context,
            "groupBy[x.Name].select[x.Key,C=x.Count(),U=x.Name.ToUpper()]").ToQueryString();

        sql.Should().Contain("GROUP BY");
        sql.ToLowerInvariant().Should().Contain("count(");
        sql.ToLowerInvariant().Should().Contain("upper(");
    }

    /// <summary>
    /// The whole pipeline through the appliers the endpoint runs, stopped at the grouped statement
    /// the provider would be asked - the group aggregate tests' shape, with a call among the
    /// group's members.
    /// </summary>
    private static IQueryable GroupedSql(DbContext context, string query)
    {
        var hlinqQuery = new HLinqQuery<Row>
        {
            SourceQueryString = query
        };
        new HLinqParser(Services.Parsers).Parse(hlinqQuery, Services.Tokenizer.Tokenize(query));

        var appliers = Services.Provider.GetRequiredService<IElementApplierFactory>();
        IQueryableContext state = new QueryableContext(context.Set<Row>(), typeof(Row));
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

    private static DbContext ContextOf(string engine) => engine switch
    {
        "npgsql" => new PgContext(),
        "sqlite" => new SqliteContext(),
        "mysql" => new MySqlContext(),
        _ => throw new ArgumentOutOfRangeException(nameof(engine))
    };

    private static string Sql(DbContext context, string query)
    {
        var builder = new ExpressionBuilder(Services.PropertiesCache, Services.MethodsCache,
            Services.ConverterFactory,
            new ParametersConverter(Services.WrittenValues, Services.DefaultConverter),
            [], Services.WrittenValues, null!);
        var tokens = Services.Tokenizer.Tokenize(query);
        var tree = new HLinqParser(Services.Parsers).TestParseEntryPoint<Row>(query, tokens);
        var selector = builder.GetSelect(typeof(Row),
            tree.GetAll<SelectRoot>().First(), query, warnings: new HLinqWarnings()).Expression;

        var source = (IQueryable)context.Set<Row>();
        var method = typeof(Queryable).GetMethods()
            .Single(m => m.Name == nameof(Queryable.Select) && m.GetParameters().Length == 2
                                                            && m.GetParameters()[1].ParameterType.GetGenericArguments()[
                                                                    0]
                                                                .GetGenericTypeDefinition() == typeof(Func<,>))
            .MakeGenericMethod(typeof(Row), selector.ReturnType);
        return ((IQueryable)method.Invoke(null, [source, selector])!).ToQueryString();
    }

    /// <summary>
    /// The row shape of the provider measurements: a text column to call on and a collection to
    /// call inside, the same one the nested select and group aggregate translations walk.
    /// </summary>
    public class Row
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public List<Related> Addresses { get; set; } = [];
    }

    public class Related
    {
        public int Id { get; set; }
        public string City { get; set; } = null!;
    }

    private abstract class TestContext : DbContext
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder) =>
            modelBuilder.Entity<Row>().HasMany(r => r.Addresses).WithOne().HasForeignKey("OwnerRowId");
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
}

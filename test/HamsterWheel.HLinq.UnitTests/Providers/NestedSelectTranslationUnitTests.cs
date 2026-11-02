using System.Linq.Expressions;
using FluentAssertions;
using HamsterWheel.HLinq.Diagnostics;
using HamsterWheel.HLinq.Pipeline.Applier.Builders;
using HamsterWheel.HLinq.Pipeline.Parser;
using HamsterWheel.HLinq.Tree.Selecting;
using HamsterWheel.HLinq.UnitTests.TestUtils;
using Microsoft.EntityFrameworkCore;

namespace HamsterWheel.HLinq.UnitTests.Providers;

/// <summary>
/// The shape a nested select asks of the engine, measured in each provider pack: one statement,
/// the related rows joined beside the row. The projection built from the query text is asked of
/// each engine's translator what SQL it would produce - the assertion the Postgres integration
/// suite makes against a live server, asked of every engine here without one.
/// </summary>
public class NestedSelectTranslationUnitTests
{
    private static readonly TestServicesCollection Services = new();

    [Theory]
    [InlineData("npgsql")]
    [InlineData("sqlite")]
    [InlineData("mysql")]
    public void CollectionProjection_WhenTranslated_ThenOneStatementWithLeftJoin(string engine)
    {
        using DbContext context = ContextOf(engine);

        var sql = Sql(context, "select[x.Name,addresses=x.Addresses.select(a.Street,a.City)]");

        sql.Should().Contain("LEFT JOIN");
        sql.Should().Contain("Street");
        sql.Should().Contain("City");
        sql.Split(';').Count(s => s.Trim().Length > 0).Should().Be(1);
    }

    [Theory]
    [InlineData("npgsql")]
    [InlineData("sqlite")]
    [InlineData("mysql")]
    public void ReferenceProjection_WhenTranslated_ThenOneStatementWithLeftJoin(string engine)
    {
        using DbContext context = ContextOf(engine);

        var sql = Sql(context, "select[x.Home.select(a.Street,a.City)]");

        sql.Should().Contain("LEFT JOIN");
        sql.Split(';').Count(s => s.Trim().Length > 0).Should().Be(1);
    }

    [Theory]
    [InlineData("npgsql")]
    [InlineData("sqlite")]
    [InlineData("mysql")]
    public void CallUnderCall_WhenTranslated_ThenStillOneStatement(string engine)
    {
        using DbContext context = ContextOf(engine);

        var sql = Sql(context, "select[x.Addresses.select(a.Street,country=a.Country.select(c.Name))]");

        sql.Should().Contain("LEFT JOIN");
        sql.Split(';').Count(s => s.Trim().Length > 0).Should().Be(1);
    }

    [Theory]
    [InlineData("npgsql")]
    [InlineData("sqlite")]
    [InlineData("mysql")]
    public void OuterCapture_WhenTranslated_ThenEngineDecidesWhetherTheColumnRidesIn(string engine)
    {
        using DbContext context = ContextOf(engine);

        //the captured outer column rides into the join subquery on Postgres (a LATERAL join, as
        //the first measurement showed) - but the engines without that operation refuse the very
        //same statement: the limit is the engine's, and the query fails as any untranslatable
        //query does, not as a silent client-side evaluation
        var translate = () => Sql(context,
            "select[x.Name,addresses=x.Addresses.select(contact=x.Name,a.City)]");

        if (engine == "npgsql")
        {
            var sql = translate();
            sql.Should().Contain("LEFT JOIN");
            sql.Split(';').Count(s => s.Trim().Length > 0).Should().Be(1);
        }
        else
        {
            translate.Should().Throw<InvalidOperationException>();
        }
    }

    private static DbContext ContextOf(string engine) => engine switch
    {
        "npgsql" => new PgContext(),
        "sqlite" => new SqliteContext(),
        "mysql" => new MySqlContext(),
        _ => throw new ArgumentOutOfRangeException(nameof(engine))
    };

    /// <summary>
    /// A row with a collection of related rows and one single related row, and a reference under
    /// the related row - the shape the three measurements walk. Written for the translator: keys
    /// and foreign keys as an engine would declare them, nothing else.
    /// </summary>
    public class Row
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public int Age { get; set; }
        public List<Related> Addresses { get; set; } = [];
        public Related? Home { get; set; }
    }

    public class Related
    {
        public int Id { get; set; }
        public string Street { get; set; } = null!;
        public string City { get; set; } = null!;
        public Nation? Country { get; set; }
    }

    public class Nation
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
    }

    private abstract class TestContext : DbContext
    {
        public DbSet<Row> Rows => Set<Row>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //the collection and the reference point at rows of the same type, so the reference
            //is its own relationship with its own key on the row
            modelBuilder.Entity<Row>().HasMany(r => r.Addresses).WithOne().HasForeignKey("OwnerRowId");
            modelBuilder.Entity<Row>().HasOne(r => r.Home).WithOne().HasForeignKey<Row>("HomeId");
            modelBuilder.Entity<Related>().HasOne(a => a.Country).WithMany();
        }
    }

    private static LambdaExpression Select(string query)
    {
        var builder = new ExpressionBuilder(Services.PropertiesCache, Services.MethodsCache,
            Services.ConverterFactory,
            new ParametersConverter(Services.WrittenValues, Services.DefaultConverter),
            [], Services.WrittenValues, null!);
        var tokens = Services.Tokenizer.Tokenize(query);
        var tree = new HLinqParser(Services.Parsers).TestParseEntryPoint<Row>(query, tokens);
        return builder.GetSelect(typeof(Row),
            tree.GetAll<SelectRoot>().First(), query, warnings: new HLinqWarnings()).Expression;
    }

    private static string Sql(DbContext context, string query)
    {
        var source = (IQueryable)context.Set<Row>();
        var selector = Select(query);
        var method = typeof(Queryable).GetMethods()
            .Single(m => m.Name == nameof(Queryable.Select) && m.GetParameters().Length == 2
                                                            && m.GetParameters()[1].ParameterType.GetGenericArguments()[
                                                                    0]
                                                                .GetGenericTypeDefinition() == typeof(Func<,>))
            .MakeGenericMethod(typeof(Row), selector.ReturnType);
        return ((IQueryable)method.Invoke(null, [source, selector])!).ToQueryString();
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
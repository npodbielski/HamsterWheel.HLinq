using FluentAssertions;
using HamsterWheel.HLinq.Pipeline.Applier;
using HamsterWheel.HLinq.Pipeline.Parser;
using HamsterWheel.HLinq.Request;
using HamsterWheel.HLinq.UnitTests.TestUtils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HamsterWheel.HLinq.UnitTests.Providers;

/// <summary>
/// A call over a member is answered by the server in the same statement, in every pack.
/// </summary>
public class CallValueTranslationUnitTests
{
    private static readonly TestServicesCollection Services = new();

    [Theory]
    [InlineData("npgsql")]
    [InlineData("sqlite")]
    [InlineData("mysql")]
    public void CountOfARelatedCollection_WhenComparedWithAValue_ThenServerCountsInTheSameStatement(
        string engine)
    {
        using var context = ContextOf(engine);

        var sql = ServerQueryOf(context, "where[x.Children.Count()>1]").ToQueryString();

        //assert
        sql.Split(';').Count(s => s.Trim().Length > 0).Should().Be(1);
        sql.ToLowerInvariant().Should().Contain("count(");
    }

    [Theory]
    [InlineData("npgsql")]
    [InlineData("sqlite")]
    [InlineData("mysql")]
    public void CountOfARelatedCollection_WhenRowsAreOrderedByIt_ThenServerCountsInTheOrder(
        string engine)
    {
        using var context = ContextOf(engine);

        var sql = ServerQueryOf(context, "orderBy[x.Children.Count()]").ToQueryString();

        sql.ToLowerInvariant().Should().Contain("order by");
        sql.ToLowerInvariant().Should().Contain("count(");
    }

    [Theory]
    [InlineData("npgsql")]
    [InlineData("sqlite")]
    [InlineData("mysql")]
    public void LengthOfText_WhenComparedWithAValue_ThenServerMeasuresItInTheSameStatement(string engine)
    {
        using var context = ContextOf(engine);

        var sql = ServerQueryOf(context, "where[x.Name.Length>1]").ToQueryString();

        sql.Split(';').Count(s => s.Trim().Length > 0).Should().Be(1);
        sql.ToLowerInvariant().Should().MatchRegex("length\\(");
    }

    [Theory]
    [InlineData("npgsql")]
    [InlineData("sqlite")]
    [InlineData("mysql")]
    public void CountOfARelatedCollection_WhenComparedWithAMember_ThenServerAnswersBothSides(
        string engine)
    {
        using var context = ContextOf(engine);

        var sql = ServerQueryOf(context, "where[x.Children.Count()>x.Int]").ToQueryString();

        sql.Split(';').Count(s => s.Trim().Length > 0).Should().Be(1);
        sql.ToLowerInvariant().Should().Contain("count(");
    }

    /// <summary>
    /// The statement the server would be asked, before anyone enumerates it.
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

        return state.Queryable!;
    }

    /// <summary>
    /// The shape the queries walk: a row and the collection under it.
    /// </summary>
    public class Owner
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public int Int { get; set; }
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
}

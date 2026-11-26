using FluentAssertions;
using HamsterWheel.HLinq.Pipeline.Applier.Builders;
using HamsterWheel.HLinq.Pipeline.Parser;
using HamsterWheel.HLinq.Tree.Selecting;
using HamsterWheel.HLinq.UnitTests.TestUtils;
using Microsoft.EntityFrameworkCore;

namespace HamsterWheel.HLinq.UnitTests.Providers;

/// <summary>
/// A join in select answers as the hand-written projection, in every provider pack.
/// </summary>
public class SelectMathTranslationUnitTests
{
    private static readonly TestServicesCollection Services = new();

    [Theory]
    [InlineData("npgsql")]
    [InlineData("sqlite")]
    [InlineData("mysql")]
    public void JoinedMembers_WhenTranslated_ThenServerJoinsTheColumns(string engine)
    {
        using DbContext context = ContextOf(engine);

        var sql = Sql(context, "select[FullName=x.Name+\" \"+x.Surname]");

        //each engine spells the concatenation its own way (|| for npgsql and sqlite, CONCAT for
        //mysql), but every one of them joins the two columns and the text between them in one
        //expression answering the written name
        sql.Should().Contain("' '");
        sql.Should().Contain("FullName");
    }

    [Theory]
    [InlineData("npgsql")]
    [InlineData("sqlite")]
    [InlineData("mysql")]
    public void JoinedNumberWithText_WhenTranslated_ThenTheNumberTravelsAsText(string engine)
    {
        using DbContext context = ContextOf(engine);

        var sql = Sql(context, "select[N=x.Id+\"x\"]");

        //npgsql casts to text, the others cast or let CONCAT read the number - in every pack the
        //column joins and nothing else answers the row
        sql.ToLowerInvariant().Should().Contain("id");
        sql.Should().Contain("'x'");
    }

    [Theory]
    [InlineData("npgsql")]
    [InlineData("sqlite")]
    [InlineData("mysql")]
    public void AddedNumbers_WhenTranslated_ThenServerAddsThem(string engine)
    {
        using DbContext context = ContextOf(engine);

        var sql = Sql(context, "select[N=x.Id+x.Id]");

        sql.ToLowerInvariant().Should().Contain("+");
        sql.Should().NotContain("WHERE");
    }

    [Theory]
    [InlineData("npgsql")]
    [InlineData("sqlite")]
    [InlineData("mysql")]
    public void Group_WhenTranslated_ThenServerComputesItFirst(string engine)
    {
        using DbContext context = ContextOf(engine);

        //`(x.Id+1)*2` is the same statement the same expression written by hand answers: one
        //column, the group computed before the multiplication, no WHERE
        var sql = Sql(context, "select[N=(x.Id+1)*2]");

        sql.ToLowerInvariant().Should().Contain("*");
        sql.Should().NotContain("WHERE");
    }

    [Theory]
    [InlineData("npgsql")]
    [InlineData("sqlite")]
    [InlineData("mysql")]
    public void WrittenNumberAndMixedOperators_WhenTranslated_ThenServerComputesThem(string engine)
    {
        using DbContext context = ContextOf(engine);

        //the whole expression - a written number, a sum, a product and a difference - is one
        //column the server answers, precedence and all: the fold wrote it the way the compiler
        //would have nested the same expression written by hand
        var sql = Sql(context, "select[N=x.Id+x.Id*2-x.Id]");

        sql.ToLowerInvariant().Should().Contain("*");
        sql.Should().NotContain("WHERE");
    }

    [Theory]
    [InlineData("npgsql")]
    [InlineData("sqlite")]
    [InlineData("mysql")]
    public void DecimalAndDoubleSum_WhenProjected_ThenTheServerWidensBeforeItAdds(string engine)
    {
        using DbContext context = ContextOf(engine);

        //the pair a comparison meets in a double is projected as two doubles too: npgsql spells
        //the cast ::double precision, sqlite CAST AS REAL, mysql widens the decimal by 0e0; the
        //sum of a decimal kept a decimal reaches sqlite as its own text-backed `ef_add`
        var sql = Sql(context, "select[N=x.Decimal+x.Double]").ToLowerInvariant();

        sql.Should().Contain("decimal");
        sql.Should().Contain("double");
        sql.Should().Contain("+");
        sql.Should().NotContain("where");
        sql.Split(';').Count(s => s.Trim().Length > 0).Should().Be(1);
    }

    [Fact]
    public void JoinedCall_WhenTranslated_ThenCallAndJoinShareTheStatement()
    {
        using DbContext context = ContextOf("npgsql");

        var sql = Sql(context, "select[FullName=x.Name+\" \"+x.Name.ToUpper()]");

        sql.ToLowerInvariant().Should().Contain("upper(");
        sql.ToLowerInvariant().Should().Contain("||");
    }

    [Fact]
    public void JoinInsideNestedSelect_WhenTranslated_ThenOneStatementWithTheJoinInTheJoin()
    {
        using DbContext context = ContextOf("npgsql");

        var sql = Sql(context, "select[A=x.Addresses.select(City=a.City+\" \"+x.Name)]");

        sql.Should().Contain("LEFT JOIN");
        sql.ToLowerInvariant().Should().Contain("||");
        sql.Split(';').Count(s => s.Trim().Length > 0).Should().Be(1);
    }

    [Theory]
    [InlineData("npgsql")]
    [InlineData("sqlite")]
    [InlineData("mysql")]
    public void UintAndByteSum_WhenProjected_ThenTheServerAddsInTheCoveringNumber(string engine)
    {
        using DbContext context = ContextOf(engine);

        //the pair meets in the uint that covers both widths - npgsql reads the uint column as
        //a bigint and widens the byte to it, sqlite casts the byte to its integer affinity,
        //mysql casts the byte unsigned
        var sql = Sql(context, "select[N=x.Uint+x.Byte]").ToLowerInvariant();

        sql.Should().Contain("uint");
        sql.Should().Contain("byte");
        sql.Should().Contain("+");
        sql.Should().NotContain("where");
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
            tree.GetAll<SelectRoot>().First(), query).Expression;

        var source = (IQueryable)context.Set<Row>();
        var method = typeof(Queryable).GetMethods()
            .Single(m => m.Name == nameof(Queryable.Select) && m.GetParameters().Length == 2
                                                            && m.GetParameters()[1].ParameterType.GetGenericArguments()[0]
                                                                .GetGenericTypeDefinition() == typeof(Func<,>))
            .MakeGenericMethod(typeof(Row), selector.ReturnType);
        return ((IQueryable)method.Invoke(null, [source, selector])!).ToQueryString();
    }

    /// <summary>
    /// The row shape of the provider measurements, with a number to add.
    /// </summary>
    public class Row
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Surname { get; set; } = null!;
        public decimal Decimal { get; set; }
        public double Double { get; set; }
        public byte Byte { get; set; }
        public uint Uint { get; set; }
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

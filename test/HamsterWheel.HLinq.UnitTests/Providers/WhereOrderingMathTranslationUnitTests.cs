using System.Linq.Expressions;
using FluentAssertions;
using HamsterWheel.HLinq.Pipeline.Applier.Builders;
using HamsterWheel.HLinq.Pipeline.Parser;
using HamsterWheel.HLinq.Tree.Ordering;
using HamsterWheel.HLinq.Tree.Selecting;
using HamsterWheel.HLinq.Tree.Filtering;
using HamsterWheel.HLinq.UnitTests.TestUtils;
using Microsoft.EntityFrameworkCore;

namespace HamsterWheel.HLinq.UnitTests.Providers;

/// <summary>
/// The statement each provider pack is asked for a computed filter or ordering key.
/// </summary>
public class WhereOrderingMathTranslationUnitTests
{
    private static readonly TestServicesCollection Services = new();

    [Theory]
    [InlineData("npgsql")]
    [InlineData("sqlite")]
    [InlineData("mysql")]
    public void DifferenceInFilter_WhenTranslated_ThenServerSubtractsAndCompares(string engine)
    {
        using RowContext context = ContextOf(engine);

        var sql = Where(context, "where[x.Id-x.Id>5]");

        sql.ToLowerInvariant().Should().Contain("where");
        sql.ToLowerInvariant().Should().Contain("-");
        sql.Should().Contain("5");
    }

    [Theory]
    [InlineData("npgsql")]
    [InlineData("sqlite")]
    [InlineData("mysql")]
    public void DifferenceAsKey_WhenTranslated_ThenServerSubtractsInOrderBy(string engine)
    {
        using RowContext context = ContextOf(engine);

        var sql = OrderBy(context, "orderBy[x.Id-x.Id]");

        sql.ToLowerInvariant().Should().Contain("order by");
        sql.ToLowerInvariant().Should().Contain("-");
        sql.Should().NotContain("WHERE");
    }

    [Theory]
    [InlineData("npgsql")]
    [InlineData("sqlite")]
    [InlineData("mysql")]
    public void GroupOfAFoldedComputation_WhenTranslated_ThenServerComputesItFirst(string engine)
    {
        using RowContext context = ContextOf(engine);

        //`(2*3)` is arithmetic over written numbers and the server computes the same constant the
        //fold answers - one comparison in the WHERE, no round trip of the value
        var sql = Where(context, "where[x.Id>(2*3)]");

        sql.ToLowerInvariant().Should().Contain("where");
        sql.Should().Contain("6");
    }

    [Theory]
    [InlineData("npgsql")]
    [InlineData("sqlite")]
    [InlineData("mysql")]
    public void KeyOpeningWithAGroup_WhenTranslated_ThenOneStatementWithTheGroupFirst(string engine)
    {
        using RowContext context = ContextOf(engine);

        var sql = OrderBy(context, "orderBy[(x.Id+x.Id)*2]");

        sql.ToLowerInvariant().Should().Contain("order by");
        sql.ToLowerInvariant().Should().Contain("*");
        sql.Should().NotContain("WHERE");
    }

    [Theory]
    [InlineData("npgsql")]
    [InlineData("sqlite")]
    [InlineData("mysql")]
    public void DecimalAndDoubleSumAsKey_WhenTranslated_ThenTheServerWidensTheKeyInsideTheOrder(
        string engine)
    {
        using RowContext context = ContextOf(engine);

        //the pair meets in a double in an ordering too, and the widening is the server's own:
        //npgsql spells the cast ::double precision, sqlite CAST AS REAL, mysql widens the
        //decimal by 0e0 - one statement, both columns named inside the ORDER BY, and no row
        //travels to the client to be summed there
        var sql = OrderBy(context, "orderBy[x.Decimal+x.Double]");

        OrderOf(sql).Should().Contain("decimal");
        OrderOf(sql).Should().Contain("double");
        sql.Should().NotContain("WHERE");
        StatementCount(sql).Should().Be(1);
    }

    [Theory]
    [InlineData("npgsql")]
    [InlineData("sqlite")]
    [InlineData("mysql")]
    public void DecimalAndDoubleSum_WhenCompared_ThenTheServerWidensTheSumInsideThePredicate(
        string engine)
    {
        using RowContext context = ContextOf(engine);

        var sql = Where(context, "where[x.Decimal+x.Double>5]");

        SqlOf(sql).Should().Contain("decimal");
        SqlOf(sql).Should().Contain("double");
        SqlOf(sql).Should().Contain("+");
        StatementCount(sql).Should().Be(1);
    }

    [Theory]
    [InlineData("npgsql")]
    [InlineData("sqlite")]
    [InlineData("mysql")]
    public void NullableDecimalAndDoubleSum_WhenCompared_ThenTheServerLiftsTheSum(string engine)
    {
        using RowContext context = ContextOf(engine);

        //nullability composes over the meeting, and the lifted sum is the server's as well
        var sql = Where(context, "where[x.NullableDecimal+x.Double>5]");

        SqlOf(sql).Should().Contain("nullabledecimal");
        SqlOf(sql).Should().Contain("double");
        StatementCount(sql).Should().Be(1);
    }

    [Fact]
    public void PlainDecimalKey_WhenTranslatedBySqlite_ThenThePackRefusesTheKey()
    {
        //sqlite keeps a decimal as text and answers no ordering over one, so a key the fold kept
        //a decimal would never reach the server; the same key met to a double is answered above
        var act = () =>
        {
            using RowContext context = ContextOf("sqlite");

            return OrderBy(context, "orderBy[x.Decimal]");
        };

        act.Should().Throw<NotSupportedException>();
    }

    [Theory]
    [InlineData("npgsql")]
    [InlineData("sqlite")]
    [InlineData("mysql")]
    public void UintAndByteSum_WhenCompared_ThenTheServerAddsInTheCoveringNumber(string engine)
    {
        using RowContext context = ContextOf(engine);

        //a uint already covers every value a byte has, so the sum is a uint and only the byte
        //travels through a cast: npgsql makes it a bigint, sqlite an INTEGER, mysql unsigned
        var sql = Where(context, "where[x.Uint+x.Byte>5]");

        SqlOf(sql).Should().Contain("uint");
        SqlOf(sql).Should().Contain("byte");
        StatementCount(sql).Should().Be(1);
    }

    [Fact]
    public void SignedAndUlongSum_WhenBuilt_ThenNoStatementIsAsked()
    {
        //no number of C# holds the values of a signed one and of a ulong, so the fold refuses
        //the pair while it builds the filter and no statement is ever asked - the same pair C#
        //refuses to add by hand
        var act = () =>
        {
            using RowContext context = ContextOf("npgsql");

            return Where(context, "where[x.Long+x.Unsigned>5]");
        };

        act.Should().Throw<PropertyAssignment.MathOperandTypesCannotBeOperatedException>();
    }

    private static RowContext ContextOf(string engine) => engine switch
    {
        "npgsql" => new PgContext(),
        "sqlite" => new SqliteContext(),
        "mysql" => new MySqlContext(),
        _ => throw new ArgumentOutOfRangeException(nameof(engine))
    };

    private static string Where(RowContext context, string query)
    {
        var filter = Filter(query);

        var source = (IQueryable)context.Rows;
        var method = typeof(Queryable).GetMethods()
            .Single(m => m.Name == nameof(Queryable.Where)
                         && m.GetParameters().Length == 2
                         && m.GetParameters()[1].ParameterType.GetGenericArguments()[0]
                             .GetGenericTypeDefinition() == typeof(Func<,>))
            .MakeGenericMethod(typeof(Row));

        return ((IQueryable)method.Invoke(null, [source, filter])!).ToQueryString();
    }

    private static string OrderBy(RowContext context, string query)
    {
        var (selector, propType) = Key(query);

        var source = (IQueryable)context.Rows;
        var method = typeof(Queryable).GetMethods()
            .Single(m => m.Name == nameof(Queryable.OrderBy)
                         && m.GetParameters().Length == 2
                         && m.GetParameters()[1].ParameterType.GetGenericArguments()[0]
                             .GetGenericTypeDefinition() == typeof(Func<,>))
            .MakeGenericMethod(typeof(Row), propType);

        return ((IQueryable)method.Invoke(null, [source, selector])!).ToQueryString();
    }

    private static LambdaExpression Filter(string query) => Builder()
        .GetFilter(typeof(Row), Parse(query).GetAll<WhereRoot>().First(), query);

    private static (LambdaExpression Expression, Type PropType) Key(string query) =>
        Builder().GetProperty(typeof(Row), Parse(query).GetAll<OrderByRoot>().First(), query);

    private static ExpressionBuilder Builder() =>
        new(Services.PropertiesCache, Services.MethodsCache, Services.ConverterFactory,
            new ParametersConverter(Services.WrittenValues, Services.DefaultConverter),
            [], Services.WrittenValues, null!);

    private static IHLinqQuery Parse(string query) =>
        new HLinqParser(Services.Parsers)
            .TestParseEntryPoint<Row>(query, Services.Tokenizer.Tokenize(query));

    /// <summary>
    /// The row shape of the provider measurements, with the numbers to compute with.
    /// </summary>
    public class Row
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public decimal Decimal { get; set; }
        public double Double { get; set; }
        public float Float { get; set; }
        public decimal? NullableDecimal { get; set; }
        public byte Byte { get; set; }
        public uint Uint { get; set; }
        public ulong Unsigned { get; set; }
        public long Long { get; set; }
    }

    //one row shape, one entity set: the engine of a measurement is the only thing a context of a
    //pack adds
    private abstract class RowContext : DbContext
    {
        public DbSet<Row> Rows => Set<Row>();
    }

    private sealed class PgContext : RowContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder options) =>
            options.UseNpgsql("Host=localhost;Database=nowhere;Username=u;Password=p");
    }

    private sealed class SqliteContext : RowContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder options) =>
            options.UseSqlite("Data Source=:memory:");
    }

    private sealed class MySqlContext : RowContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder options) =>
            options.UseMySql("host=localhost;database=nowhere;uid=u;pwd=p",
                new MySqlServerVersion(new Version(8, 0, 0)));
    }

    private static string OrderOf(string sql) =>
        sql[(sql.IndexOf("ORDER BY", StringComparison.OrdinalIgnoreCase) + 8)..].ToLowerInvariant();

    private static string SqlOf(string sql) =>
        sql[(sql.IndexOf("WHERE", StringComparison.OrdinalIgnoreCase) + 5)..].ToLowerInvariant();

    private static int StatementCount(string sql) =>
        sql.Split(';').Count(s => s.Trim().Length > 0);
}

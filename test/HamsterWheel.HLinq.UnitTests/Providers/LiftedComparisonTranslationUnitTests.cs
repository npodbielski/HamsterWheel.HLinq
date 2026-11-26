using System.Linq.Expressions;
using FluentAssertions;
using HamsterWheel.HLinq.Pipeline.Applier.Builders;
using HamsterWheel.HLinq.Pipeline.Parser;
using HamsterWheel.HLinq.Tree.Filtering;
using HamsterWheel.HLinq.UnitTests.TestUtils;
using Microsoft.EntityFrameworkCore;

namespace HamsterWheel.HLinq.UnitTests.Providers;

/// <summary>
/// A lifted comparison reaches the engine as one statement, as hand-written Linq asks.
/// </summary>
public class LiftedComparisonTranslationUnitTests
{
    private static readonly TestServicesCollection Services = new();

    [Theory]
    [InlineData("npgsql")]
    [InlineData("sqlite")]
    [InlineData("mysql")]
    public void IntComparedToItsNullable_WhenTranslated_ThenServerComparesTheColumns(string engine)
    {
        using RowContext context = ContextOf(engine);

        var sql = Where(context, "where[x.Int==x.NullableInt]");

        //the lifted comparison is the server's own: one statement, both columns named in its
        //predicate, and no row traveling to the client to be compared there
        SqlOf(sql).Should().Contain("int");
        SqlOf(sql).Should().Contain("nullableint");
        StatementCount(sql).Should().Be(1);
    }

    [Theory]
    [InlineData("npgsql")]
    [InlineData("sqlite")]
    [InlineData("mysql")]
    public void DecimalComparedToItsNullable_WhenTranslated_ThenServerComparesTheColumns(string engine)
    {
        using RowContext context = ContextOf(engine);

        var sql = Where(context, "where[x.Decimal==x.NullableDecimal]");

        SqlOf(sql).Should().Contain("decimal");
        SqlOf(sql).Should().Contain("nullabledecimal");
        StatementCount(sql).Should().Be(1);
    }

    [Theory]
    [InlineData("npgsql")]
    [InlineData("sqlite")]
    [InlineData("mysql")]
    public void EnumComparedToItsUnderlyingInt_WhenTranslated_ThenTheServerComparesTheColumns(string engine)
    {
        using RowContext context = ContextOf(engine);

        //the enum is converted to the number the store already holds for its column: npgsql
        //leaves the column as it is, sqlite casts it AS INTEGER, mysql AS signed - in every
        //pack the predicate names both columns and no row travels to the client
        var sql = Where(context, "where[x.Kind==x.Int]");

        SqlOf(sql).Should().Contain("kind");
        SqlOf(sql).Should().Contain("int");
        StatementCount(sql).Should().Be(1);
    }

    [Theory]
    [InlineData("npgsql")]
    [InlineData("sqlite")]
    [InlineData("mysql")]
    public void NullableEnumComparedToNullableInt_WhenTranslated_ThenServerComparesTheColumns(string engine)
    {
        using RowContext context = ContextOf(engine);

        //the meeting and the nullable compose: a `Kind?` and an `int?` compare as `int?`, the
        //two nulls meeting the way C#'s lifted operator meets them
        var sql = Where(context, "where[x.NullableKind==x.NullableInt]");

        SqlOf(sql).Should().Contain("kind");
        SqlOf(sql).Should().Contain("nullableint");
        StatementCount(sql).Should().Be(1);
    }

    [Theory]
    [InlineData("npgsql")]
    [InlineData("sqlite")]
    [InlineData("mysql")]
    public void EnumComparedToText_WhenTranslated_ThenTheNameSpelledByTheCaseIsInThePredicate(string engine)
    {
        using RowContext context = ContextOf(engine);

        //the enum's name is EF's own CASE over the mapped values - one CASE per pack, each
        //spelling the member names the CLR type declares - and it stands inside the predicate,
        //so the server answers the text comparison itself
        var sql = Where(context, "where[x.Kind==x.Name]");

        SqlOf(sql).Should().Contain("case");
        SqlOf(sql).Should().Contain("when");
        SqlOf(sql).Should().Contain("kind");
        SqlOf(sql).Should().Contain("name");
        StatementCount(sql).Should().Be(1);
    }

    [Theory]
    [InlineData("npgsql")]
    [InlineData("sqlite")]
    [InlineData("mysql")]
    public void DateTimeComparedToItsNullable_WhenTranslated_ThenServerComparesTheColumns(string engine)
    {
        using RowContext context = ContextOf(engine);

        var sql = Where(context, "where[x.DateTime>x.NullableDateTime]");

        SqlOf(sql).Should().Contain("datetime");
        SqlOf(sql).Should().Contain("nullabledatetime");
        StatementCount(sql).Should().Be(1);
    }

    [Theory]
    [InlineData("npgsql")]
    [InlineData("sqlite")]
    [InlineData("mysql")]
    public void ShortComparedToByte_WhenTranslated_ThenServerComparesTheColumns(string engine)
    {
        using RowContext context = ContextOf(engine);

        var sql = Where(context, "where[x.Short==x.Byte]");

        //the promotion to int is what the translator of each pack gives the same widened
        //comparison written by hand: npgsql spells it a ::integer cast, sqlite and mysql let the
        //columns' own integer affinity answer it - in every pack the predicate names both
        //columns and no row travels to the client
        SqlOf(sql).Should().Contain("short");
        SqlOf(sql).Should().Contain("byte");
        StatementCount(sql).Should().Be(1);
    }

    [Theory]
    [InlineData("npgsql")]
    [InlineData("sqlite")]
    [InlineData("mysql")]
    public void IntComparedToLong_WhenTranslated_ThenServerComparesTheColumns(string engine)
    {
        using RowContext context = ContextOf(engine);

        var sql = Where(context, "where[x.Int==x.Long]");

        SqlOf(sql).Should().Contain("int");
        SqlOf(sql).Should().Contain("long");
        StatementCount(sql).Should().Be(1);
    }

    [Theory]
    [InlineData("npgsql")]
    [InlineData("sqlite")]
    [InlineData("mysql")]
    public void IntComparedToNullableLong_WhenTranslated_ThenServerComparesTheWidenedNullable(string engine)
    {
        using RowContext context = ContextOf(engine);

        //the width and the nullable meet together: `int` and `long?` compare as `long?`
        var sql = Where(context, "where[x.Int==x.NullableLong]");

        SqlOf(sql).Should().Contain("int");
        SqlOf(sql).Should().Contain("nullablelong");
        StatementCount(sql).Should().Be(1);
    }

    [Theory]
    [InlineData("npgsql")]
    [InlineData("sqlite")]
    [InlineData("mysql")]
    public void NullableEnumComparedToText_WhenTranslated_ThenTheCaseIsInThePredicate(string engine)
    {
        using RowContext context = ContextOf(engine);

        //a null enum spells the empty text the ELSE branch of the CASE answers it with, so no
        //guard around the name is needed - the CASE of the pack is the whole meeting
        var sql = Where(context, "where[x.NullableKind==x.Name]");

        SqlOf(sql).Should().Contain("case");
        SqlOf(sql).Should().Contain("nullablekind");
        SqlOf(sql).Should().Contain("name");
        StatementCount(sql).Should().Be(1);
    }

    [Theory]
    [InlineData("npgsql")]
    [InlineData("sqlite")]
    [InlineData("mysql")]
    public void DecimalComparedToDouble_WhenTranslated_ThenTheCastIsInThePredicate(string engine)
    {
        using RowContext context = ContextOf(engine);

        //the decimal is cast to a double inside the predicate: npgsql spells it ::double
        //precision, sqlite CAST AS REAL, mysql the decimal widened by 0e0 - one statement in
        //every pack and no row travels to the client to be compared there
        var sql = Where(context, "where[x.Decimal==x.Double]");

        SqlOf(sql).Should().Contain("decimal");
        SqlOf(sql).Should().Contain("double");
        StatementCount(sql).Should().Be(1);
    }

    [Theory]
    [InlineData("npgsql")]
    [InlineData("sqlite")]
    [InlineData("mysql")]
    public void DecimalComparedToFloat_WhenTranslated_ThenBothSidesMeetInTheDouble(string engine)
    {
        using RowContext context = ContextOf(engine);

        var sql = Where(context, "where[x.Decimal==x.Float]");

        SqlOf(sql).Should().Contain("decimal");
        SqlOf(sql).Should().Contain("float");
        StatementCount(sql).Should().Be(1);
    }

    [Theory]
    [InlineData("npgsql")]
    [InlineData("sqlite")]
    [InlineData("mysql")]
    public void NullableDecimalComparedToDouble_WhenTranslated_ThenTheCastOfTheNullableIsInThePredicate(
        string engine)
    {
        using RowContext context = ContextOf(engine);

        var sql = Where(context, "where[x.NullableDecimal==x.Double]");

        SqlOf(sql).Should().Contain("nullabledecimal");
        SqlOf(sql).Should().Contain("double");
        StatementCount(sql).Should().Be(1);
    }

    [Theory]
    [InlineData("npgsql")]
    [InlineData("sqlite")]
    [InlineData("mysql")]
    public void EnumComparedToWiderIntegral_WhenTranslated_ThenTheServerComparesTheColumns(string engine)
    {
        using RowContext context = ContextOf(engine);

        //an `enum : int` and a `long` meet in `long`, the widening C# gives an `int` to meet a
        //`long`: npgsql casts the enum's column ::bigint, sqlite and mysql cast it to signed
        var sql = Where(context, "where[x.Kind==x.Long]");

        SqlOf(sql).Should().Contain("kind");
        SqlOf(sql).Should().Contain("long");
        StatementCount(sql).Should().Be(1);
    }

    [Theory]
    [InlineData("where[x.Kind==x.Flag]")] //a bool is no number an enum stands for
    [InlineData("where[x.Decimal==x.Name]")] //a decimal and a text have no single meeting
    [InlineData("where[x.Int==x.Name]")] //C#'s own refusal stays
    [InlineData("where[x.Decimal==x.DateTime]")] //no number in one and a date in the other
    public void APairWithNoSingleMeeting_WhenBuilt_ThenTheTreeIsNeverBuilt(string query)
    {
        //the refusal is the builder's, while it builds the filter: the pair has no comparison
        //operator in C# and no meeting the engine can state, so no statement is asked
        var act = () => Filter(query);

        act.Should().Throw<Condition.ComparisonOperandTypesCannotBeComparedException>();
    }

    private static RowContext ContextOf(string engine) => engine switch
    {
        "npgsql" => new PgContext(),
        "sqlite" => new SqliteContext(),
        "mysql" => new MySqlContext(),
        _ => throw new ArgumentOutOfRangeException(nameof(engine))
    };

    //the predicate alone, in lower case: everything before WHERE is the selected columns, which
    //cast too when one of them is read through the same promotion - only the predicate is the
    //question here, and each engine spells its own cast
    private static string SqlOf(string sql) =>
        sql[(sql.IndexOf("WHERE", StringComparison.OrdinalIgnoreCase) + 5)..].ToLowerInvariant();

    private static int StatementCount(string sql) =>
        sql.Split(';').Count(s => s.Trim().Length > 0);

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

    private static LambdaExpression Filter(string query) =>
        Builder().GetFilter(typeof(Row), Parse(query).GetAll<WhereRoot>().First(), query);

    private static ExpressionBuilder Builder() =>
        new(Services.PropertiesCache, Services.MethodsCache, Services.ConverterFactory,
            new ParametersConverter(Services.WrittenValues, Services.DefaultConverter),
            [], Services.WrittenValues, null!);

    private static IHLinqQuery Parse(string query) =>
        new HLinqParser(Services.Parsers)
            .TestParseEntryPoint<Row>(query, Services.Tokenizer.Tokenize(query));

    /// <summary>
    /// The row shape of the lifted comparisons.
    /// </summary>
    public class Row
    {
        public int Id { get; set; }
        public RowKind Kind { get; set; }
        public RowKind? NullableKind { get; set; }
        public string Name { get; set; } = null!;
        public bool Flag { get; set; }
        public DateTime DateTime { get; set; }
        public bool? NullableFlag { get; set; }
        public DateTime? NullableDateTime { get; set; }
        public short Short { get; set; }
        public byte Byte { get; set; }
        public int Int { get; set; }
        public long Long { get; set; }
        public float Float { get; set; }
        public decimal Decimal { get; set; }
        public double Double { get; set; }
        public int? NullableInt { get; set; }
        public long? NullableLong { get; set; }
        public decimal? NullableDecimal { get; set; }
    }

    public enum RowKind
    {
        Zero = 0,
        One = 1
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
}

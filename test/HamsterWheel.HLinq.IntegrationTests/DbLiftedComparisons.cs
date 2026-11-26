using FluentAssertions;
using HamsterWheel.HLinq.Demo.Data;
using HamsterWheel.HLinq.IntegrationTests.Fixtures;
using HamsterWheel.HLinq.Pipeline.Applier;
using HamsterWheel.HLinq.Pipeline.Parser;
using HamsterWheel.HLinq.Pipeline.Tokenizer;
using HamsterWheel.HLinq.Request;
using HamsterWheel.HLinq.Tree.Filtering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HamsterWheel.HLinq.IntegrationTests;

/// <summary>
/// Comparisons of two columns meet in the type C# compares them in, answered by the server.
/// </summary>
[Collection(nameof(IntegrationCollectionDefinition))]
public class DbLiftedComparisons(DemoFixture fixture) : IAsyncLifetime
{
    private DemoContext Context { get; set; } = null!;

    private IQueryable<Measurement> Measurements => Context.Measurements;

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
    public async Task WhenIntIsComparedToItsNullable_ThenServerComparesTheColumns()
    {
        //act
        //same comparison written by hand
        var actual = await Run("where[x.Int==x.NullableInt]");

        //assert
        actual.Should().BeEquivalentTo(Measurements.Where(x => x.Int == x.NullableInt));
    }

    [Fact]
    public async Task WhenNullableIntIsComparedToInt_ThenServerComparesTheColumns()
    {
        var actual = await Run("where[x.NullableInt==x.Int]");

        actual.Should().BeEquivalentTo(Measurements.Where(x => x.NullableInt == x.Int));
    }

    [Fact]
    public async Task WhenDecimalIsComparedToItsNullable_ThenServerComparesTheColumns()
    {
        var actual = await Run("where[x.Decimal==x.NullableDecimal]");

        actual.Should().BeEquivalentTo(Measurements.Where(x => x.Decimal == x.NullableDecimal));
    }

    [Fact]
    public async Task WhenDateTimeIsComparedToItsNullable_ThenServerComparesTheColumns()
    {
        var actual = await Run("where[x.DateTime>x.NullableDateTime]");

        actual.Should().BeEquivalentTo(Measurements.Where(x => x.DateTime > x.NullableDateTime));
    }

    [Fact]
    public async Task WhenFlagIsComparedToItsNullable_ThenServerComparesTheColumns()
    {
        var actual = await Run("where[x.Flag==x.NullableFlag]");

        actual.Should().BeEquivalentTo(Measurements.Where(x => x.Flag == x.NullableFlag));
    }

    [Fact]
    public async Task WhenEnumIsComparedToItsNullable_ThenServerComparesTheColumns()
    {
        var actual = await Run("where[x.Kind==x.NullableKind]");

        actual.Should().BeEquivalentTo(Measurements.Where(x => x.Kind == x.NullableKind));
    }

    [Fact]
    public async Task WhenShortIsComparedToByte_ThenServerComparesTheColumnsAfterThePromotion()
    {
        var actual = await Run("where[x.Short==x.Byte]");

        actual.Should().BeEquivalentTo(Measurements.Where(x => x.Short == x.Byte));
    }

    [Fact]
    public async Task WhenIntIsComparedToLong_ThenServerComparesTheColumnsAfterThePromotion()
    {
        var actual = await Run("where[x.Int==x.Long]");

        actual.Should().BeEquivalentTo(Measurements.Where(x => x.Int == x.Long));
    }

    [Fact]
    public async Task WhenFloatIsComparedToDouble_ThenServerComparesTheColumnsAfterThePromotion()
    {
        var actual = await Run("where[x.Float==x.Double]");

        //the promotion lifts the float to a double; the seeded values are halves a binary double
        //and a binary float both hold exactly, so the widened equality compares without rounding
        actual.Should().BeEquivalentTo(
            // ReSharper disable once CompareOfFloatsByEqualityOperator - the seeded values are exact
            Measurements.Where(x => x.Float == x.Double));
    }

    [Fact]
    public async Task WhenIntIsComparedToNullableLong_ThenServerComparesTheWidenedNullable()
    {
        //the width and the nullable meet together: `int` and `long?` compare as `long?`
        var actual = await Run("where[x.Int==x.NullableLong]");

        actual.Should().BeEquivalentTo(Measurements.Where(x => x.Int == x.NullableLong));
    }

    [Fact]
    public async Task WhenEnumIsComparedToItsUnderlyingInt_ThenServerComparesTheColumns()
    {
        //the enum is the number the column already stores, and the rows are those where the
        //number written beside it is the enum's own value
        var actual = await Run("where[x.Kind==x.Level]");

        actual.Should().BeEquivalentTo(Measurements.Where(x => (int)x.Kind == x.Level));
    }

    [Fact]
    public async Task WhenIntIsComparedToEnum_ThenServerComparesTheColumns()
    {
        var actual = await Run("where[x.Level==x.Kind]");

        actual.Should().BeEquivalentTo(Measurements.Where(x => x.Level == (int)x.Kind));
    }

    [Fact]
    public async Task WhenEnumIsComparedToText_ThenServerComparesTheNameToTheColumn()
    {
        //the enum meets a text of its name: the rows whose text spells the member answer yes
        //and the rows that hold its number as a text answer no
        var actual = await Run("where[x.Kind==x.Name]");

        actual.Should().BeEquivalentTo(
            Measurements.Where(x => x.Kind.ToString() == x.Name));
    }

    [Fact]
    public async Task WhenNullableEnumIsComparedToText_ThenServerAnswersTheCaseOfTheName()
    {
        //a null enum spells the empty text EF's own CASE answers for it, so it meets nothing -
        //the same answer the server gives through its CASE
        var actual = await Run("where[x.NullableKind==x.Name]");

        actual.Should().BeEquivalentTo(
            Measurements.Where(x => x.NullableKind.ToString() == x.Name));
    }

    [Fact]
    public async Task WhenDecimalIsComparedToDouble_ThenServerMeetsThemInADouble()
    {
        var actual = await Run("where[x.Decimal==x.Double]");

        // ReSharper disable once CompareOfFloatsByEqualityOperator - the meeting is the double
        actual.Should().BeEquivalentTo(Measurements.Where(x => (double)x.Decimal == x.Double));
    }

    [Fact]
    public async Task WhenDecimalOfItsTailMeetsTheNearestDouble_ThenTheServerStillAnswersTheRow()
    {
        //the written loss of the meeting, answered by the server: the seeded row's decimal of a
        //tail a double cannot keep is not the double as decimals, but it is that double once met
        var actual = await Run("where[x.Decimal==x.Double]");

        // ReSharper disable once CompareOfFloatsByEqualityOperator - the meeting is the double
        actual.Should().Contain(Measurements.Single(x => x.Id == 10));
        Measurements.Single(x => x.Id == 10).Decimal.Should().NotBe(10000000000000000m);
    }

    [Fact]
    public async Task WhenNullableDecimalIsComparedToDouble_ThenServerComparesTheColumns()
    {
        var actual = await Run("where[x.NullableDecimal==x.Double]");

        // ReSharper disable once CompareOfFloatsByEqualityOperator - the meeting is the double
        actual.Should().BeEquivalentTo(Measurements.Where(x => (double?)x.NullableDecimal == x.Double));
    }

    [Fact]
    public async Task WhenEnumIsComparedToWiderIntegral_ThenServerComparesTheColumns()
    {
        //an enum meets any integral number in the type its underlying one widens to - an
        //`enum : int` and a `long` compare as `long`, the widening C# gives an `int`
        var actual = await Run("where[x.Kind==x.Long]");

        actual.Should().BeEquivalentTo(Measurements.Where(x => (int)x.Kind == x.Long));
    }

    [Fact]
    public async Task WhenByteIsComparedToUshort_ThenServerComparesThemAsInts()
    {
        //neither width holds the other's values and int holds both: the covering number C#
        //binds the same comparison written by hand in
        var actual = await Run("where[x.Byte==x.Ushort]");

        actual.Should().BeEquivalentTo(Measurements.Where(x => x.Byte == x.Ushort));
    }

    [Fact]
    public async Task WhenUintIsComparedToByte_ThenServerComparesThemAsUints()
    {
        var actual = await Run("where[x.Uint==x.Byte]");

        actual.Should().BeEquivalentTo(Measurements.Where(x => x.Uint == x.Byte));
    }

    [Fact]
    public async Task WhenSignedIsComparedToUlong_ThenTheQueryStaysRefused()
    {
        //no 64-bit number holds both ranges: the pair meets in nothing and the refusal names
        //the two types before any statement is asked
        var act = async () => await Run("where[x.Int==x.Unsigned]");

        await act.Should().ThrowAsync<Condition.ComparisonOperandTypesCannotBeComparedException>();
    }

    [Fact]
    public async Task WhenDecimalIsComparedToText_ThenTheQueryStaysRefused()
    {
        var act = async () => await Run("where[x.Decimal==x.Name]");

        await act.Should().ThrowAsync<Condition.ComparisonOperandTypesCannotBeComparedException>();
    }

    private async Task<List<Measurement>> Run(string query)
    {
        //the applier takes a query that has already been parsed; parse it the way the endpoint does
        var services = fixture.DemoTestHost.Services;
        var parsed = new HLinqQuery<Measurement>
        {
            SourceQueryString = query
        };
        services.GetRequiredService<IHLinqParser>().Parse(parsed,
            services.GetRequiredService<IHLinqTokenizer>().Tokenize(query));

        var results = await Task.Run(() =>
            (object[])Applier().Apply(Measurements, typeof(Measurement), parsed));

        return [.. results.Cast<Measurement>()];
    }

    private IHLinqQueryApplier Applier() =>
        fixture.DemoTestHost.Services.GetRequiredService<IHLinqQueryApplier>();
}

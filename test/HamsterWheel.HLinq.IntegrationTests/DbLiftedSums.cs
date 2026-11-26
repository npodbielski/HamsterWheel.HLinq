using System.Text.Json;
using FluentAssertions;
using HamsterWheel.HLinq.Demo.Data;
using HamsterWheel.HLinq.IntegrationTests.Fixtures;
using HamsterWheel.HLinq.Pipeline.Applier;
using HamsterWheel.HLinq.Pipeline.Parser;
using HamsterWheel.HLinq.Pipeline.Tokenizer;
using HamsterWheel.HLinq.Request;
using HamsterWheel.HLinq.Tree.Selecting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HamsterWheel.HLinq.IntegrationTests;

/// <summary>
/// A sum of a `decimal` and a `double` is answered by the server as the meeting states it.
/// </summary>
[Collection(nameof(IntegrationCollectionDefinition))]
public class DbLiftedSums(DemoFixture fixture) : IAsyncLifetime
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
    public async Task WhenServerSumsADecimalAndADouble_ThenItAddsThemAsDoubles()
    {
        //the seeded row of a decimal past what a double keeps: its sum met as doubles is the
        //rounder number the double of the decimal is, and the threshold of that number is one
        //a sum that kept the decimal's tail does not pass - the loss the meeting states
        var actual = await Run("where[x.Decimal+x.Double>=20000000000000000]");

        actual.Should().BeEquivalentTo(
            Measurements.Where(x => (double)x.Decimal + x.Double >= 20000000000000000L));
        actual.Should().NotBeEmpty();
    }

    [Fact]
    public async Task WhenServerSumsANullableDecimalAndADouble_ThenTheNullableLiftsTheSum()
    {
        var actual = await Run("where[x.NullableDecimal+x.Double>30]");

        actual.Should().BeEquivalentTo(
            Measurements.Where(x => (double?)x.NullableDecimal + x.Double > 30));
    }

    [Fact]
    public async Task WhenServerSelectsADecimalAndDoubleSum_ThenTheAnswerIsADouble()
    {
        //the ordering is named so one answer can be read against the other: a projection with
        //no order comes back in whatever order the server walks the rows
        var actual = await RunAny("orderBy[x.Id].select[N=x.Decimal+x.Double]");

        //the seeded decimal of a tail no double keeps comes back as that double's sum, which is
        //not the decimal sum the fold answered before
        Json(actual).Should().Be(Json(Measurements
            .OrderBy(x => x.Id)
            .Select(x => new { N = (double)x.Decimal + x.Double })
            .ToList()));
    }

    [Fact]
    public async Task WhenServerSelectsANullableDecimalAndDoubleSum_ThenTheNullStaysNull()
    {
        var actual = await RunAny("orderBy[x.Id].select[N=x.NullableDecimal+x.Double]");

        Json(actual).Should().Be(Json(Measurements
            .OrderBy(x => x.Id)
            .Select(x => new { N = (double?)x.NullableDecimal + x.Double })
            .ToList()));
    }

    [Fact]
    public async Task WhenServerOrdersByADecimalAndDoubleSum_ThenTheKeyIsTheSumOfDoubles()
    {
        var actual = await Run("orderBy[x.Decimal+x.Double]");

        //the widened key is the server's own work in the ORDER BY, and its order is the order the
        //same selector written by hand gives the rows
        actual.Should().BeEquivalentTo(
            Measurements.OrderBy(x => (double)x.Decimal + x.Double).ToList(),
            o => o.WithStrictOrdering());
    }

    [Fact]
    public async Task WhenServerSumsAUintAndAByte_ThenItAddsThemAsUints()
    {
        var actual = await Run("where[x.Uint+x.Byte>50]");

        actual.Should().BeEquivalentTo(
            Measurements.Where(x => x.Uint + x.Byte > 50u));
    }

    [Fact]
    public async Task WhenServerSumsAUlongAndAnIntegral_ThenTheQueryIsRefused()
    {
        //no number of C# holds the values of a signed one and of a ulong, so the fold refuses
        //the pair while it builds - the refusal is client-side, the server is never asked
        var act = async () => await Run("where[x.Long+x.Unsigned>50]");

        await act.Should().ThrowAsync<PropertyAssignment.MathOperandTypesCannotBeOperatedException>();
    }

    private async Task<List<Measurement>> Run(string query) =>
        [.. (await RunAny(query)).Cast<Measurement>()];

    /// <summary>
    /// The query through the endpoint's applier, with the answers as they come back.
    /// </summary>
    private async Task<object[]> RunAny(string query)
    {
        //the applier takes a query that has already been parsed; parse it the way the endpoint does
        var services = fixture.DemoTestHost.Services;
        var parsed = new HLinqQuery<Measurement>
        {
            SourceQueryString = query
        };
        services.GetRequiredService<IHLinqParser>().Parse(parsed,
            services.GetRequiredService<IHLinqTokenizer>().Tokenize(query));

        return await Task.Run(() =>
            (object[])Applier().Apply(Measurements, typeof(Measurement), parsed));
    }

    private static string Json(object value) =>
        JsonSerializer.Serialize(value, new JsonSerializerOptions(JsonSerializerDefaults.Web));

    private IHLinqQueryApplier Applier() =>
        fixture.DemoTestHost.Services.GetRequiredService<IHLinqQueryApplier>();
}

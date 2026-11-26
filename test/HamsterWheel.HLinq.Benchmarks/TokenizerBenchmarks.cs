using System.Reflection;
using BenchmarkDotNet.Attributes;
using HamsterWheel.HLinq.Pipeline.Tokenizer;
using Microsoft.Extensions.DependencyInjection;

namespace HamsterWheel.HLinq.Benchmarks;

/// <summary>
/// Tokenizer-only benchmarks. The baseline lives in benchmarks/; tokenizer changes
/// re-run this and commit a diff of the tables.
/// </summary>
[ShortRunJob]
[MemoryDiagnoser]
public class TokenizerBenchmarks
{
    // representative queries: plain, dots in value, date constant, dead-prefix keywords,
    // range, wide select, full chain, math in a filter and as an ordering key;
    // groupByOnly and takeOne are the controls of a grammar change: clauses whose group of words
    // the math operators are not part of
    private static readonly Dictionary<string, string> Queries = new()
    {
        ["whereInt"] = "where[x.Int==5]",
        ["whereDoubleDot"] = "where[x.Double==1.21]",
        ["whereDate"] = "where[x.Date>2023-10-12]",
        ["chain"] = "where[x.Name==John&&x.Int>3].select[FullName=Name,x.Id].orderBy[x.Name].take[10..20]",
        ["orderbyLower"] = "orderby[x.Name]",
        ["orderByDescending"] = "orderByDescending[x.Name]",
        ["takeRange"] = "take[10..20]",
        ["nestedSelect"] = "select[x.Name,addresses=x.Addresses.select(a.Street,a.City)]",
        ["select27"] = BuildWideSelectQuery(27),
        ["whereName"] = "where[x.Name==John]",
        ["groupByOnly"] = "groupBy[x.Name]",
        ["takeOne"] = "take[10]",
        ["whereDifference"] = "where[x.Int-x.Int>5]",
        ["whereFoldedGroup"] = "where[x.Int>(2*3)]",
        ["orderByKey"] = "orderBy[x.Int+1]"
    };

    public static IEnumerable<string> QueryKeys => Queries.Keys;

    [ParamsSource(nameof(QueryKeys))]
    public string Key { get; set; } = null!;

    private IHLinqTokenizer _tokenizer = null!;

    private static string BuildWideSelectQuery(int properties) =>
        "select[" + string.Join(",", Enumerable.Range(1, properties)
            .Select(i => $"R{i:D2}=P{i:D2}")) + "]";

    // runs once per params case
    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();

        // HLinqCore is internal, unit tests reach it through InternalsVisibleTo, benches by reflection
        typeof(HLinqOptions).Assembly
            .GetType("HamsterWheel.HLinq.HLinqCore")!
            .GetMethod("ConfigureServices", BindingFlags.Public | BindingFlags.Static)!
            .Invoke(null, [services, new HLinqOptions()]);

        _tokenizer = services.BuildServiceProvider().GetRequiredService<IHLinqTokenizer>();
    }

    [Benchmark]
    public object Tokenize() => _tokenizer.Tokenize(Queries[Key]);
}

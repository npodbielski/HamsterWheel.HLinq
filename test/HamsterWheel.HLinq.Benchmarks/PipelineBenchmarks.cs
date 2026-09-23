using System.Reflection;
using BenchmarkDotNet.Attributes;
using HamsterWheel.HLinq.Pipeline;
using HamsterWheel.HLinq.Pipeline.Applier;
using HamsterWheel.HLinq.Pipeline.Parser;
using HamsterWheel.HLinq.Pipeline.Tokenizer;
using HamsterWheel.HLinq.Reflection;
using HamsterWheel.HLinq.Request;
using Microsoft.Extensions.DependencyInjection;

namespace HamsterWheel.HLinq.Benchmarks;

public class BenchEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Int { get; set; }
    public double Double { get; set; }
    public DateTime Date { get; set; }
    public bool Flag { get; set; }
}

/// <summary>
///     Tokenize / tokenize+parse / tokenize+parse+apply over an in-memory list, showing the
///     tokenizer's share of the whole pipeline. Baseline in benchmarks/.
/// </summary>
[ShortRunJob]
[MemoryDiagnoser]
public class PipelineBenchmarks
{
    private static readonly string[] Queries =
    [
        "where[x.Int>3]",
        "where[x.Name==entity12].take[10..20]",
        "orderBy[x.Name].take[0..10]"
    ];

    private readonly BenchEntity[] _data = Enumerable.Range(0, 100)
        .Select(i => new BenchEntity
        {
            Id = i,
            Name = $"entity{i}",
            Int = i,
            Double = i / 3d,
            Date = new DateTime(2020, 1, 1).AddDays(i)
        })
        .ToArray();

    private IHLinqQueryApplier _applier = null!;
    private IHLinqParser _parser = null!;
    private IHLinqQueryBinder _binder = null!;
    private IHLinqTokenizer _tokenizer = null!;

    [ParamsSource(nameof(QueryValues))]
    public string Query { get; set; } = null!;

    public static IEnumerable<string> QueryValues => Queries;

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();
        typeof(HLinqOptions).Assembly
            .GetType("HamsterWheel.HLinq.HLinqCore")!
            .GetMethod("ConfigureServices", BindingFlags.Public | BindingFlags.Static)!
            .Invoke(null, [services, new HLinqOptions()]);
        var provider = services.BuildServiceProvider();

        _tokenizer = provider.GetRequiredService<IHLinqTokenizer>();
        _parser = provider.GetRequiredService<IHLinqParser>();
        _applier = provider.GetRequiredService<IHLinqQueryApplier>();
        _binder = provider.GetRequiredService<IHLinqQueryBinder>();
    }

    [Benchmark]
    public object Tokenize() => _tokenizer.Tokenize(Query);

    [Benchmark]
    public object TokenizeAndParse() => Parse();

    [Benchmark]
    public object Apply() => _applier.Apply(_data.AsQueryable(), Parse());

    private IHLinqQuery Parse()
    {
        var hlinqQuery = _binder.BindQuery(Query, typeof(HLinqQuery<BenchEntity>));
        _parser.Parse(hlinqQuery, _tokenizer.Tokenize(Query));
        return hlinqQuery;
    }
}

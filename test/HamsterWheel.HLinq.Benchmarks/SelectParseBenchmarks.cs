using System.Reflection;
using BenchmarkDotNet.Attributes;
using HamsterWheel.HLinq.Pipeline.Parser;
using HamsterWheel.HLinq.Pipeline.Tokenizer;
using HamsterWheel.HLinq.Request;
using Microsoft.Extensions.DependencyInjection;

namespace HamsterWheel.HLinq.Benchmarks;

/// <summary>
/// Tokenize and parse over the shapes a <c>select</c> answers, beside one that carries none.
/// </summary>
[ShortRunJob]
[MemoryDiagnoser]
public class SelectParseBenchmarks
{
    private static readonly string[] Queries =
    [
        "where[x.Int>3]",
        "select[x.Name,x.Int,x.Double,x.Flag]",
        "select[A=x.Name,B=x.Int,C=x.Double,D=x.Flag]",
        "select[x.Name,x.Children.select(x.Name,x.Id)]",
        "select[x.Name,x.Children.select(A=a.Name,B=a.Id)]",
        "groupBy[x.Name].select[x.Key,C=x.Count(),x.Int]"
    ];

    private IHLinqQueryBinder _binder = null!;
    private IHLinqParser _parser = null!;
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
        _binder = provider.GetRequiredService<IHLinqQueryBinder>();
    }

    [Benchmark]
    public object TokenizeAndParse()
    {
        var hlinqQuery = _binder.BindQuery(Query, typeof(HLinqQuery<BenchEntity>));
        _parser.Parse(hlinqQuery, _tokenizer.Tokenize(Query));
        return hlinqQuery;
    }
}

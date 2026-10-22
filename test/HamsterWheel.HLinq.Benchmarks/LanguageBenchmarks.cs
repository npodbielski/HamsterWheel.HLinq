using System.Globalization;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using HamsterWheel.HLinq.Pipeline.Tokenizer;
using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Filtering;
using HamsterWheel.HLinq.Tokens.Ordering;
using HamsterWheel.HLinq.Tokens.Paging;
using HamsterWheel.HLinq.Tokens.Selecting;
using Microsoft.Extensions.DependencyInjection;

namespace HamsterWheel.HLinq.Benchmarks;

/// <summary>
/// What a language of one culture costs: the walk over a word set that carries the words of two
/// vocabularies, and the building of that language for a culture. The baseline is the same walk over
/// the invariant words alone - the language an API that registers nothing answers in.
/// </summary>
[ShortRunJob]
[MemoryDiagnoser]
public class LanguageBenchmarks
{
    // the same queries, spelled in the two vocabularies the endpoint with a table answers
    private static readonly Dictionary<string, (string Invariant, string Translated)> Queries = new()
    {
        ["whereInt"] = ("where[x.Int==5]", "gdzie[x.Int==5]"),
        ["whereDate"] = ("where[x.Date>2023-10-12]", "gdzie[x.Date>2023-10-12]"),
        ["chain"] =
        (
            "where[x.Name==John&&x.Int>3].select[FullName=Name,x.Id].orderBy[x.Name].take[10..20]",
            "gdzie[x.Name==John&&x.Int>3].wybierz[FullName=Name,x.Id].sortujPo[x.Name].wez[10..20]"
        ),
        ["takeRange"] = ("take[10..20]", "wez[10..20]"),
        ["select27"] = (BuildWideSelect("select", ","), BuildWideSelect("wybierz", ","))
    };

    public static IEnumerable<string> QueryKeys => Queries.Keys;

    [ParamsSource(nameof(QueryKeys))]
    public string Key { get; set; } = null!;

    private IHLinqTokenizer _withoutTable = null!;
    private IHLinqTokenizer _withTable = null!;

    private static readonly CultureInfo Polish = CultureInfo.GetCultureInfo("pl");

    private static string BuildWideSelect(string clause, string separator) =>
        clause + "[" + string.Join(separator, Enumerable.Range(1, 27)
            .Select(i => $"R{i:D2}=P{i:D2}")) + "]";

    public static ServiceProvider Provider(params IHLinqWord[] table)
    {
        var services = new ServiceCollection();

        // HLinqCore is internal, unit tests reach it through InternalsVisibleTo, benches by reflection
        typeof(HLinqOptions).Assembly
            .GetType("HamsterWheel.HLinq.HLinqCore")!
            .GetMethod("ConfigureServices", BindingFlags.Public | BindingFlags.Static)!
            .Invoke(null, [services, new HLinqOptions()]);

        foreach (var word in table)
        {
            services.AddSingleton(word);
        }

        return services.BuildServiceProvider();
    }

    public static IHLinqWord[] PolishTable() =>
    [
        Word<Where>("gdzie"),
        Word<Select>("wybierz"),
        Word<Take>("wez"),
        Word<Skip>("pomin"),
        Word<Count>("zlicz"),
        Word<OrderBy>("sortujPo"),
        Word<OrderByDescending>("sortujPoMalejaco"),
        Word<ThenBy>("potemPo"),
        Word<ThenByDescending>("potemPoMalejaco")
    ];

    private static IHLinqWord Word<TToken>(string keyword) where TToken : TokenBase, new() =>
        new KeywordWord<TToken>(keyword, Polish);

    [GlobalSetup]
    public void Setup()
    {
        _withTable = Provider(PolishTable()).GetRequiredService<IHLinqTokenizer>();
        _withoutTable = Provider().GetRequiredService<IHLinqTokenizer>();
    }

    [Benchmark(Baseline = true)]
    public object TokenizeInvariantWords() => _withoutTable.Tokenize(Queries[Key].Invariant);

    [Benchmark]
    public object TokenizeInvariantWordsBesideAnotherLanguage() => _withTable.Tokenize(Queries[Key].Invariant, Polish);

    [Benchmark]
    public object TokenizeTranslatedWords() => _withTable.Tokenize(Queries[Key].Translated, Polish);
}

/// <summary>
/// What assembling one language costs: a grammar over the words of that culture and the invariant
/// ones, the self-check over them, and the text rules and clause partitions of the walk - the whole of
/// it paid while the API is composed, once per culture it spells a word of, over words that
/// are built once and shared by every language. The baseline is an API that spells no word of any
/// culture: the invariant language alone, which is what every API that registers nothing builds.
/// </summary>
[ShortRunJob]
[MemoryDiagnoser]
public class LanguageBuildBenchmarks
{
    [Benchmark(Baseline = true)]
    public IHLinqTokenizer TheInvariantLanguage() =>
        LanguageBenchmarks.Provider().GetRequiredService<IHLinqTokenizer>();

    [Benchmark]
    public IHLinqTokenizer ALanguageOfNineWords() =>
        LanguageBenchmarks.Provider(LanguageBenchmarks.PolishTable()).GetRequiredService<IHLinqTokenizer>();
}

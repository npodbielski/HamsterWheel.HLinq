using FluentAssertions;
using HamsterWheel.HLinq.Pipeline.Parser;
using HamsterWheel.HLinq.Pipeline.Tokenizer;
using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.UnitTests.TestUtils;
using Microsoft.Extensions.DependencyInjection;

namespace HamsterWheel.HLinq.UnitTests.Pipeline.Tokenizer;

/// <summary>
/// One question about the grammar per test, each answered as one line per class.
/// </summary>
public class GrammarFactsUnitTests
{
    private static readonly TestServicesCollection Services = new();

    [Fact]
    public void Roots_WhenInvariantLanguage_ThenTheRecordedOnes() =>
        RootsOf(Language()).Should().Be(GrammarFactsData.Roots);

    [Fact]
    public void Parents_WhenInvariantLanguage_ThenTheRecordedOnes() =>
        ParentsOf(Language()).Should().Be(GrammarFactsData.Parents);

    [Fact]
    public void Words_WhenInvariantLanguage_ThenTheRecordedOnes() =>
        WordsOf(Language()).Should().Be(GrammarFactsData.Words);

    [Fact]
    public void CanOpenQuery_WhenInvariantLanguage_ThenTheRecordedTokens() =>
        CanOpenQueryOf(Language()).Should().Be(GrammarFactsData.CanOpenQuery);

    [Fact]
    public void StandsAfter_WhenInvariantLanguage_ThenTheRecordedTokens() =>
        LinePerRuleBefore(Language()).Should().Be(GrammarFactsData.StandsAfter);

    [Fact]
    public void StandsBefore_WhenInvariantLanguage_ThenTheRecordedTokens() =>
        LinePerRuleAfter(Language()).Should().Be(GrammarFactsData.StandsBefore);

    [Fact]
    public void ClauseRoots_WhenInvariantLanguage_ThenTheRecordedOnes() =>
        ClauseRootsOf(Language()).Should().Be(GrammarFactsData.ClauseRoots);

    [Fact]
    public void Facts_WhenEveryLanguageOfTheTokenizer_ThenAllOfThemAnswerTheSame()
    {
        var languages = Languages();

        Func<HLinqTokenizer.Language, string>[] questions =
            [RootsOf, ParentsOf, WordsOf, CanOpenQueryOf, LinePerRuleBefore, LinePerRuleAfter, ClauseRootsOf];
        foreach (var question in questions)
        {
            var first = languages[0];
            foreach (var language in languages.Skip(1))
            {
                question(language).Should().Be(question(first),
                    $"{CultureName(language)} answers differently from {CultureName(first)}");
            }
        }
    }

    [Fact]
    public void Facts_WhenACultureOwnsWordsOfItsOwn_ThenItsGrammarStaysTheSameOne()
    {
        using var provider = ProviderWith(new LocalizedCountWord());

        var language = ((HLinqTokenizer)provider.GetRequiredService<IHLinqTokenizer>())
            .GetLanguages().First();

        ParentsOf(language).Should().Be(GrammarFactsData.Parents);
        CanOpenQueryOf(language).Should().Be(GrammarFactsData.CanOpenQuery);
        LinePerRuleBefore(language).Should().Be(GrammarFactsData.StandsAfter);
    }

    //one word of a culture that no language has by default, so a language with words of its own is
    //walked together with the invariant one
    private sealed class LocalizedCountWord() : TokenWord<global::HamsterWheel.HLinq.Tokens.Paging.Count>("zlicz");

    private static ServiceProvider ProviderWith(params IHLinqWord[] words)
    {
        var services = new ServiceCollection();
        HLinqCore.ConfigureServices(services, new HLinqOptions());
        foreach (var word in words)
        {
            services.AddSingleton(word);
        }

        return services.BuildServiceProvider();
    }

    private static HLinqTokenizer.Language[] Languages() => [.. ((HLinqTokenizer)Services.Tokenizer).GetLanguages()];

    private static HLinqTokenizer.Language Language() =>
        Languages().First(l => l.Grammar.Words.All(w => w.Culture is null));

    private static string CultureName(HLinqTokenizer.Language language) =>
        language.Grammar.Words.FirstOrDefault(w => w.Culture is not null)?.Culture?.Name ?? "Invariant";

    /// <summary>
    /// The elements whose parser names no parent, so a query can only start with one of them.
    /// </summary>
    internal static string RootsOf(HLinqTokenizer.Language language) =>
        Lines(Parsers().Where(p => p.IsRoot).Select(p => (p.ForElement().Name, "-")));

    /// <summary>
    /// Whose child an element is, read off the parser of the element itself.
    /// </summary>
    internal static string ParentsOf(HLinqTokenizer.Language language) =>
        Lines(Parsers()
            .Select(p => (p.ForElement().Name, Names(ValidParentsOf(p)))));

    /// <summary>
    /// The keyword a token is spelled with; a token with none is free text or bare characters.
    /// </summary>
    internal static string WordsOf(HLinqTokenizer.Language language) =>
        Lines(Rules(language)
            .Select(rule => (rule.ForType.Name, string.Join(" ", language.Grammar.KeywordsOf(rule.ForType))))
            .Where(line => line.Item2.Length > 0));

    internal static string CanOpenQueryOf(HLinqTokenizer.Language language) =>
        Names(Rules(language).Where(rule => rule.CanBeFirst).Select(rule => rule.ForType));

    /// <summary>
    /// Adjacency asked one window of one token at a time, so a widened matcher is a longer list.
    /// </summary>
    internal static string LinePerRuleBefore(HLinqTokenizer.Language language) =>
        LinePerRule(language, (grammar, rule) => Names(PrecedingTokens(grammar, rule)));

    internal static string LinePerRuleAfter(HLinqTokenizer.Language language) =>
        LinePerRule(language, (grammar, rule) => Names(FollowingTokens(grammar, rule)));

    private static string LinePerRule(HLinqTokenizer.Language language,
        Func<IGrammar, IGrammarRule, string> answer) =>
        Lines(Rules(language).Select(rule => (rule.ForType.Name, answer(language.Grammar, rule))));


    internal static string ClauseRootsOf(HLinqTokenizer.Language language) =>
        Lines(Rules(language).Select(rule =>
            (rule.ForType.Name, Names(AllRoots.Where(root => rule.MatchesRoot(root))))));

    private static IGrammarRule[] Rules(HLinqTokenizer.Language language) =>
        [.. language.Grammar.Rules.OrderBy(r => r.ForType.Name)];

    private static IElementParser[] Parsers() => [.. Services.Provider.GetServices<IElementParser>()];

    private static Type[] AllRoots { get; } =
    [
        typeof(global::HamsterWheel.HLinq.Tokens.Selecting.Select), typeof(global::HamsterWheel.HLinq.Tokens.Filtering.Where), typeof(global::HamsterWheel.HLinq.Tokens.Paging.Count),
        typeof(global::HamsterWheel.HLinq.Tokens.Paging.Skip), typeof(global::HamsterWheel.HLinq.Tokens.Paging.Take), typeof(global::HamsterWheel.HLinq.Tokens.Grouping.GroupBy),
        typeof(global::HamsterWheel.HLinq.Tokens.Ordering.OrderBy), typeof(global::HamsterWheel.HLinq.Tokens.Ordering.OrderByDescending),
        typeof(global::HamsterWheel.HLinq.Tokens.Ordering.ThenBy), typeof(global::HamsterWheel.HLinq.Tokens.Ordering.ThenByDescending)
    ];

    private static Type[] ValidParentsOf(IElementParser parser) => parser.ValidParents;

    private static IEnumerable<Type> PrecedingTokens(IGrammar grammar, IGrammarRule rule) =>
        TokenTypes(grammar)
            .Where(before => rule.PreviousTokensMatch([TokenOf(before)]))
            .Select(t => t);

    private static IEnumerable<Type> FollowingTokens(IGrammar grammar, IGrammarRule rule) =>
        TokenTypes(grammar)
            .Where(after => after != rule.ForType)
            .Where(after => grammar.GetRuleFor(after).PreviousTokensMatch([TokenOf(rule.ForType)]))
            .Select(t => t);

    private static Type[] TokenTypes(IGrammar grammar) => [.. grammar.Rules.Select(r => r.ForType)];

    private static IToken TokenOf(Type tokenType) => (IToken)tokenType.GetProperty("Empty")!.GetValue(null)!;

    private static string Names(IEnumerable<Type> types) =>
        OrNothing(string.Join(" ", types.Select(t => t.Name).OrderBy(n => n)));

    private static string OrNothing(string answer) => answer.Length == 0 ? "-" : answer;

    private static string Lines(IEnumerable<(string Name, string Answer)> lines) =>
        string.Join('\n', lines.OrderBy(l => l.Name).Select(l => $"{l.Name}: {OrNothing(l.Answer)}"));
}

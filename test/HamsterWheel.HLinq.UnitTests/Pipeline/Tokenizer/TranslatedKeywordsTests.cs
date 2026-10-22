using System.Globalization;
using FluentAssertions;
using HamsterWheel.HLinq.Pipeline.Tokenizer;
using HamsterWheel.HLinq.Request;
using HamsterWheel.HLinq.UnitTests.TestUtils;
using HamsterWheel.HLinq.UnitTests.TestUtils.Assertions;
using HamsterWheel.HLinq.UnitTests.TestUtils.Dummies;
using Microsoft.Extensions.DependencyInjection;
using static HamsterWheel.HLinq.UnitTests.TestUtils.Assertions.ExpectedToken;
using Entity = HamsterWheel.HLinq.Tokens.Entity;
using IToken = HamsterWheel.HLinq.Tokens.IToken;
using AndToken = HamsterWheel.HLinq.Tokens.Filtering.And;
using CountToken = HamsterWheel.HLinq.Tokens.Paging.Count;
using OrderByDescendingToken = HamsterWheel.HLinq.Tokens.Ordering.OrderByDescending;
using OrderByToken = HamsterWheel.HLinq.Tokens.Ordering.OrderBy;
using SelectToken = HamsterWheel.HLinq.Tokens.Selecting.Select;
using Word = HamsterWheel.HLinq.Tokens.TokenWord<HamsterWheel.HLinq.Tokens.Filtering.Where>;
using WhereToken = HamsterWheel.HLinq.Tokens.Filtering.Where;

namespace HamsterWheel.HLinq.UnitTests.Pipeline.Tokenizer;

/// <summary>
/// A word joins the invariant words of a query whose request names its culture, belongs to that culture
/// and no other, and makes a language while the endpoint is composed.
/// </summary>
public class TranslatedKeywordsTests
{
    private static readonly CultureInfo Pl = CultureInfo.GetCultureInfo("pl");
    private static readonly CultureInfo PlPl = CultureInfo.GetCultureInfo("pl-PL");
    private static readonly CultureInfo DeDe = CultureInfo.GetCultureInfo("de-DE");

    private static readonly IHLinqWord[] Polish =
    [
        Word<WhereToken>(Pl, "gdzie"),
        Word<SelectToken>(Pl, "wybierz"),
        Word<CountToken>(Pl, "zlicz"),
        Word<OrderByToken>(Pl, "sortujPo"),
        Word<OrderByDescendingToken>(Pl, "sortujPoMalejaco")
    ];

    [Fact]
    public void Tokenize_WhenAClauseIsSpelledInTheLanguageTheRequestNamed_ThenItIsAClause()
    {
        var query = "gdzie[x.Int>1]";

        Tokenize(query, Pl).Should().HaveSequenceOf(query, [
            new ExpectedToken(typeof(WhereToken), "gdzie"),
            LeftSquareBracket, Entity(), Dot, Prop("Int"), GreaterThan, NameOrValue("1"), RightSquareBracket
        ]);
    }

    [Fact]
    public void Tokenize_WhenACultureAddsItsWords_ThenTheInvariantWordsAreStillClauses()
    {
        var query = "where[x.Int>1]";

        Tokenize(query, Pl).Should().HaveSequenceOf(query, [
            Where, LeftSquareBracket, Entity(), Dot, Prop("Int"), GreaterThan, NameOrValue("1"), RightSquareBracket
        ]);
    }

    [Fact]
    public void Tokenize_WhenTheRequestNamesNoCulture_ThenItSpeaksTheInvariantWordsOnly()
    {
        var act = () => Tokenize("gdzie[x.Int>1]", null);

        act.Should().Throw<HLinqTokenizer.UnknownTokenException>();
    }

    [Fact]
    public void Tokenize_WhenAWordBelongsToAnotherCulture_ThenItIsNoWordAtAll()
    {
        var act = () => Tokenize("gdzie[x.Int>1]", DeDe);

        act.Should().Throw<HLinqTokenizer.UnknownTokenException>();
    }

    [Fact]
    public void Tokenize_WhenACultureHasNoLanguageOfItsOwn_ThenItIsAnsweredInTheNearestOne()
    {
        var query = "gdzie[x.Int>1]";

        Tokenize(query, PlPl).Should().HaveSequenceOf(query, [
            new ExpectedToken(typeof(WhereToken), "gdzie"),
            LeftSquareBracket, Entity(), Dot, Prop("Int"), GreaterThan, NameOrValue("1"), RightSquareBracket
        ]);
    }

    [Fact]
    public void Tokenize_WhenALanguageIsRegisteredForARegion_ThenTheLanguageBelowItDoesNotAskForItsWords()
    {
        //the words of a region are not inherited by the culture it lies in
        using var provider = ProviderWith([Word<WhereToken>(PlPl, "kudzie")]);
        var tokenizer = provider.GetRequiredService<IHLinqTokenizer>();

        var act = () => tokenizer.Tokenize("kudzie[x.Int>1]", Pl);

        act.Should().Throw<HLinqTokenizer.UnknownTokenException>();
    }

    [Fact]
    public void Tokenize_WhenAWordBeginsAnotherWordOfItsLanguage_ThenTheShorterOneWaits()
    {
        Tokenize("sortujPo[x.Int]", Pl)[0].Should().BeOfType<OrderByToken>();
        Tokenize("sortujPoMalejaco[x.Int]", Pl)[0].Should().BeOfType<OrderByDescendingToken>();
    }

    [Fact]
    public void Tokenize_WhenAWordOfTheLanguageEndsAValue_ThenTheValueStopsBeforeIt()
    {
        using var provider = ProviderWith([Word<AndToken>(Pl, "oraz")]);
        var tokenizer = provider.GetRequiredService<IHLinqTokenizer>();
        //the `o` that begins `oraz` cuts the value, and no invariant word would have made it a delimiter
        var query = "where[x.Int>1 oraz x.Int<2]";

        tokenizer.Tokenize(query, Pl).Should().HaveSequenceOf(query, [
            Where, LeftSquareBracket, Entity(), Dot, Prop("Int"), GreaterThan, NameOrValue("1"),
            new ExpectedToken(typeof(AndToken), "oraz"),
            Entity(), Dot, Prop("Int"), ExpectedToken.LessThan, NameOrValue("2"), RightSquareBracket
        ]);
    }

    [Fact]
    public void Tokenize_WhenAWordWouldEndAValueInAnotherLanguage_ThenHereItIsTextOfTheValue()
    {
        using var provider = ProviderWith([Word<AndToken>(Pl, "oraz")]);
        var tokenizer = provider.GetRequiredService<IHLinqTokenizer>();
        var query = "where[x.Int>1 oraz x.Int<2]";

        tokenizer.Tokenize(query, DeDe).Should().HaveSequenceOf(query, [
            Where, LeftSquareBracket, Entity(), Dot, Prop("Int"), GreaterThan,
            NameOrValue("1 oraz x.Int"), ExpectedToken.LessThan, NameOrValue("2"), RightSquareBracket
        ]);
    }

    [Fact]
    public void Tokenize_WhenACultureRegistersAWordForTheEntity_ThenItIsAWordToo()
    {
        var query = "gdzie[towar.Int>1]";
        var table = Polish.Append(Word<Entity>(Pl, "towar")).ToArray();
        using var provider = ProviderWith(table);
        var tokenizer = provider.GetRequiredService<IHLinqTokenizer>();

        tokenizer.Tokenize(query, Pl).Should().HaveSequenceOf(query, [
            new ExpectedToken(typeof(WhereToken), "gdzie"),
            LeftSquareBracket, Entity("towar"), Dot, Prop("Int"), GreaterThan, NameOrValue("1"), RightSquareBracket
        ]);
    }

    [Fact]
    public void Tokenize_WhenAWordOfTheLanguageIsAWordOfItsOwn_ThenItIsReadToo()
    {
        using var provider = ProviderWith(new ElsewhereWhere());
        var tokenizer = provider.GetRequiredService<IHLinqTokenizer>();
        var query = "gdzie[x.Int>1]";

        tokenizer.Tokenize(query, Pl).Should().HaveSequenceOf(query, [
            new ExpectedToken(typeof(WhereToken), "gdzie"),
            LeftSquareBracket, Entity(), Dot, Prop("Int"), GreaterThan, NameOrValue("1"), RightSquareBracket
        ]);
    }

    [Fact]
    public void Compose_WhenAWordSwallowsAWordItCompetesWith_ThenTheEndpointIsNotComposed()
    {
        //`wh` completes where `where` could still continue, and such a language is refused while the
        //endpoint is composed, not by a request answered 500
        using var provider = ProviderWith(new SwallowingWhere());

        var act = () => GrammarValidator.Validate(provider.GetRequiredService<IHLinqTokenizer>());

        act.Should().Throw<GrammarValidationException>()
            .WithMessage("*'wh' completes where 'where' could still continue*");
    }

    [Fact]
    public void GetRequest_WhenABareClauseStandsInAnotherLanguage_ThenItIsNotAParameterOfTheEndpoint()
    {
        var withTable = RequestOf(ProviderWith(Polish), "culture=pl&page=2&zlicz");
        var withoutTable = RequestOf(new TestServicesCollection().Provider, "page=2&zlicz");

        //a clause of the language this endpoint answers survives the parameters being taken out of it
        withTable.Query.Should().Be("zlicz");
        withoutTable.Query.Should().Be("");
    }

    [Fact]
    public void Parse_WhenAClauseIsSpelledInTheLanguageOfTheRequest_ThenTheQueryIsFull()
    {
        var provider = ProviderWith(Polish);

        var query = HLinqQuery<DummyEntity>.Parse(
            provider.GetRequiredService<HLinqBinderDependenciesBag>(), "culture=pl&gdzie[x.Int>1]");

        query.Culture.Should().Be("pl");
        query.SourceQueryString.Should().Be("gdzie[x.Int>1]");
        query.Children.Should().ContainSingle();
    }

    private static IToken[] Tokenize(string query, CultureInfo? culture)
    {
        using var provider = ProviderWith(Polish);
        return provider.GetRequiredService<IHLinqTokenizer>().Tokenize(query, culture);
    }

    private static HLinqRequest RequestOf(IServiceProvider provider, string queryString) =>
        new HLinqQueryParameters(
            provider.GetServices<IHLinqWord>(),
            new CultureParameterKeyword()).GetRequest(queryString);

    private static ServiceProvider ProviderWith(params IHLinqWord[] table)
    {
        var services = new ServiceCollection();
        HLinqCore.ConfigureServices(services, new HLinqOptions());
        foreach (var word in table)
        {
            services.AddSingleton(word);
        }

        return services.BuildServiceProvider();
    }

    private static IHLinqWord Word<TToken>(CultureInfo culture, string keyword)
        where TToken : HamsterWheel.HLinq.Tokens.TokenBase, new() =>
        new HamsterWheel.HLinq.Tokens.KeywordWord<TToken>(keyword, culture);

    private sealed class SwallowingWhere() : Word("wh", Pl);

    private sealed class ElsewhereWhere() : Word("gdzie", Pl);
}

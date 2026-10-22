using System.Globalization;
using FluentAssertions;
using HamsterWheel.HLinq.AspNet;
using HamsterWheel.HLinq.Pipeline.Tokenizer;
using HamsterWheel.HLinq.Lang.Pl;
using HamsterWheel.HLinq.Tokens;
using Microsoft.Extensions.DependencyInjection;
using CountToken = HamsterWheel.HLinq.Tokens.Paging.Count;
using GroupByToken = HamsterWheel.HLinq.Tokens.Grouping.GroupBy;
using OrderByDescendingToken = HamsterWheel.HLinq.Tokens.Ordering.OrderByDescending;
using OrderByToken = HamsterWheel.HLinq.Tokens.Ordering.OrderBy;
using SelectToken = HamsterWheel.HLinq.Tokens.Selecting.Select;
using SkipToken = HamsterWheel.HLinq.Tokens.Paging.Skip;
using TakeToken = HamsterWheel.HLinq.Tokens.Paging.Take;
using ThenByDescendingToken = HamsterWheel.HLinq.Tokens.Ordering.ThenByDescending;
using ThenByToken = HamsterWheel.HLinq.Tokens.Ordering.ThenBy;
using WhereToken = HamsterWheel.HLinq.Tokens.Filtering.Where;

namespace HamsterWheel.HLinq.UnitTests.Lang.Pl;

/// <summary>
/// A language as a package: one call installs its words, the words belong to the culture they name and
/// to no other, the words the endpoint always answered stay words beside them, and the language is
/// checked while the endpoint is composed rather than by a query answered 500.
/// </summary>
public class PolishTranslationUnitTests
{
    private static readonly CultureInfo Pl = CultureInfo.GetCultureInfo("pl");
    private static readonly CultureInfo DeDe = CultureInfo.GetCultureInfo("de-DE");

    [Fact]
    public void WhenThePackIsInstalled_ThenAClauseSpelledInPolishIsAClause()
    {
        Tokens("gdzie[x.Int>1]", Pl)[0].Should().BeOfType<WhereToken>();
    }

    [Fact]
    public void WhenThePackIsInstalled_ThenTheWordsEveryRequestIsReadWithAreStillClauses()
    {
        Tokens("where[x.Int>1]", Pl)[0].Should().BeOfType<WhereToken>();
    }

    [Fact]
    public void WhenThePackIsInstalled_ThenItsWordsBelongToTheCultureTheyName()
    {
        var act = () => Tokens("gdzie[x.Int>1]", DeDe);

        act.Should().Throw<HLinqTokenizer.UnknownTokenException>();
    }

    [Fact]
    public void WhenThePackIsInstalled_ThenEveryClauseOfTheLanguageIsReadInOneQuery()
    {
        var query =
            "gdzie[x.Int>1].pomin[1].wez[1].sortujPo[x.Int].potemPo[x.Int].potemPoMalejaco[x.Int].wybierz[x.Int]";

        var types = Tokens(query, Pl).Select(t => t.GetType()).ToArray();

        types.Should().Contain([
            typeof(WhereToken), typeof(SkipToken), typeof(TakeToken), typeof(OrderByToken), typeof(ThenByToken),
            typeof(ThenByDescendingToken), typeof(SelectToken)
        ]);
    }

    [Fact]
    public void WhenThePackIsInstalled_ThenACountSpelledInPolishCounts()
    {
        Tokens("zlicz[]", Pl)[0].Should().BeOfType<CountToken>();
    }

    [Fact]
    public void WhenThePackIsInstalled_ThenAGroupingSpelledInPolishIsAGrouping()
    {
        Tokens("grupujPo[x.Int]", Pl)[0].Should().BeOfType<GroupByToken>();
    }

    [Fact]
    public void WhenThePackIsInstalled_ThenAWordBeginsAnotherWordAndBothAreRead()
    {
        //the shorter word completes where the longer one could still continue, so it waits
        Tokens("gdzie[x.Int>1].sortujPo[x.Int]", Pl).Should().Contain(t => t.GetType() == typeof(OrderByToken));
        Tokens("gdzie[x.Int>1].sortujPoMalejaco[x.Int]", Pl)
            .Should().Contain(t => t.GetType() == typeof(OrderByDescendingToken));
    }

    [Fact]
    public void WhenThePackIsInstalled_ThenAPolishQueryOfTheDemoIsAWholeQuery()
    {
        var types = Tokens("gdzie[x.Name==Superman].sortujPo[x.Name].wez[2]", Pl).Select(t => t.GetType()).ToArray();

        types.Should().Contain([typeof(WhereToken), typeof(OrderByToken), typeof(TakeToken)]);
    }

    private static IToken[] Tokens(string query, CultureInfo culture)
    {
        var services = new ServiceCollection();
        services.ConfigureHLinq(c => c.AddPolishToHLinq());
        using var provider = services.BuildServiceProvider();

        return provider.GetRequiredService<IHLinqTokenizer>().Tokenize(query, culture);
    }
}

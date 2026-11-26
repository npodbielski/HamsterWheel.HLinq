using System.Globalization;
using FluentAssertions;
using HamsterWheel.HLinq.AspNet;
using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Lang.Pl;
using HamsterWheel.HLinq.Pipeline.Parser;
using HamsterWheel.HLinq.Pipeline.Tokenizer;
using HamsterWheel.HLinq.Request;
using HamsterWheel.HLinq.UnitTests.TestUtils.Dummies;
using Microsoft.Extensions.DependencyInjection;

namespace HamsterWheel.HLinq.UnitTests.Lang.Pl;

/// <summary>
/// A refused member list quotes the query's own words, in any language.
/// </summary>
public class PolishDuplicateMemberNameUnitTests
{
    private static readonly CultureInfo Pl = CultureInfo.GetCultureInfo("pl");

    [Theory]
    [InlineData("wybierz[Z=x.Children.wybierz(a.Name,a.Id),Z=x.Name]",
        "'Z' answers two values - 'x.Children.wybierz(a.Name,a.Id)' and 'x.Name'*")]
    //a list is refused as it closes, so the inner one answers first when both hold a duplicate
    [InlineData("wybierz[Z=x.Children.wybierz(Imie=a.Name,Imie=a.Id),Z=x.Name]",
        "'Imie' answers two values - 'a.Name' and 'a.Id';*")]
    public void Parse_WhenTheValuesAreSpelledWithTheWordsOfALanguage_ThenTheReasonQuotesThemAsWritten(
        string query,
        string expected)
    {
        //arrange
        var services = new ServiceCollection();
        services.ConfigureHLinq(c => c.AddPolishToHLinq());
        using var provider = services.BuildServiceProvider();

        //act
        var parsers = provider.GetServices<IElementParser>();
        var tokens = provider.GetRequiredService<IHLinqTokenizer>().Tokenize(query, Pl);
        var act = () => new HLinqParser(parsers).Parse(new HLinqQuery<DummyEntity>
        {
            SourceQueryString = query
        }, tokens);

        //assert
        act.Should().Throw<MemberNameCollisionException>().WithMessage(expected);
    }
}

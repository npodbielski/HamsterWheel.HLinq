using FluentAssertions;
using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Pipeline.Tokenizer;
using HamsterWheel.HLinq.Request;
using HamsterWheel.HLinq.UnitTests.TestUtils;
using HamsterWheel.HLinq.UnitTests.TestUtils.Dummies;
using Microsoft.Extensions.DependencyInjection;

namespace HamsterWheel.HLinq.UnitTests.Requests;

public class HLinqQueryParametersUnitTests
{
    [Theory]
    [InlineData("page=2&where[x.Int>10]", "where[x.Int>10]")]
    [InlineData("where[x.Int>10]&page=2", "where[x.Int>10]")]
    [InlineData("sort=asc&where[x.Int>10].take[5]&culture=pl-PL", "where[x.Int>10].take[5]")]
    [InlineData("person.name=John&where[x.Int>10]", "where[x.Int>10]")]
    [InlineData("cultures=pl-PL&where[x.Int>10]", "where[x.Int>10]")]
    [InlineData("page=2", "")]
    public void Parse_WhenTheRequestCarriesParametersOfTheEndpoint_ThenTheQueryIsParsedWithoutThem(
        string queryString,
        string expectedQuery)
    {
        Parse(queryString).SourceQueryString.Should().Be(expectedQuery);
    }

    [Theory]
    //`&&` is an operator of the query language, and a url that would read it as a separator of two
    //parameters is answered as the one clause it was written as
    [InlineData("where[x.Int>1&&x.Int<5]", "where[x.Int>1&&x.Int<5]")]
    [InlineData("page=2&where[x.Int>1&&x.Int<5]&sort=asc", "where[x.Int>1&&x.Int<5]")]
    [InlineData("where[ ( x.Int == 22 || x.Int == 1) && x.Int > 3 ]",
        "where[ ( x.Int == 22 || x.Int == 1) && x.Int > 3 ]")]
    [InlineData("where[Name==\"a&b\"]", "where[Name==\"a&b\"]")]
    [InlineData("where[x.Int>1&&x.Int<5]&culture=pl", "where[x.Int>1&&x.Int<5]")]
    public void Parse_WhenAClauseCarriesAnAmpersandOfItsOwn_ThenItSeparatesNothing(
        string queryString,
        string expectedQuery)
    {
        Parse(queryString).SourceQueryString.Should().Be(expectedQuery);
    }

    [Theory]
    [InlineData("wher[x.Int>1]&page=2")]
    [InlineData("ids[]=1&where[x.Int>10]")]
    [InlineData("page%3D2%26where[x.Int>10]")]
    [InlineData("where[x.Int>10]&take[5]")]
    public void Parse_WhenAParameterCouldOnlyBeReadByDroppingAClause_ThenThrowsUnknownToken(string queryString)
    {
        var parse = () => Parse(queryString);

        parse.Should().Throw<HLinqTokenizer.UnknownTokenException>();
    }

    [Fact]
    public void Parse_WhenAParameterStandsBesideAClauseRootWithoutBrackets_ThenTheQueryIsStillRefusedLoudly()
    {
        var parse = () => Parse("page=2&count");

        parse.Should().Throw<NonParsableTokenSequenceException>();
    }

    [Fact]
    public void Parse_WhenParametersAreAllTheRequestHas_ThenItAsksForEverything()
    {
        var parsed = Parse("page=2&sort=asc");

        parsed.Children.Should().BeEmpty();
    }

    [Theory]
    [InlineData("culture=pl-PL&where[x.Int>10]", "pl-PL")]
    [InlineData("where[x.Int>10]&culture=pl", "pl")]
    [InlineData("where[x.Int>10]&culture=ch-wakau", "ch-wakau")]
    [InlineData("page=2&culture=pl&take[5]", "pl")]
    [InlineData("culture=pl&culture=en&where[x.Int>10]", "pl")]
    [InlineData("culture=en&culture=pl&where[x.Int>10]", "en")]
    [InlineData("culture=pl-PL", "pl-PL")]
    [InlineData("culture=", null)]
    [InlineData("cultures=pl-PL&where[x.Int>10]", null)]
    public void Parse_WhenTheRequestAsksForACulture_ThenThatIsTheCultureOfTheQuery(
        string queryString,
        string? expectedCulture)
    {
        Parse(queryString).Culture.Should().Be(expectedCulture);
    }

    [Fact]
    public void Parse_WhenTheGrammarSpeaksAnotherLanguage_ThenTheRequestAsksForItsCultureInThatLanguage()
    {
        var services = new ServiceCollection();
        HLinqCore.ConfigureServices(services, new HLinqOptions());
        services.AddSingleton<ICultureParameterKeyword, FormatKeyword>();
        var dependenciesBag = services.BuildServiceProvider().GetRequiredService<HLinqBinderDependenciesBag>();

        var parsed = HLinqQuery<DummyEntity>.Parse(dependenciesBag, "format=pl-PL&where[x.Int>10]");

        parsed.Culture.Should().Be("pl-PL");
        parsed.SourceQueryString.Should().Be("where[x.Int>10]");
    }

    private static HLinqQuery<DummyEntity> Parse(string queryString) =>
        HLinqQuery<DummyEntity>.Parse(new TestServicesCollection().BinderDependenciesBag, queryString);

    private class FormatKeyword : ICultureParameterKeyword
    {
        public string Value => "format";
    }
}

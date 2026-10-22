using FluentAssertions;
using HamsterWheel.HLinq.Diagnostics;
using HamsterWheel.HLinq.Pipeline.Applier.Builders;
using HamsterWheel.HLinq.Pipeline.Parser;
using HamsterWheel.HLinq.Tree.Filtering;
using HamsterWheel.HLinq.UnitTests.Reflection;
using HamsterWheel.HLinq.UnitTests.TestUtils;

namespace HamsterWheel.HLinq.UnitTests.Diagnostics;

/// <summary>
/// A parse says which member its query got only when the rules could not say it: two members answered,
/// every rule ranked them equal, and an ordinal comparison chose.
/// </summary>
public class MemberWarningsUnitTests
{
    private static readonly TestServicesCollection Services = new();
    private readonly HLinqWarnings _warnings = new();

    private readonly ExpressionBuilder _sut = new(Services.PropertiesCache, Services.MethodsCache,
        Services.ConverterFactory, new ParametersConverter(), []);

    private readonly IHLinqParser _parser = new HLinqParser(Services.Parsers);

    private void Filter(string query) => Filter<RankedEntity>(query);

    private void Filter<TEntity>(string query) where TEntity : class
    {
        var tokens = Services.Tokenizer.Tokenize(query);
        var tree = _parser.TestParseEntryPoint<TEntity>(query, tokens);

        _sut.GetFilter(typeof(TEntity), tree.GetAll<WhereRoot>().First(), query, warnings: _warnings);
    }

    [Fact]
    public void WhenTwoMembersAnswerANameAndNeitherIsSpelledAsTheQueryWroteIt_ThenTheWarningNamesBoth()
    {
        //act
        Filter("where[x.nAmE==abc]");

        //assert
        var warning = _warnings.Reported.Single();
        warning.Reason.Should().Be(HLinqMemberWarningReason.AmbiguousName);
        warning.Member.Should().Be("Name");
        warning.Competitors.Should().BeEquivalentTo(["Name", "name"]);
        warning.Type.Should().Be(typeof(RankedEntity).ToString());
        warning.Query.Should().Be("where[x.nAmE==abc]");
    }

    [Fact]
    public void WhenANameIsSpelledExactlyAsAMemberSpellsItself_ThenNothingIsReported()
    {
        //act
        Filter("where[x.Name==abc]");

        //assert
        _warnings.Reported.Should().BeEmpty();
    }

    [Fact]
    public void WhenARuleChoseBetweenOverloads_ThenNothingIsReported()
    {
        //act
        Filter("where[x.Service.Sub(abc)]");

        //assert
        _warnings.Reported.Should().BeEmpty();
    }

    [Fact]
    public void WhenTwoOverloadsBindTheArgumentAndNoRuleSeparatesThem_ThenTheWarningNamesTheCall()
    {
        //act - the argument is a one-character `char` and a member of the enum at once, so no rule tells
        //the two overloads apart and an ordinal comparison of their signatures decides
        Filter<TiedEntity>("where[x.Service.Sub(A)]");

        //assert
        var warning = _warnings.Reported.Single();
        warning.Reason.Should().Be(HLinqMemberWarningReason.AmbiguousCall);
        warning.Member.Should().Be("Sub(Char)");
        warning.Competitors.Should().BeEquivalentTo(["Sub(Char)", "Sub(TiedEnum)"]);
    }

}

public class TiedEntity
{
    public TiedService Service { get; set; } = new();
}

public class TiedService
{
    public bool Sub(char value) => true;
    public bool Sub(TiedEnum value) => true;
}

public enum TiedEnum
{
    A,
    B,
}

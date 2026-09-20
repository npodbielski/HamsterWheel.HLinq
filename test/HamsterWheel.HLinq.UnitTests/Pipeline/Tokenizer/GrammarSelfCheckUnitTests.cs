using FluentAssertions;
using HamsterWheel.HLinq.Pipeline.Tokenizer;
using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Filtering;
using HamsterWheel.HLinq.Tokens.Paging;
using HamsterWheel.HLinq.UnitTests.TestUtils;
using HamsterWheel.HLinq.UnitTests.TestUtils.Assertions;
using Microsoft.Extensions.DependencyInjection;

namespace HamsterWheel.HLinq.UnitTests.Pipeline.Tokenizer;

/// <summary>
///     Grammar self-check: the grammar must describe itself consistently - one rule per token,
///     every token reachable, no keyword swallowing a longer one, and delimiter sets exactly as
///     the adjacency rules derive them. Runs against the effective registered possibilities, so
///     a localized grammar (tokens replaced through <c>AddTokenPossibility</c>) validates too.
/// </summary>
public class GrammarSelfCheckUnitTests
{
    private static readonly TestServicesCollection TestServicesCollection = new();

    [Fact]
    public void Validate_WhenDefaultGrammar_ThenNoProblems()
    {
        var grammar = TestServicesCollection.Provider.GetRequiredService<IGrammar>();

        var act = () => grammar.Validate();

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_WhenGrammarLocalized_ThenNoProblems()
    {
        var services = new ServiceCollection();
        HamsterWheel.HLinq.HLinqCore.ConfigureServices(services, new HamsterWheel.HLinq.HLinqOptions());
        var localized = services
            .Where(d => d.ServiceType == typeof(IHLinqTokenPossibility) &&
                        d.ImplementationType == typeof(Count.Possibility))
            .ToList();
        foreach (var descriptor in localized) services.Remove(descriptor);
        services.AddSingleton<IHLinqTokenPossibility, LocalizedCountPossibility>();
        using var provider = services.BuildServiceProvider();

        var act = () => provider.GetRequiredService<IGrammar>().Validate();

        act.Should().NotThrow();
    }

    [Fact]
    public void GetDelimiters_WhenNameOrValue_ThenDerivedFromAdjacency()
    {
        var grammar = TestServicesCollection.Provider.GetRequiredService<IGrammar>();

        grammar.GetDelimiters(typeof(NameOrValue)).Should()
            .BeEquivalentTo(new[] { '=', '&', '|', ',', '!', '<', '>', ')', ']' });
    }

    [Fact]
    public void GetDelimiters_WhenPropertyName_ThenIncludesDotAndEndsOfExpression()
    {
        var grammar = TestServicesCollection.Provider.GetRequiredService<IGrammar>();

        grammar.GetDelimiters(typeof(PropertyName)).Should()
            .BeEquivalentTo(new[] { '=', '&', '|', ',', '!', '<', '>', ')', ']', '.' });
    }

    [Fact]
    public void GetDelimiters_WhenMethodName_ThenOnlyLeftParenthesis()
    {
        var grammar = TestServicesCollection.Provider.GetRequiredService<IGrammar>();

        grammar.GetDelimiters(typeof(MethodName)).Should().BeEquivalentTo(new[] { '(' });
    }

    [Fact]
    public void Equality_WhenDoubleEqualsAfterNameOrValue_ThenStaysEqualityNotTwoAssignments()
    {
        var tokenizer = new HLinqTokenizer(TestServicesCollection.TokenPossibilities);

        var tokens = tokenizer.Tokenize("where[Name==5]");

        tokens.Should().HaveSequenceOf("where[Name==5]", [
            ExpectedToken.Where,
            ExpectedToken.LeftSquareBracket,
            ExpectedToken.NameOrValue("Name"),
            ExpectedToken.Equality,
            ExpectedToken.NameOrValue("5"),
            ExpectedToken.RightSquareBracket
        ]);
    }

    private sealed class LocalizedCountPossibility(IGrammar grammar)
        : TokenPossibility<Count>(grammar, "zlicz");
}

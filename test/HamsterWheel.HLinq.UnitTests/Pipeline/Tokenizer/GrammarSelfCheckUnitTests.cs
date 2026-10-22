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
/// Grammar self-check: the grammar must describe itself consistently - one rule per token,
/// every token reachable, no keyword swallowing a longer one, and delimiter sets exactly as
/// the adjacency rules derive them. Runs against the effective registered words, so
/// a localized grammar (tokens replaced through <c>AddWord</c>) validates too.
/// </summary>
public class GrammarSelfCheckUnitTests
{
    private static readonly TestServicesCollection TestServicesCollection = new();

    [Fact]
    public void Validate_WhenDefaultGrammar_ThenNoProblems()
    {
        var tokenizer = TestServicesCollection.Tokenizer;

        var act = () => GrammarValidator.Validate(tokenizer);

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_WhenGrammarLocalized_ThenNoProblems()
    {
        var services = new ServiceCollection();
        HLinqCore.ConfigureServices(services, new HLinqOptions());
        var localized = services
            .Where(d => d.ServiceType == typeof(IHLinqWord) &&
                        d.ImplementationType == typeof(Count.Word))
            .ToList();
        foreach (var descriptor in localized) services.Remove(descriptor);
        services.AddSingleton<IHLinqWord, LocalizedCountWord>();
        using var provider = services.BuildServiceProvider();

        // ReSharper disable once AccessToDisposedClosure
        var act = () => GrammarValidator.Validate(TestServicesCollection.Tokenizer);

        act.Should().NotThrow();
    }

    [Fact]
    public void GetDelimiters_WhenNameOrValue_ThenDerivedFromAdjacency()
    {
        var grammar = TestServicesCollection.Provider.GetRequiredService<IGrammar>();

        grammar.GetDelimiters(typeof(NameOrValue)).Should()
            .BeEquivalentTo(['=', '&', '|', ',', '!', '<', '>', ')', ']']);
    }

    [Fact]
    public void GetDelimiters_WhenPropertyName_ThenIncludesDotAndEndsOfExpression()
    {
        var grammar = TestServicesCollection.Provider.GetRequiredService<IGrammar>();

        grammar.GetDelimiters(typeof(PropertyName)).Should()
            .BeEquivalentTo(['=', '&', '|', ',', '!', '<', '>', ')', ']', '.']);
    }

    [Fact]
    public void GetDelimiters_WhenMethodName_ThenOnlyLeftParenthesis()
    {
        var grammar = TestServicesCollection.Provider.GetRequiredService<IGrammar>();

        grammar.GetDelimiters(typeof(MethodName)).Should().BeEquivalentTo(['(']);
    }

    [Fact]
    public void Equality_WhenDoubleEqualsAfterNameOrValue_ThenStaysEqualityNotTwoAssignments()
    {
        var tokenizer = TestServicesCollection.Tokenizer;

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

    private sealed class LocalizedCountWord() : TokenWord<Count>("zlicz");
}
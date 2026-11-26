using FluentAssertions;
using HamsterWheel.HLinq.Pipeline.Tokenizer;
using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Filtering;
using HamsterWheel.HLinq.Tokens.Paging;
using HamsterWheel.HLinq.Tokens.Selecting;
using HamsterWheel.HLinq.UnitTests.TestUtils;
using HamsterWheel.HLinq.UnitTests.TestUtils.Assertions;
using Microsoft.Extensions.DependencyInjection;

namespace HamsterWheel.HLinq.UnitTests.Pipeline.Tokenizer;

/// <summary>
/// A grammar must describe itself consistently, checked on the registered words.
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

        //the operators of a math expression end a member; they are derived from adjacency and
        //belong to no free text - a written date or GUID stays one value of its clause
        grammar.GetDelimiters(typeof(PropertyName)).Should()
            .BeEquivalentTo(['=', '&', '|', ',', '!', '<', '>', ')', ']', '.', '+', '-', '*', '/']);
    }

    [Fact]
    public void GetDelimiters_WhenMathOperand_ThenDerivedFromAdjacency()
    {
        var grammar = TestServicesCollection.Provider.GetRequiredService<IGrammar>();

        grammar.GetDelimiters(typeof(MathOperand)).Should()
            .BeEquivalentTo([',', ']', ')', '+', '-', '*', '/', '=', '!', '<', '>', '&', '|']);
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
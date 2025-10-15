using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;
using HamsterWheel.HLinq.Tokens;

namespace HamsterWheel.HLinq.UnitTests.Assertions;

public class HLinqTokenArrayAssertions(IToken[] instance) :
    ReferenceTypeAssertions<IToken[], HLinqTokenArrayAssertions>(instance, AssertionChain.GetOrCreate())
{
    protected override string Identifier => "hLinqTokenArray";

    public AndConstraint<HLinqTokenArrayAssertions> HaveSequenceOf(string query,
        IEnumerable<ExpectedToken> expected, bool assertTokensValues = true)
    {
        var expectedTokens = expected as ExpectedToken[] ?? expected.ToArray();
        Subject.Length.Should().Be(expectedTokens.Length,
            " of query that was '{0}' with tokens:\r\n- {1}\r\n instead found tokens\r\n- {2}\r\n ",
            string.Join("", Subject.Select(t => t.GetValue(query))),
            string.Join("\r\n- ", expectedTokens.Select(t => $"{t.Type.Name}:{t.TokenValue}")),
            string.Join("\r\n- ", Subject.Select(t => $"{t.GetType().Name}:{t.GetValue(query)}"))
        );
        for (var index = 0; index < Subject.Length; index++)
        {
            var token = Subject[index];
            var and = token.Should().BeOfType(expectedTokens.ElementAt(index).Type).And;
            if (assertTokensValues)
            {
                and.HaveValueOf(query, expectedTokens.ElementAt(index).TokenValue);
            }
        }

        return new AndConstraint<HLinqTokenArrayAssertions>(this);
    }
}
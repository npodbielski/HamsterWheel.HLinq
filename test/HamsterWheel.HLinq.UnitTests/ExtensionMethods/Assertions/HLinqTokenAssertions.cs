using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;
using HamsterWheel.HLinq.Tokens;

namespace HamsterWheel.HLinq.UnitTests.Assertions;

public class HLinqTokenAssertions(IToken instance) :
    ReferenceTypeAssertions<IToken, HLinqTokenAssertions>(instance, AssertionChain.GetOrCreate())
{
    protected override string Identifier => "hLinqToken";

    public AndConstraint<HLinqTokenAssertions> HaveValueOf(string wholeQuery,
        string expected, string because = "", params object[] becauseArgs)
    {
        Subject.GetValue(wholeQuery).ToLower().Should().Be(expected.ToLower());
        return new AndConstraint<HLinqTokenAssertions>(this);
    }
}
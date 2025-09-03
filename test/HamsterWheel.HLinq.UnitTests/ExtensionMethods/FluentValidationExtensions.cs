using HamsterWheel.HLinq.Parsers;
using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.UnitTests.Assertions;

namespace HamsterWheel.HLinq.UnitTests;

public static class FluentValidationExtensions
{
    public static HLinqTokenAssertions Should(this IToken instance) => new(instance);

    public static HLinqTokenArrayAssertions Should(this IToken[] instance) => new(instance);

    public static HLinqITreeBranchAssertions Should(this ITreeBranch instance) => new(instance);
}
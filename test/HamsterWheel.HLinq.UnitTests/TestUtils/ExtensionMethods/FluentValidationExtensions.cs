using HamsterWheel.HLinq.Pipeline.Parser;
using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.UnitTests.TestUtils.Assertions;

namespace HamsterWheel.HLinq.UnitTests.TestUtils;

public static class FluentValidationExtensions
{
    public static HLinqTokenAssertions Should(this IToken instance) => new(instance);

    public static HLinqTokenArrayAssertions Should(this IToken[] instance) => new(instance);

    public static HLinqITreeBranchAssertions Should(this ITreeBranch instance) => new(instance);
}
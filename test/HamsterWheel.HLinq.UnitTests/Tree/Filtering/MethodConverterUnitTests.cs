using FluentAssertions;
using HamsterWheel.HLinq.Pipeline.Applier.Builders;
using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Filtering;
using HamsterWheel.HLinq.Tree.Filtering;
using HamsterWheel.HLinq.UnitTests.TestUtils.Dummies;

namespace HamsterWheel.HLinq.UnitTests.Tree.Filtering;

public class MethodConverterUnitTests
{
    [Fact]
    public void Build_WhenMethodHaveIncorrectTokenSource_ThenThrows()
    {
        //arrange
        var sut = new Method.Converter(null!, null!);
        var element = new Method([NameOrValue.Empty, MethodName.Empty]);
        var action = () => sut.Build(
            new BuilderContext(null!, typeof(DummyEntity), "where[]",
                new ExpressionBuilder(null!, null!, null!, new ParametersConverter(), null!)), element);

        //act
        var exception = action.Should().Throw<Method.InvalidMethodQueryException>();

        //assert
        exception.WithMessage("Could not bound method:*");
    }
}
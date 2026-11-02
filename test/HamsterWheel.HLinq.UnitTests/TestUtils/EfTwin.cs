using System.Linq.Expressions;
using FluentAssertions;
using HamsterWheel.HLinq.Reflection;

namespace HamsterWheel.HLinq.UnitTests.TestUtils;

/// <summary>
/// Compares the tree the binder builds against the tree the C# compiler built for the same call
/// written by hand in Linq. The hand-written call is the oracle: the compiler resolved the overload
/// and typed the arguments when it was written, and the binder has to arrive at the same tree from
/// the query text.
/// </summary>
public static class EfTwin
{
    /// <summary>
    /// Asserts the two lambdas are the same tree: the same methods, the same arguments in the same
    /// order, a column where the twin reads one and a value where the twin writes one.
    /// </summary>
    /// <remarks>
    /// Two normalizations, both of how the two trees are written rather than of what they mean: the
    /// twin calls the extension method on the <c>DbFunctions</c> instance while the binder hands that
    /// parameter as a null constant, and the compiler inserts a conversion for a comparison of a
    /// nullable result while the binder converts the written text to that type.
    /// </remarks>
    public static void AssertSameTree(LambdaExpression twin, LambdaExpression actual) =>
        Same(twin.Body, actual.Body, "body");

    private static void Same(Expression expected, Expression actual, string path)
    {
        switch (Unwrap(expected))
        {
            case MethodCallExpression expectedCall:
                var actualCall = actual.Should().BeAssignableTo<MethodCallExpression>($"the twin calls {expectedCall.Method.Name} at {path}").Subject;
                actualCall.Method.DeclaringType.Should().Be(expectedCall.Method.DeclaringType,
                    $"the call at {path} is declared on the class the twin resolved");
                actualCall.Method.Name.Should().Be(expectedCall.Method.Name,
                    $"the call at {path} is the method the twin resolved");
                actualCall.Method.GetParameters().Select(p => p.ParameterType)
                    .Should().Equal(expectedCall.Method.GetParameters().Select(p => p.ParameterType),
                        $"the call at {path} is the overload the twin resolved");
                var expectedArguments = (IReadOnlyList<Expression>)expectedCall.Arguments;
                var actualArguments = (IReadOnlyList<Expression>)actualCall.Arguments;
                if (expectedCall.Method.GetParameters() is [{ } first, ..] &&
                    EfDbFunctionsMatcher.IsEfDbFunctionParameter(first))
                {
                    actualArguments[0].Should()
                        .BeAssignableTo<ConstantExpression>(
                            $"the binder hands the this-parameter of {path} as a null constant")
                        .Which.Value.Should().BeNull();
                    expectedArguments = [.. expectedArguments.Skip(1)];
                    actualArguments = [.. actualArguments.Skip(1)];
                }

                actualArguments.Should().HaveCount(expectedArguments.Count,
                    $"the call at {path} takes the arguments of the twin in order");
                for (var i = 0; i < expectedArguments.Count; i++)
                {
                    Same(expectedArguments[i], actualArguments[i], $"{path}.arg{i}");
                }

                break;

            case BinaryExpression expectedBinary:
                var actualBinary = actual.Should().BeAssignableTo<BinaryExpression>($"the twin compares at {path}").Subject;
                actualBinary.NodeType.Should().Be(expectedBinary.NodeType, $"the comparison at {path} is the one the twin makes");
                Same(expectedBinary.Left, actualBinary.Left, $"{path}.left");
                Same(expectedBinary.Right, actualBinary.Right, $"{path}.right");
                break;

            case MemberExpression expectedMember:
                var actualMember = actual.Should().BeAssignableTo<MemberExpression>($"the twin reads {expectedMember.Member.Name} at {path}").Subject;
                actualMember.Member.Name.Should().Be(expectedMember.Member.Name,
                    $"the column at {path} is the one the twin reads");
                actualMember.Member.DeclaringType.Should().Be(expectedMember.Member.DeclaringType,
                    $"the column at {path} is declared on the type the twin reads it on");
                Same(expectedMember.Expression!, actualMember.Expression!, $"{path}.owner");
                break;

            case ParameterExpression expectedParameter:
                actual.Should().BeAssignableTo<ParameterExpression>($"the twin reads the row itself at {path}").Subject
                    .Type.Should().Be(expectedParameter.Type, $"both trees read the row of {expectedParameter.Type.Name}");
                break;

            case ConstantExpression expectedConstant:
                var actualConstant = actual.Should().BeAssignableTo<ConstantExpression>($"the twin writes {expectedConstant.Value} at {path}").Subject;
                var actualType = Nullable.GetUnderlyingType(actualConstant.Type) ?? actualConstant.Type;
                var expectedType = Nullable.GetUnderlyingType(expectedConstant.Type) ?? expectedConstant.Type;
                actualType.Should().Be(expectedType, $"the value at {path} is of the type the twin typed it as");
                actualConstant.Value.Should().Be(expectedConstant.Value, $"the value at {path} is the one the twin wrote");
                break;

            default:
                actual.NodeType.Should().Be(expected.NodeType, $"the tree at {path} is the one the twin builds");
                break;
        }
    }

    private static Expression Unwrap(Expression expression) =>
        expression is UnaryExpression { NodeType: ExpressionType.Convert } convert
        && convert.Operand is ConstantExpression
            ? convert.Operand
            : expression;
}

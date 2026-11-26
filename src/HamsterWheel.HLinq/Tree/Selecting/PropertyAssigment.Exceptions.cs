using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Tokens;

namespace HamsterWheel.HLinq.Tree.Selecting;

partial class PropertyAssignment
{
    /// <summary>
    /// No CLR operator of the keyword between the types the two values answer.
    /// </summary>
    public sealed class MathOperandTypesCannotBeOperatedException(Type left, Type right, string keyword)
        : HLinqQueryException(
            $"The values answer '{left.Name}' and '{right.Name}' and there is no {keyword} between them.");

    /// <summary>
    /// A value of an expression answers an enumeration, which no engine operates on.
    /// </summary>
    public sealed class MathOperandIsAnEnumerationException(string name)
        : HLinqQueryException(
            $"""
             '{name}' answers an enumeration and a database operates on no enumeration;
             operate on values the engine can operate on.
             """);

    /// <summary>
    /// An expression of members has no name of its own, and the language invents none: the query
    /// writes one.
    /// </summary>
    public sealed class MathExpressionNeedsANameException() : HLinqQueryException(
        """
        A value built of several values has no name of its own, write one for it: select[FullName=x.Name+" "+x.Surname]
        """);

    public class PropertyAssignmentValueCannotBeResolvedException()
        : HLinqQueryException(
            $"""
             {nameof(PropertyAssignment)} provide value of '{nameof(NameOrValue)}' token if it was not part of the parsed tree.
             Make sure that your code is trying to translate query into expression correctly
             """);
}
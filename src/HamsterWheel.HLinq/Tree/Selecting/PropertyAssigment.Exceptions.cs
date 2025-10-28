using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Tokens;

namespace HamsterWheel.HLinq.Tree.Selecting;

partial class PropertyAssignment
{
    public class PropertyAssignmentValueCannotBeResolvedException()
        : HLinqQueryException(
            $"{nameof(PropertyAssignment)} provide value of '{nameof(NameOrValue)}' token if it was not part of the parsed tree. " +
            $"Make sure that your code is trying to translate query into expression correctly");
}
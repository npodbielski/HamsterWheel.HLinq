using HamsterWheel.HLinq.Tree;
using HamsterWheel.HLinq.Tree.Selecting;

namespace HamsterWheel.HLinq.Exceptions;

public class PropertyAssignmentWithoutPropertyCannotBeTranslatedException()
    : HLinqQueryException(
        $"{nameof(PropertyAssignment)} cannot be translated to Expression if it does not contain source '{nameof(Property)}' information.");
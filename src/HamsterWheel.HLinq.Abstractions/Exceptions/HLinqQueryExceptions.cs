namespace HamsterWheel.HLinq.Exceptions;

public abstract class HLinqQueryException(string message) : Exception(message);

public sealed class InvalidTypeOfTreeRoot<TExpected>(object actual) : HLinqQueryException(
    $"Tree root: '{actual}' is of type: '{actual.GetType()}' but type of {typeof(TExpected)} was expected.");
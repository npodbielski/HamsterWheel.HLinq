using HamsterWheel.HLinq.Parsers;
using HamsterWheel.HLinq.Tokens;

namespace HamsterWheel.HLinq.Exceptions;

public abstract class HLinqQueryException(string message) : Exception(message);

public sealed class InvalidTypeOfTreeRoot<TExpected>(object actual) : HLinqQueryException(
    $"Tree root: '{actual}' is of type: '{actual.GetType()}' but type of {typeof(TExpected)} was expected.");

public sealed class NonParsableTokenSequenceException(IToken[] actual, IElementParser[] expected)
    : HLinqQueryException($"HLinq query was invalid: '{string.Join(", ", actual.Select(a => a.GetType().Name))}'." +
                          $" Was expecting one of: [{string.Join(", ", expected.Select(a => a.ForElement().Name))}]");

public sealed class InvalidPropertyPathException(Type type, string path, string[] availableProps)
    : HLinqQueryException(
        $"Invalid property path '{path}' for entity '{type.Name}'." +
        $" Available properties at this point are: {(availableProps.Length != 0 ? string.Join(",", availableProps) : "none")} ");
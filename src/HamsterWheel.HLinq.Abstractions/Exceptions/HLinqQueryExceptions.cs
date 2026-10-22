using HamsterWheel.HLinq.Pipeline.Parser;
using HamsterWheel.HLinq.Tokens;

namespace HamsterWheel.HLinq.Exceptions;

public abstract class HLinqQueryException(string message) : Exception(message);

public sealed class InvalidTypeOfTreeRoot<TExpected>(object actual) : HLinqQueryException(
    $"Tree root: '{actual}' is of type: '{actual.GetType()}' but type of {typeof(TExpected)} was expected.");

public sealed class NonParsableTokenSequenceException(string queryString, IToken[] actual, IElementParser[] expected)
    : InvalidTokenCollectionException(queryString, actual, expected.First().ExampleTokens, MapParsers([
        .. expected.Skip(1)
    ]))
{
    private static IToken[][] MapParsers(IElementParser[] elementParsers) =>
        [.. elementParsers.Select(p => p.ExampleTokens)];
}

public sealed class InvalidPropertyPathException(Type type, string path, string[] availableProps)
    : HLinqQueryException(
        $"Invalid property path '{path}' for entity '{type.Name}'." +
        $" Available properties at this point are: '{(availableProps.Length != 0 ? string.Join("', '", availableProps) : "none")}'");

public sealed class RowsCannotBeOrderedOrFilteredException()
    : HLinqQueryException(
        "The rows of a group cannot be ordered or filtered by; only the key of the group orders and filters groups.");

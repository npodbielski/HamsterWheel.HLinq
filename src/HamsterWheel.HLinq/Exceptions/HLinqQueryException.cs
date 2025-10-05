using System.Linq.Expressions;
using HamsterWheel.HLinq.Appliers;
using HamsterWheel.HLinq.Parsers;
using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tree;
using HamsterWheel.HLinq.Tree.Select;

namespace HamsterWheel.HLinq.Exceptions;

public sealed class CouldNotDeserialize<T>(string json)
    : HLinqQueryException($"Could not deserialize string '{json.TakeAtMost(100)}' as '{typeof(T).FullName}'");

public sealed class UnknownTokenException(Range range, IToken[] expectedTokens)
    : HLinqQueryException(
        $"Unknown token exception at ({range.Start}, {range.End})." +
        $" Was expecting one of: {string.Join(", ", expectedTokens.Select(t => t.ToString()))}");

//TODO: write better exception messages
public sealed class InvalidTokenCollectionException(IToken[] actual, IToken[] expected, params IToken[][] orExpected)
    : HLinqQueryException(
        $"HLinq query was invalid: '{string.Join(", ", actual.Select(a => a.GetType().Name))}'. Was expecting one of: [{string.Join(", ", expected.Select(a => a.GetType().Name))}{(orExpected.Length != 0 ? $"], or [{string.Join("], or [", orExpected.SelectMany(a => a.Select(t => t.GetType().Name)))}" : "")}]");

public sealed class NonParsableTokenSequenceException(IToken[] actual, IElementParser[] expected)
    : HLinqQueryException($"HLinq query was invalid: '{string.Join(", ", actual.Select(a => a.GetType().Name))}'." +
                          $" Was expecting one of: [{string.Join(", ", expected.Select(a => a.ForElement().Name))}]");

public sealed class InvalidPropertyPathException(Type type, string path, string[] availableProps)
    : HLinqQueryException(
        $"Invalid property path '{path}' for entity '{type.Name}'." +
        $" Available properties at this point are: {(availableProps.Length != 0 ? string.Join(',', availableProps) : "none")} ");

public sealed class InvalidMethodException(Type type, string propName, string method, string[] available)
    : HLinqQueryException(
        $"Invalid method for property '{type.Name}.{propName}.{method}'. Available methods at this point are: [{string.Join(", ", available)}]");

public sealed class InvalidStaticMethodException(string method, string[] available)
    : HLinqQueryException(
        $"Invalid static method in query '{method}'. Available methods at this point are: [{string.Join(", ", available)}]");

public class ExpectedMemberOrMemberInitExpressionException() : HLinqQueryException(
    $"At this point type of expression should be {nameof(MemberExpression)} or {nameof(MemberInitExpression)}.");

public class ExpectedImplementationOfIEnumerableException()
    : HLinqQueryException($"Exptected object that implements: '{nameof(IEnumerable<int>)}' interface");

public class HLinqQueryParserReturnedNullException()
    : HLinqQueryException($"'{nameof(IHLinqParser)}' should never return null");

public class InitializerPropertyTypeNotInitializedException()
    : HLinqQueryException(
        $"When building expression for {nameof(InitializerConstantValue)} property type of final object must be known. Make sure that expression is built from inside {nameof(PropertyAssignment)}");

public class PropertyAssignmentNameNotFoundException()
    : HLinqQueryException("Could not find name or property with name of HLinq query");

public class PropertyAssignmentWithoutPropertyCannotBeTranslatedException()
    : HLinqQueryException(
        $"{nameof(PropertyAssignment)} cannot be translated to Expression if it does not contain source '{nameof(Property)}' information.");

public class PropertyAssignmentValueCannotBeResolvedException()
    : HLinqQueryException(
        $"{nameof(PropertyAssignment)} provide value of '{nameof(NameOrValue)}' token if it was not part of the parsed tree. " +
        $"Make sure that your code is trying to translate query into expression correctly");

public class DbFunctionsNotAvailableExceptions(string name) : HLinqQueryException(
    $"Db Function `{name}` are not available in current context. Either source is in memory collection or it was evaluated prior applying HLinq");

public class InvalidApplierResultException() : HLinqQueryException(
    $"'{nameof(IHLinqQueryApplier)}' returned '{nameof(IResult)}' with '{nameof(IResult.Data)}' and '{nameof(IResult.Count)}' null");
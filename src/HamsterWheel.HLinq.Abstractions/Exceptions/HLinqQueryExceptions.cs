using HamsterWheel.HLinq.Diagnostics;
using HamsterWheel.HLinq.Pipeline.Parser;
using HamsterWheel.HLinq.Tokens;

namespace HamsterWheel.HLinq.Exceptions;

/// <summary>
/// Base class for every hlinq exception, saying its sentence in <see cref="Exception.Message"/>.
/// </summary>
public abstract class HLinqQueryException(string message) : Exception(message)
{
    /// <summary>
    /// Hlinq error code.
    /// </summary>
    public virtual HLinqErrorCode Code => HLinqErrorCode.Refused;

    /// <summary>
    /// InvalidQueryChunk
    /// </summary>
    public virtual string InvalidQueryChunk => Message;

    /// <summary>
    /// Examples
    /// </summary>
    public virtual IReadOnlyList<string> Examples => [];

    public virtual bool ShouldReportToClient => true;

    /// <summary>
    /// List of errors.
    /// </summary>
    public IReadOnlyList<HLinqError> Errors { get; internal set; } = [];

    /// <summary>
    /// This exception as data, stamped with the root it stands in.
    /// </summary>
    public HLinqError ToError(string? rootKeyword) => HLinqError.Create(Code, rootKeyword, InvalidQueryChunk, Message, Examples);
}

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
        $"Invalid property path '{path}' for entity '{type.Name}'. Available properties at this point are: '{(availableProps.Length != 0 ? string.Join("', '", availableProps) : "none")}'")
{
    public override HLinqErrorCode Code => HLinqErrorCode.UnknownMember;
    public override string InvalidQueryChunk => path;
    public override IReadOnlyList<string> Examples { get; } = availableProps;
}

public sealed class InvalidPagingValueException(string operation, int value) : HLinqQueryException(
    $"'{operation}[{value}]' needs a non-negative number of records.")
{
    public override HLinqErrorCode Code => HLinqErrorCode.InvalidPagingValue;
    public override string InvalidQueryChunk => $"{operation}[{value}]";
}

public sealed class QueryTooLongException(int characters, int maxCharacters) : HLinqQueryException(
    $"The query is {characters} characters long and this host lets one query be {maxCharacters}.")
{
    public override HLinqErrorCode Code => HLinqErrorCode.QueryTooLong;
    public override string InvalidQueryChunk => characters.ToString();
    public override IReadOnlyList<string> Examples { get; } = [maxCharacters.ToString()];
}

public sealed class TooManyPropertiesException(int members, int maxMembers) : HLinqQueryException(
    $"The query writes {members} properties and this host lets one query ask for {maxMembers}.")
{
    public override HLinqErrorCode Code => HLinqErrorCode.TooManyProperties;
    public override string InvalidQueryChunk => members.ToString();
    public override IReadOnlyList<string> Examples { get; } = [maxMembers.ToString()];
}

public sealed class RowsCannotBeOrderedOrFilteredException()
    : HLinqQueryException(
        "The rows of a group cannot be ordered or filtered by; only the key of the group orders and filters groups.")
{
    public override HLinqErrorCode Code => HLinqErrorCode.RootNotAllowed;
}

public sealed class GroupRowsNameReservedException(string name) : HLinqQueryException(
    $"'{name}' is the name the group answers its own rows under; select them with Rows=x.Rows.select(...), or name their members directly beside the aggregates: select[Count=x.Count(),x.Name].")
{
    public override HLinqErrorCode Code => HLinqErrorCode.RootNotAllowed;
    public override string InvalidQueryChunk => name;
}

public sealed class GroupRowsSelectSourceException(string name) : HLinqQueryException(
    $"'{name}=...' selects something else than the group's own rows; write Rows=x.Rows.select(...) to select the rows the group answers.")
{
    public override HLinqErrorCode Code => HLinqErrorCode.RootNotAllowed;
    public override string InvalidQueryChunk => name;
}

public sealed class GroupRowsSelectedTwiceException() : HLinqQueryException(
    "'Rows=x.Rows.select(...)' selects the group's rows, and so does a member written beside it; the same rows are selected twice, choose one spelling.")
{
    public override HLinqErrorCode Code => HLinqErrorCode.RootNotAllowed;
}

/// <summary>
/// Two members of one member list answer under one name, refused while the query is parsed.
/// </summary>
public sealed class MemberNameCollisionException(string name, string first, string second)
    : HLinqQueryException(
        $"'{name}' answers two values - '{first}' and '{second}'; one name answers one value, give the second a name of its own.")
{
    public override HLinqErrorCode Code => HLinqErrorCode.DuplicateMemberName;
    public override string InvalidQueryChunk => name;
    public override IReadOnlyList<string> Examples { get; } = [first, second];
}

public sealed class GroupAggregateNameCollisionException(string name) : HLinqQueryException(
    $"The answer already names '{name}' - a group answers Key and Rows beside its aggregates, so an aggregate cannot take that name; give it another, i.e. Count=x.Count().")
{
    public override HLinqErrorCode Code => HLinqErrorCode.DuplicateMemberName;
    public override string InvalidQueryChunk => name;
}

public sealed class EntityAggregateNeedsGroupException(string name) : HLinqQueryException(
    $"'{name}()' stands on the entity of the clause here, and there is no group to aggregate over; write it after a groupBy, or on a collection the query reached through a property, i.e. x.Addresses.Count().")
{
    public override HLinqErrorCode Code => HLinqErrorCode.AggregateNeedsGroup;
    public override string InvalidQueryChunk => name;
}

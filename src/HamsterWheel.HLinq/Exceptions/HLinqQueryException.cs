namespace HamsterWheel.HLinq.Exceptions;

public sealed class CouldNotDeserializeException<T>(string json)
    : HLinqQueryException($"Could not deserialize string '{json.TakeAtMost(100)}' as '{typeof(T).FullName}'");


public class DbFunctionsNotAvailableExceptions(string name) : HLinqQueryException(
    $"Db Function `{name}` are not available in current context. Either source is in memory collection or it was evaluated prior applying HLinq");
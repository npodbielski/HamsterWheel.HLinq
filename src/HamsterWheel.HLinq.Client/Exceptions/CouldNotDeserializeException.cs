using HamsterWheel.HLinq.Exceptions;

namespace HamsterWheel.HLinq.Client.Exceptions;

public sealed class CouldNotDeserializeException<T>(string json)
    : HLinqQueryException($"Could not deserialize string '{json.TakeAtMost(100)}' as '{typeof(T).FullName}'");
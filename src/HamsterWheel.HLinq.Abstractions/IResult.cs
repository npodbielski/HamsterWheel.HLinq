namespace HamsterWheel.HLinq;

public interface IResult
{
    /// <summary>
    /// The rows the query answered, null when the query answered a count instead.
    /// </summary>
    object[]? Data { get; }
    Type ItemType { get; }
    int? Count { get; }
}
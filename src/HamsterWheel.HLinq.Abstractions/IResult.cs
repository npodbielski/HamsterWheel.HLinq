namespace HamsterWheel.HLinq;

public interface IResult
{
    object[]? Data { get; }
    Type ItemType { get; }
    int? Count { get; }
}
namespace HamsterWheel.HLinq.Request;

public record Result(object[]? Data, Type ItemType, int? Count = null) : IResult;
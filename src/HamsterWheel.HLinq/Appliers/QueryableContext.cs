namespace HamsterWheel.HLinq.Appliers;

public record QueryableContext(IQueryable? Queryable, Type CurrentResultType, int? Count = null) : IQueryableContext;
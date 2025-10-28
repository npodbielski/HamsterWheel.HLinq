namespace HamsterWheel.HLinq.Pipeline.Applier;

public record QueryableContext(IQueryable? Queryable, Type CurrentResultType, int? Count = null) : IQueryableContext;
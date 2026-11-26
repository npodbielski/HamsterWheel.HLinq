namespace HamsterWheel.HLinq.Pipeline.Applier;

public static class QueryableContextExtensions
{
    public static IQueryableContext NextByCalling(this IQueryableContext context, Type currentResultType,
        Func<IQueryable?, IQueryable?> call, int? count = null, GroupingState? grouping = null) =>
        context.Next(
            context.ValidateMode ? null : call(context.Queryable),
            currentResultType, count, grouping);
}

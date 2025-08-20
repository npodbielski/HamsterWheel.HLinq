namespace HamsterWheel.HLinq.Appliers;

public interface IHLinqQueryApplier
{
    object Apply<T1>(IQueryable<T1> queryable, IHLinqQuery hLinqQuery, CancellationToken token = default)
        where T1 : class;

    object Apply(IQueryable queryable, Type itemType, IHLinqQuery hLinqQuery, CancellationToken token = default);

    IResult ApplyGetType<T1>(IQueryable<T1> queryable, IHLinqQuery hLinqQuery, CancellationToken token = default)
        where T1 : class;

    IResult ApplyGetType(IQueryable queryable, IHLinqQuery hLinqQuery, Type itemType, CancellationToken token = default);
}
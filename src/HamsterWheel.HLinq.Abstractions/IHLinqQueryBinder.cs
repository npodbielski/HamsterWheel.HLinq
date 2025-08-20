namespace HamsterWheel.HLinq;

public interface IHLinqQueryBinder
{
    IHLinqQuery BindQuery(string queryString, Type model);
}
namespace HamsterWheel.HLinq.Request;

/// <summary>
/// The parameters a request carries: the endpoint's are left to the endpoint, the ones HLinq reads
/// are read here, before the query is interpreted.
/// </summary>
public interface IHLinqQueryParameters
{
    HLinqRequest GetRequest(string queryString);
}

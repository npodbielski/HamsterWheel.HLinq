using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using HamsterWheel.HLinq.Exceptions;

namespace HamsterWheel.HLinq.Client;

public class ResponseHLinqClientQueryBuilder<T>
{
    protected StringBuilder Query = new();

    public string BuildQuery() => UrlEncoder.Default.Encode(Query.ToString());

    public T Deserialize(string json) => JsonSerializer.Deserialize<T>(json, JsonSerializerOptions.Web) ??
                                         throw new CouldNotDeserialize<T>(json);

    protected void AddDotIfNecessary()
    {
        if (Query.Length > 0)
        {
            Query.Append('.');
        }
    }

    protected static OrderedHLinqClientQueryBuilder<TNext>
        NextOrdered<TNext>(ResponseHLinqClientQueryBuilder<T> previous) =>
        new()
        {
            Query = previous.Query
        };

    protected static UnorderedHLinqClientQueryBuilder<TNext> Next<TNext>(ResponseHLinqClientQueryBuilder<T> previous) =>
        new()
        {
            Query = previous.Query
        };

    protected static ResponseHLinqClientQueryBuilder<int> NextCounted(ResponseHLinqClientQueryBuilder<T> previous) =>
        new()
        {
            Query = previous.Query
        };
}
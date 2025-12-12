using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using HamsterWheel.HLinq.Client.Exceptions;

namespace HamsterWheel.HLinq.Client;

public class ResponseHLinqClientQueryBuilder
{
    public static JsonSerializerOptions DefaultJsonSerializerOptions { get; } = new(JsonSerializerDefaults.Web);

    static ResponseHLinqClientQueryBuilder() =>
        DefaultJsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
}

public class ResponseHLinqClientQueryBuilder<T> : ResponseHLinqClientQueryBuilder
{
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    protected StringBuilder Query = new();

    protected ResponseHLinqClientQueryBuilder(JsonSerializerOptions? options = null) =>
        _jsonSerializerOptions = options ?? DefaultJsonSerializerOptions;

    public string BuildAndEncode() => UrlEncoder.Default.Encode(Build());

    public string Build() => Query.ToString();

    public T Deserialize(string json) => JsonSerializer.Deserialize<T>(json, _jsonSerializerOptions) ??
                                         throw new CouldNotDeserializeException<T>(json);

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
        new(previous._jsonSerializerOptions)
        {
            Query = previous.Query
        };

    protected static ResponseHLinqClientQueryBuilder<int> NextCounted(ResponseHLinqClientQueryBuilder<T> previous) =>
        new(previous._jsonSerializerOptions)
        {
            Query = previous.Query
        };
}
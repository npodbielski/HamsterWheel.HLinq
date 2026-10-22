using Microsoft.Extensions.Logging;

namespace HamsterWheel.HLinq.Client;

public static class HttpClientExtensions
{
    /// <summary>
    /// Performs a HLinq query for a specific resource of a compatible API, and deserializes its answer.
    /// </summary>
    /// <param name="logger">
    /// Where the warnings of the response are logged: which member a name of the query got when more
    /// than one member answered it and no rule could decide between them. A warning is a header of a
    /// response the caller of a library never sees, so it is said here as a warning - and to the error
    /// stream when there is no logger. It is said before the status code is looked at, since what a
    /// server said about a name may be why the query failed.
    /// </param>
    public static async Task<TResult> GetWithHLinq<TResult>(this HttpClient client, string path,
        Func<HLinqClientQueryBuilderFactory, ResponseHLinqClientQueryBuilder<TResult>> builder,
        ILogger? logger = null)
    {
        var hLinqClient = builder(new HLinqClientQueryBuilderFactory());
        var response = await client.GetAsync($"{path}?{hLinqClient.BuildAndEncode()}");
        response.ReportWarnings(logger);
        return response.IsSuccessStatusCode
            ? hLinqClient.Deserialize(await response.Content.ReadAsStringAsync())
            : throw new HttpRequestException(await response.Content.ReadAsStringAsync());
    }
}
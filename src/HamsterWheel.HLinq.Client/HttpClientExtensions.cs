using System.Text.Json;

namespace HamsterWheel.HLinq.Client;

public static class HttpClientExtensions
{
    public static async Task<TResult> GetWithHLinq<TResult>(this HttpClient client, string path,
        Func<HLinqClientQueryBuilderFactory, ResponseHLinqClientQueryBuilder<TResult>> builder)
    {
        var hLinqClient = builder(new HLinqClientQueryBuilderFactory());
        var response = await client.GetAsync($"{path}?{hLinqClient.BuildAndEncode()}");
        return response.IsSuccessStatusCode
            ? hLinqClient.Deserialize(await response.Content.ReadAsStringAsync())
            : throw new HttpRequestException(await response.Content.ReadAsStringAsync());
    }
}
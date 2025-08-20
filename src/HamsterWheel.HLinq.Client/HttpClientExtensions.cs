namespace HamsterWheel.HLinq.Client;

public static class HttpClientExtensions
{
    public static async Task<TResult> GetWithHLinq<TResult>(this HttpClient client, string path,
        Func<HLinqClientQueryBuilderFactory, ResponseHLinqClientQueryBuilder<TResult>> builder)
    {
        var queryBuilder = new HLinqClientQueryBuilderFactory();
        var hLinqClient = builder(queryBuilder);
        var response = await client.GetAsync($"{path}?{hLinqClient.BuildQuery()}");
        if (response.IsSuccessStatusCode)
        {
            return hLinqClient.Deserialize(await response.Content.ReadAsStringAsync());
        }

        throw new HttpRequestException(await response.Content.ReadAsStringAsync());
    }
}
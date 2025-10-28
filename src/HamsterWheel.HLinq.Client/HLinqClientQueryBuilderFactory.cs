using System.Text.Json;

#pragma warning disable CA1822
namespace HamsterWheel.HLinq.Client;

public class HLinqClientQueryBuilderFactory
{
    // ReSharper disable once MemberCanBeMadeStatic.Global - it cannot be static because it will break nice Fluent API
    /// <summary>
    /// Creates <see cref="UnorderedHLinqClientQueryBuilder{T}"/> for model of provided type.
    /// </summary>
    /// <typeparam name="T">Type of model to query server for</typeparam>
    /// <param name="jsonSerializerOptions">Optional JSON serialization options if API responses require custom rules.</param>
    /// <returns>API JSON response from the server</returns>
    /// <example>
    /// <code>await fixture.Client.GetWithHLinq("/demo/memory", q => q.For&lt;Superhero&gt;().Count()); </code>
    /// </example>
    public UnorderedHLinqClientQueryBuilder<T> For<T>(JsonSerializerOptions? jsonSerializerOptions = null) => new(jsonSerializerOptions);
}
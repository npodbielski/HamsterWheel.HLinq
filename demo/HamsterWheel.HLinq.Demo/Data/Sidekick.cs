using System.Text.Json.Serialization;

namespace HamsterWheel.HLinq.Demo.Data;

/// <summary>
/// The demo's example of a query whose name has two answers: <c>where[x.name==Bucky]</c> is
/// answered by <see cref="NAME" />, which is not the member the caller meant either, and the response
/// says so. A query that spells one of them exactly gets the same answer and hears nothing about it.
/// </summary>
public class Sidekick
{
    public string Name { get; set; } = "";

    /// <summary>
    ///     The same name spelled the other way, out of the answer it goes: a response carrying both would
    ///     carry one name twice, and the serializer refuses to say which of the two owns it.
    /// </summary>
    [JsonIgnore]
    public string NAME { get; set; } = "";

    public static Sidekick[] Sidekicks =
    [
        new() { Name = "Bucky", NAME = "Bucky" },
        new() { Name = "Sam", NAME = "Sam" },
        new() { Name = "Rhodey", NAME = "Rhodey" },
    ];
}

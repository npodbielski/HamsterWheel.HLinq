namespace HamsterWheel.HLinq.Request;

/// <summary>
///     HLinq query provided by the client.
/// </summary>
/// <param name="Query">The query string with the parameters of the endpoint taken out.</param>
/// <param name="Culture">
///     The culture as the request named it (<c>pl</c>, <c>pl-PL</c>), null when it did not ask for one.
/// </param>
public sealed record HLinqRequest(string Query, string? Culture);

using HamsterWheel.HLinq.Diagnostics;
using Microsoft.Extensions.Primitives;

namespace HamsterWheel.HLinq.AspNet;

/// <summary>
/// The warnings of a request, written to its response as they arrive and logged either way. They are
/// known while the query is applied to data, which is after an endpoint read the query and before it
/// wrote a byte of the answer - so a header still fits.
/// </summary>
public class HttpResponseWarnings(
    IHttpContextAccessor httpContextAccessor,
    IHLinqOptions options,
    ILogger<HttpResponseWarnings> logger) : HLinqWarnings
{
    /// <summary>
    /// How many of them a response names. Kestrel caps the whole header block of a response, and a
    /// query ambiguous about forty members must not turn into a failure at the first byte written;
    /// the log below carries every one of them.
    /// </summary>
    public const int HeaderLimit = 5;

    private bool _moreThanTheLimitWasAnnounced;

    public override void Report(HLinqMemberWarning warning)
    {
        base.Report(warning);

        var value = HeaderValue(warning);

        logger.LogWarning("HLinq resolved a member of {Type} that more than one answered: {Warning}",
            warning.Type, value);

        var response = httpContextAccessor.HttpContext?.Response;
        if (response is null
            || !options.EmitWarnings
            || response.HasStarted)
        {
            return;
        }

        if (Reported.Count <= HeaderLimit)
        {
            response.Headers.Append(HLinqMemberWarning.Header, value);
            return;
        }

        if (!_moreThanTheLimitWasAnnounced)
        {
            _moreThanTheLimitWasAnnounced = true;
            response.Headers.Append(HLinqMemberWarning.Header, new StringValues("more=\"true\""));
        }
    }

    /// <summary>
    /// One warning as the value of a header line: a comma-separated set of quoted parameters, the way the
    /// <c>Warning</c> and <c>Deprecation</c> headers of the same genre read. A value that is not ASCII
    /// goes out in the extended notation of RFC 8187 in place of the plain one, because a header value
    /// is ASCII and raw UTF-8 in one arrives mangled at a proxy.
    /// </summary>
    private static string HeaderValue(HLinqMemberWarning warning) =>
        string.Join(", ",
            Parameter("reason", Token(warning.Reason)),
            Parameter("member", warning.Member),
            Parameter("type", warning.Type),
            Parameter("competitors", string.Join(", ", warning.Competitors)),
            Parameter("query", warning.Query));

    /// <summary>
    /// The closed vocabulary a caller branches on; <c>conversion</c> and <c>declared-on-type</c> are held
    /// back for the rules that report nothing today.
    /// </summary>
    private static string Token(HLinqMemberWarningReason reason) =>
        reason == HLinqMemberWarningReason.AmbiguousName ? "ambiguous-name" : "ambiguous-call";

    private static string Parameter(string name, string value) =>
        IsAscii(value)
            ? $"{name}=\"{value.Replace("\"", "\\\"")}\""
            : $"{name}*=UTF-8''{Uri.EscapeDataString(value)}";

    private static bool IsAscii(string value)
    {
        foreach (var character in value)
        {
            if (character > 127)
            {
                return false;
            }
        }

        return true;
    }
}

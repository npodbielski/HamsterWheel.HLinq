using HamsterWheel.HLinq.Diagnostics;
using Microsoft.Extensions.Logging;

namespace HamsterWheel.HLinq.Client;

/// <summary>
/// HLinq response warnings: what a response said about the query it answered. They arrive on a header
/// of a response the caller never sees - so they are repeated to the log the caller gave, or to the
/// error stream of a program that has no log.
/// </summary>
internal static class ResponseWarnings
{
    private const string Prefix = "HLinq warning: ";

    private const string Template = Prefix + "{Warning}";

    internal static void ReportWarnings(this HttpResponseMessage response, ILogger? logger = null)
    {
        if (!response.Headers.TryGetValues(HLinqMemberWarning.Header, out var warnings))
        {
            return;
        }

        foreach (var warning in warnings)
        {
            if (logger != null)
            {
                logger.LogWarning(Template, warning);
            }
            else
            {
                Console.Error.WriteLine(Prefix + warning);
            }
        }
    }
}

using System.Net;
using System.Reflection;
using HamsterWheel.HLinq.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace HamsterWheel.HLinq.AspNet.ExceptionHandlers;

public sealed class HLinqQueryExceptionHandler(ILogger<HLinqQueryExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception,
        CancellationToken cancellationToken)
    {
        var hLinqException = CanHandle(exception);
        if (hLinqException is null)
        {
            return false;
        }

        LogHLinqOriginalMessage(hLinqException);
        var problemDetails = GetDetails(hLinqException);
        httpContext.Response.StatusCode = problemDetails.Status ?? (int)HttpStatusCode.InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }

    private static HLinqQueryException? CanHandle(Exception exception) =>
        exception switch
        {
            HLinqQueryException targetException => targetException,
            //if method is called via reflection exception is wrapped 
            TargetInvocationException targetInvocationException =>
                targetInvocationException.InnerException as HLinqQueryException,
            _ => null
        };

    private static ProblemDetails GetDetails(HLinqQueryException exception) =>
        new()
        {
            Status = (int)HttpStatusCode.BadRequest,
            Title = exception.Message
        };

    private static readonly Action<ILogger, string, Exception> LogHLinqOriginalMessageAction =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(13, nameof(TryHandleAsync)), "{Message}");

    private void LogHLinqOriginalMessage(HLinqQueryException exception) =>
        LogHLinqOriginalMessageAction(logger, exception.Message, exception);
}
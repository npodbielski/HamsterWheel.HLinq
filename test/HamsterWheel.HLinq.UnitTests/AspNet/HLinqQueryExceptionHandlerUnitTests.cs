using System.Text.Json;
using FluentAssertions;
using HamsterWheel.HLinq.AspNet.ExceptionHandlers;
using HamsterWheel.HLinq.Diagnostics;
using HamsterWheel.HLinq.Exceptions;
using Microsoft.AspNetCore.Http;
using HamsterWheel.HLinq.UnitTests.TestUtils.Dummies;

namespace HamsterWheel.HLinq.UnitTests.AspNet;

/// <summary>
/// What a caller over HTTP hears about a refused query: the sentence and the code.
/// </summary>
public class HLinqQueryExceptionHandlerUnitTests
{
    private sealed class PlainException(string message) : HLinqQueryException(message);

    private sealed class NamedException(string message, string path) : HLinqQueryException(message)
    {
        public override HLinqErrorCode Code => HLinqErrorCode.UnknownMember;
        public override string InvalidQueryChunk => path;
        public override IReadOnlyList<string> Examples { get; } = ["Name"];
    }

    [Fact]
    public async Task WhenAQueryIsRefused_ThenTheProblemCarriesEveryAnswerTheExceptionCameWith()
    {
        //arrange
        //that was found and the response hands all of it over
        var exception = new NamedException("no", "Nope");
        exception.Errors =
        [
            HLinqError.Create(HLinqErrorCode.UnknownMember, "where", "Nope", "no", ["Name"]),
            HLinqError.Create(HLinqErrorCode.UnknownMember, "select", "Texto", "no", ["Text"])
        ];

        //act
        var problem = await Handle(exception);

        //assert
        problem.GetProperty("status").GetInt32().Should().Be(400);
        problem.GetProperty("title").GetString().Should().Be("no");
        var errors = problem.GetProperty(HLinqQueryExceptionHandler.ErrorsExtension);
        errors.GetArrayLength().Should().Be(2);
        var error = errors[0];
        error.GetProperty("errorCode").GetString().Should().Be("unknown-member");
        error.GetProperty("rootKeyword").GetString().Should().Be("where");
        error.GetProperty("text").GetString().Should().Be("Nope");
        error.GetProperty("message").GetString().Should().Be("no");
        error.GetProperty("examples")[0].GetString().Should().Be("Name");
        errors[1].GetProperty("rootKeyword").GetString().Should().Be("select");
        errors[1].GetProperty("text").GetString().Should().Be("Texto");
    }

    [Fact]
    public async Task WhenAnExceptionCameFromTheTextItself_ThenItIsTheWholeAnswerAndStillSaysItself()
    {
        //act
        //exception arrives with no answers beside it
        var problem = await Handle(new PlainException("no"));

        //assert
        problem.GetProperty("title").GetString().Should().Be("no");
        var error = problem.GetProperty(HLinqQueryExceptionHandler.ErrorsExtension).EnumerateArray().Single();
        error.GetProperty("errorCode").GetString().Should().Be("refused");
        error.GetProperty("text").GetString().Should().Be("no");
    }

    [Fact]
    public async Task WhenAnExceptionIsNotHLinqs_ThenItIsLeftToThePipeline()
    {
        //arrange
        var handler = new HLinqQueryExceptionHandler(new RecordedLog<HLinqQueryExceptionHandler>());

        //act + assert
        (await handler.TryHandleAsync(new DefaultHttpContext(), new InvalidOperationException("no"),
            CancellationToken.None)).Should().BeFalse();
    }

    private static async Task<JsonElement> Handle(HLinqQueryException exception)
    {
        var context = new DefaultHttpContext();
        var body = new MemoryStream();
        context.Response.Body = body;

        var handler = new HLinqQueryExceptionHandler(new RecordedLog<HLinqQueryExceptionHandler>());
        (await handler.TryHandleAsync(context, exception, CancellationToken.None)).Should().BeTrue();

        body.Position = 0;
        using var document = await JsonDocument.ParseAsync(body);
        return document.RootElement.Clone();
    }
}

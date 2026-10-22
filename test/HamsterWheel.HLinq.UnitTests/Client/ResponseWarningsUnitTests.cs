using System.Net;
using System.Text;
using FluentAssertions;
using HamsterWheel.HLinq.Client;
using HamsterWheel.HLinq.UnitTests.TestUtils.Dummies;
using Microsoft.Extensions.Logging;

namespace HamsterWheel.HLinq.UnitTests.Client;

/// <summary>
/// What a client does with what a response said about the query it answered: repeats it, one line per
/// warning, to the log it was handed - and to the error stream of the program that handed it none.
/// </summary>
public class ResponseWarningsUnitTests
{
    private readonly RecordedLog<HttpClient> _log = new();

    [Fact]
    public async Task WhenTheResponseSaidNothing_ThenTheClientSaysNothing()
    {
        //arrange
        var client = Client(HttpStatusCode.OK, "[]");

        //act
        await client.GetWithHLinq("/demo", q => q.For<Dummy>(), _log);

        //assert
        _log.Lines.Should().BeEmpty();
    }

    [Fact]
    public async Task WhenTheResponseWarned_ThenEveryWarningReachesTheLogAsAWarningInItsOrder()
    {
        //arrange
        var client = Client(HttpStatusCode.OK, "[]", "reason=\"ambiguous-name\", member=\"NAME\"",
            "reason=\"ambiguous-call\", member=\"Sub(Char)\"");

        //act
        await client.GetWithHLinq("/demo", q => q.For<Dummy>(), _log);

        //assert
        _log.Lines.Should().BeEquivalentTo(
            [
                new RecordedLine(LogLevel.Warning, "HLinq warning: reason=\"ambiguous-name\", member=\"NAME\""),
                new RecordedLine(LogLevel.Warning, "HLinq warning: reason=\"ambiguous-call\", member=\"Sub(Char)\"")
            ]);
    }

    [Fact]
    public async Task WhenTheProgramGaveTheClientNoLog_ThenTheWarningsGoToItsErrorStream()
    {
        //arrange
        var client = Client(HttpStatusCode.OK, "[]", "reason=\"ambiguous-name\", member=\"NAME\"");
        var error = new StringWriter();
        var before = Console.Error;
        Console.SetError(error);

        try
        {
            //act
            await client.GetWithHLinq("/demo", q => q.For<Dummy>());
        }
        finally
        {
            Console.SetError(before);
        }

        //assert
        error.ToString().Trim().Should().Be("HLinq warning: reason=\"ambiguous-name\", member=\"NAME\"");
    }

    [Fact]
    public async Task WhenTheQueryFailedAndTheResponseWarned_ThenTheWarningIsSaidBeforeTheException()
    {
        //arrange
        var client = Client(HttpStatusCode.BadRequest, "take is not a clause",
            "reason=\"ambiguous-name\", member=\"NAME\"");

        //act
        var action = () => client.GetWithHLinq("/demo", q => q.For<Dummy>(), _log);

        //assert
        await action.Should().ThrowAsync<HttpRequestException>().WithMessage("take is not a clause");
        _log.Lines.Should().BeEquivalentTo(
            [
                new RecordedLine(LogLevel.Warning, "HLinq warning: reason=\"ambiguous-name\", member=\"NAME\"")
            ]);
    }

    private static HttpClient Client(HttpStatusCode status, string body, params string[] warnings) =>
        new(new Answers(status, body, warnings))
        {
            BaseAddress = new Uri("http://localhost")
        };

    private class Dummy
    {
        public string Name { get; set; } = "";
    }

    private class Answers(HttpStatusCode status, string body, params string[] warnings) : HttpClientHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };

            foreach (var warning in warnings)
            {
                response.Headers.Add("X-HLinq-Warning", warning);
            }

            return Task.FromResult(response);
        }
    }
}

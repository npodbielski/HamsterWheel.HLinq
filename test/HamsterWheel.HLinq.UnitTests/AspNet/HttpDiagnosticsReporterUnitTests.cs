using FluentAssertions;
using HamsterWheel.HLinq.AspNet;
using HamsterWheel.HLinq.Diagnostics;
using HamsterWheel.HLinq.UnitTests.TestUtils.Dummies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace HamsterWheel.HLinq.UnitTests.AspNet;

/// <summary>
/// What a request hears about members its query could not decide between.
/// </summary>
public class HttpDiagnosticsReporterUnitTests
{

    private readonly RecordedLog<HttpDiagnosticsReporter> _log = new();

    [Fact]
    public void WhenAWarningIsReported_ThenTheResponseCarriesItOnAHeaderLineOfItsOwn()
    {
        //arrange
        var context = new DefaultHttpContext();

        //act
        Sink(context).Report(Warning(1));

        //assert
        context.Response.Headers["X-HLinq-Warning"].ToString().Should().Be(
            "reason=\"ambiguous-name\", member=\"Name\", type=\"Demo.Ranked\", competitors=\"Name, NAME\", " +
            "query=\"where[x.nAmE==1]\"");
        _log.Lines.Should().BeEquivalentTo(
            [
                new RecordedLine(LogLevel.Warning, "HLinq resolved a member of Demo.Ranked that more than one " +
                                                  "answered: reason=\"ambiguous-name\", member=\"Name\", " +
                                                  "type=\"Demo.Ranked\", competitors=\"Name, NAME\", " +
                                                  "query=\"where[x.nAmE==1]\"")
            ]);
    }

    [Fact]
    public void WhenAWarningIsReported_ThenEveryValueOfItIsQuoted()
    {
        //arrange
        var context = new DefaultHttpContext();
        var warning = new HLinqMemberWarning(HLinqMemberWarningReason.AmbiguousCall, "Sub(String,Int32)",
            "Demo.Ranked", ["Sub(String,Int32)", "Sub(String,Int64)"], "where[x.Service.Sub(abc,5)]");

        //act
        Sink(context).Report(warning);

        //assert
        context.Response.Headers["X-HLinq-Warning"].ToString().Should().Be(
            "reason=\"ambiguous-call\", member=\"Sub(String,Int32)\", type=\"Demo.Ranked\", " +
            "competitors=\"Sub(String,Int32), Sub(String,Int64)\", query=\"where[x.Service.Sub(abc,5)]\"");
    }

    [Fact]
    public void WhenAValueOfAWarningIsNotAscii_ThenThatValueGoesOutEncoded()
    {
        //arrange
        var context = new DefaultHttpContext();
        var warning = new HLinqMemberWarning(HLinqMemberWarningReason.AmbiguousName, "Imię", "Demo.Ranked",
            ["Imię", "imię"], "where[x.imię==abc]");

        //act
        Sink(context).Report(warning);

        //assert
        context.Response.Headers["X-HLinq-Warning"].ToString().Should().Be(
            "reason=\"ambiguous-name\", member*=UTF-8''Imi%C4%99, type=\"Demo.Ranked\", " +
            "competitors*=UTF-8''Imi%C4%99%2C%20imi%C4%99, query*=UTF-8''where%5Bx.imi%C4%99%3D%3Dabc%5D");
    }

    [Fact]
    public void WhenTheApplicationAskedForNoHeaders_ThenTheWarningsAreReadFromTheParseAndTheResponseSaysNothing()
    {
        //arrange
        var context = new DefaultHttpContext();
        var sink = Sink(context, asHeaders: false);

        //act
        sink.Report(Warning(1));

        //assert
        context.Response.Headers.ContainsKey("X-HLinq-Warning").Should().BeFalse();
        sink.Warnings.Should().HaveCount(1);
        _log.Lines.Should().HaveCount(1);
    }

    [Fact]
    public void WhenMoreWarningsThanTheHeaderLimitArrive_ThenTheResponseSaysOnceThatThereWereMore()
    {
        //arrange
        var context = new DefaultHttpContext();
        var sink = Sink(context);

        //act
        for (var i = 0; i < HttpDiagnosticsReporter.HeaderLimit + 2; i++)
        {
            sink.Report(Warning(i));
        }

        //assert
        var header = context.Response.Headers["X-HLinq-Warning"].ToArray();
        header.Should().HaveCount(HttpDiagnosticsReporter.HeaderLimit + 1);
        header.Last().Should().Be("more=\"true\"");
        sink.Warnings.Should().HaveCount(HttpDiagnosticsReporter.HeaderLimit + 2);
        _log.Lines.Should().HaveCount(HttpDiagnosticsReporter.HeaderLimit + 2);
    }

    private static HLinqMemberWarning Warning(int of) =>
        new(HLinqMemberWarningReason.AmbiguousName, "Name", "Demo.Ranked", ["Name", "NAME"],
            $"where[x.nAmE=={of}]");

    private HttpDiagnosticsReporter Sink(DefaultHttpContext context, bool asHeaders = true) =>
        new(new HttpContextAccessor
        {
            HttpContext = context
        }, new HLinqOptions
        {
            EmitWarnings = asHeaders
        }, _log);
}

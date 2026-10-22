using Microsoft.Extensions.Logging;

namespace HamsterWheel.HLinq.UnitTests.TestUtils.Dummies;

/// <summary>
/// What a library said to the operator, caught on its way to a log instead of printed.
/// </summary>
/// <typeparam name="T">
/// The type whose log the code under test was handed, which is all that distinguishes one
/// <see cref="ILogger"/> from another of the same name.
/// </typeparam>
public class RecordedLog<T> : ILogger<T>
{
    public List<RecordedLine> Lines { get; } = [];

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter) =>
        Lines.Add(new RecordedLine(logLevel, formatter(state, exception)));

    public bool IsEnabled(LogLevel logLevel) => true;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
}

/// <summary>
/// A line at the level it was said, since whether a warning was said as a warning is part of what a
/// caller can rely on.
/// </summary>
public record RecordedLine(LogLevel Level, string Message);

namespace HamsterWheel.HLinq.Diagnostics;

/// <summary>
/// Hlinq query error information.
/// </summary>
/// <param name="ErrorCode">
/// What a caller branches on.
/// </param>
/// <param name="RootKeyword">
/// The root the error stands in, not translated. Null when the error is of the whole query.
/// </param>
/// <param name="Text">
/// The text that failed, as the query wrote it.
/// </param>
/// <param name="Message">
/// The sentence for a human reading a log.
/// </param>
/// <param name="Examples">
/// What was possible where the query failed.
/// </param>
public sealed record HLinqError(
    string ErrorCode,
    string? RootKeyword,
    string Text,
    string Message,
    IReadOnlyList<string> Examples)
{
    /// <summary>
    /// Creates an error with the string a client reads for <paramref name="code"/>.
    /// </summary>
    public static HLinqError Create(
        HLinqErrorCode code,
        string? rootKeyword,
        string text,
        string message,
        IReadOnlyList<string> examples) =>
        new(code.ToErrorString(), rootKeyword, text, message, examples);
}

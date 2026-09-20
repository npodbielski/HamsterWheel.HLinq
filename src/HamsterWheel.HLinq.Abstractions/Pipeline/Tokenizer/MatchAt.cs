namespace HamsterWheel.HLinq.Pipeline.Tokenizer;

/// <summary>
///     Result of matching a token's text rule against the tokenizer window:
///     <see cref="Discarded" /> - this text can never grow into the token (e.g. a letter
///     inside a number), <see cref="Possible" /> - the text is a valid prefix and may still grow,
///     <see cref="Complete" /> - the token is finished at <see cref="CompleteAt" />, a window
///     relative exclusive offset: the tokenizer commits the token and continues the window walk
///     at that offset, so characters a complete match does not cover start the next token
///     without being rescanned (<c>orderBy</c> + <c>D</c> is the example: <c>orderBy</c> does
///     not complete there, <c>orderByDescending</c> stays possible).
///     There is no score and no ordering between possibilities: a window either completes a
///     token or it does not.
/// </summary>
public readonly struct MatchAt : IEquatable<MatchAt>
{
    private enum MatchResult
    {
        Discarded,
        Possible,
        Completed
    }

    private readonly MatchResult _result;

    private MatchAt(MatchResult result, int completeAt)
    {
        _result = result;
        CompleteAt = completeAt;
    }

    public static MatchAt Discarded { get; } = new(MatchResult.Discarded, 0);

    public static MatchAt Possible { get; } = new(MatchResult.Possible, 0);

    public bool IsDiscarded => _result == MatchResult.Discarded;

    public bool IsPossible => _result == MatchResult.Possible;

    public bool IsCompleted => _result == MatchResult.Completed;

    public int CompleteAt { get; }

    public static MatchAt Complete(int completeAt) => new(MatchResult.Completed, completeAt);

    public static MatchAt CompleteAtEnd(int windowLength) => new(MatchResult.Completed, windowLength);

    public bool Equals(MatchAt other) => _result == other._result && CompleteAt == other.CompleteAt;

    public override bool Equals(object? obj) => obj is MatchAt other && Equals(other);

    public override int GetHashCode() => ((int)_result * 397) ^ CompleteAt;

    public static bool operator ==(MatchAt left, MatchAt right) => left.Equals(right);

    public static bool operator !=(MatchAt left, MatchAt right) => !left.Equals(right);

    public override string ToString() =>
        _result == MatchResult.Completed ? $"Complete({CompleteAt})" : _result.ToString();
}

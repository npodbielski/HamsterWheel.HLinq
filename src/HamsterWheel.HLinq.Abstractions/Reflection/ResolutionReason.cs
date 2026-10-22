namespace HamsterWheel.HLinq.Reflection;

/// <summary>
/// Member resolution enumeration.
/// </summary>
public enum ResolutionReason
{
    /// <summary>
    /// No member answered the name at all.
    /// </summary>
    NoCandidate,

    /// <summary>
    /// Members answered the name but none of them takes the arguments the query wrote.
    /// </summary>
    NoBindableCandidate,

    /// <summary>
    /// Exactly one member answered the name, so it won without competition.
    /// </summary>
    OnlyCandidate,

    /// <summary>
    /// Several members answered the name but only one of them takes the arguments the query wrote.
    /// </summary>
    OnlyBindableCandidate,

    /// <summary>
    /// Several members could take the arguments; the winner is the one the values convert into
    /// with the least conversion.
    /// </summary>
    CheapestConversion,

    /// <summary>
    /// The conversions were equal; the winner is the one whose name is spelled exactly as the query
    /// wrote it, while the other differs only in case.
    /// </summary>
    SpelledExactlyAsWritten,

    /// <summary>
    /// The conversions and spellings were equal; the winner is declared on the type the query
    /// reached the name on, while the other is inherited.
    /// </summary>
    DeclaredOnType,

    /// <summary>
    /// Nothing about the members decided - the winner is the first in the ordinal order of name,
    /// signature and declaring type, the one thing about the answer a caller cannot read off the
    /// query.
    /// </summary>
    OrdinalTieBreak
}

namespace HamsterWheel.HLinq.Diagnostics;

/// <summary>
/// The reason a caller could not have predicted which member their query reached.
/// </summary>
public enum HLinqMemberWarningReason
{
    /// <summary>
    /// More than one member of the type answered the name of the query and none was spelled as the
    /// query spelled it: an ordinal comparison chose between them.
    /// </summary>
    AmbiguousName,

    /// <summary>
    /// More than one overload bound the arguments of the call and the ranking rules left them equal:
    /// an ordinal comparison chose between them.
    /// </summary>
    AmbiguousCall,
}

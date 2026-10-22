namespace HamsterWheel.HLinq.Reflection;

/// <summary>
/// Resolved member of the type that should match the value token of the query.
/// </summary>
public sealed record MemberResolution<T>(T? Member, IReadOnlyList<T> Competitors, ResolutionReason Reason);

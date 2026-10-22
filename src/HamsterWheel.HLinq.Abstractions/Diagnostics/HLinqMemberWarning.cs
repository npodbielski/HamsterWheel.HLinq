namespace HamsterWheel.HLinq.Diagnostics;

/// <summary>
/// Ambigous member naming/selection warning. HLinq could not tell for certain but returned the best guess response instead of an error.
/// </summary>
/// <param name="Reason">
/// Whether it was the name of a member or the arguments of a call that several members answered to.
/// </param>
/// <param name="Member">
/// The member the query got, spelled the way a caller reads one: <c>Name</c> for a property,
/// <c>Sub(String)</c> for an overload, since the name alone cannot tell two of them apart.
/// </param>
/// <param name="Type">The type the member belongs to.</param>
/// <param name="Competitors">
/// Every member that answered the name, the one the query got among them.
/// </param>
/// <param name="Query">The query the warning was raised by, as it was written.</param>
public sealed record HLinqMemberWarning(
    HLinqMemberWarningReason Reason,
    string Member,
    string Type,
    IReadOnlyList<string> Competitors,
    string Query)
{
    /// <summary>
    /// Name of the header warnings are carried on.
    /// </summary>
    public const string Header = "X-HLinq-Warning";
}

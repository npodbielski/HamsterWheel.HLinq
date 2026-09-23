namespace HamsterWheel.HLinq.Tokens;

/// <summary>
///     A token opening a clause of a query (<c>select</c>, <c>where</c>, the ordering and the
///     paging roots). The clause a token belongs to is the clause opened by the last clause root
///     committed before it, and that is the whole extent of context the grammar keeps: it lets the
///     same character belong to different tokens in different clauses - <c>=</c> renames inside a
///     select clause and compares in every other one - without the tokenizer ever looking at
///     surrounding syntax. A localized root replaces the keyword of a token type, never the type,
///     so it keeps opening a clause of the same kind.
/// </summary>
public interface IClauseRootToken : IToken;

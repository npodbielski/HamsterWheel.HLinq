namespace HamsterWheel.HLinq.Pipeline.Tokenizer;

partial class HLinqTokenizer
{
    internal sealed record Language(
        Grammar Grammar,
        WordTranslation[] Words,
        Dictionary<Type, WordTranslation[]> ByClauseRoot,
        WordTranslation[] OutsideOfAnyClause,
        WordTranslation[] Starting);
}
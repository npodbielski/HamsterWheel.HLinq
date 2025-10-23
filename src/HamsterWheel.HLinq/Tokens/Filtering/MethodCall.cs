namespace HamsterWheel.HLinq.Tokens.Filtering;

public sealed class MethodCall(Range range) : MemberAccess(range)
{
    public sealed class Possibility(IGrammar grammar)
        : TokenPossibility<MethodCall>(grammar, haveDelimiters: true)
    {
        protected override bool PreviousTokenMatchImpl(IToken previousToken) =>
            Grammar.PreviousTokenMatch<MethodCall>(previousToken);

        protected override MethodCall BuildImpl(Range range) => new(range);
    }
}
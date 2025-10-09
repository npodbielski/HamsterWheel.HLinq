using HamsterWheel.HLinq.Parsers;
using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Filtering;

namespace HamsterWheel.HLinq.Tree.Filtering;

public sealed class MethodConstParam(IToken[] tokens) : TreeLeaf(tokens), IMethodParamElement
{
    public NameOrValue Value => Tokens.OfType<NameOrValue>().Single();

    public string GetValue(string hLinqQuery) => Value.GetValue(hLinqQuery);

    public sealed class Parser : ElementParserBase<MethodConstParam>
    {
        protected override Type[] ValidParents { get; } = [typeof(Method)];
        public override IToken[] ExampleTokens { get; } = [new NameOrValue(default)];

        protected override MethodConstParam? BuildBranch(IParsingContext context) =>
            context.Tokens switch
            {
                [NameOrValue _, Comma, ..] => new MethodConstParam(context.Tokens[..2]),
                [Comma, NameOrValue _, ..] => new MethodConstParam(context.Tokens[..2]),
                [NameOrValue _, RightCircleBracket, ..] => new MethodConstParam(context.Tokens[..1]),
                _ => null
            };
    }
}
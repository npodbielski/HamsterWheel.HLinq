using System.Linq.Expressions;
using HamsterWheel.HLinq.Builders;
using HamsterWheel.HLinq.Parsers;
using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Filtering;
using HamsterWheel.HLinq.Tree.Filtering;
using HamsterWheel.HLinq.Tree.Ordering;

namespace HamsterWheel.HLinq.Tree;

public class EntityLeaf(IToken token) : TreeLeaf([token]), IMethodParamElement
{
    public string GetValue(string hLinqQuery) => token.GetValue(hLinqQuery);

    public sealed class Parser : ElementParserBase<EntityLeaf>
    {
        public override IToken[] ExampleTokens { get; } = [Entity.Empty];

        protected override Type[] ValidParents { get; } =
        [
            typeof(Condition), typeof(OrderByRoot), typeof(OrderByDescendingRoot), typeof(ThenByRoot),
            typeof(ThenByDescendingRoot)
        ];

        protected override EntityLeaf? BuildBranch(IParsingContext context) =>
            context.Tokens is [Entity, IComparisonToken, ..] ? new EntityLeaf(context.Tokens[0]) : null;
    }

    public sealed class Converter : ElementToExpressionConverter<EntityLeaf>
    {
        protected override Expression Build(IBuilderContext context, EntityLeaf element) => context.Param;
    }
}
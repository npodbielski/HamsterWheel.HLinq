using HamsterWheel.HLinq.Pipeline.Applier;
using HamsterWheel.HLinq.Pipeline.Parser;
using HamsterWheel.HLinq.Reflection;
using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Paging;

namespace HamsterWheel.HLinq.Tree.Paging;

public sealed class CountRoot(IToken[] tokens) : TreeBranch(tokens)
{
    public override bool NoChildren => true;

    public sealed class Parser : ElementParserBase<CountRoot>
    {
        public override IToken[] ExampleTokens { get; } =
            [Count.Empty, LeftSquareBracket.Empty, RightSquareBracket.Empty];

        protected override CountRoot? BuildBranch(IParsingContext context) =>
            context.Tokens switch
            {
                [Count, LeftSquareBracket, RightSquareBracket] => new CountRoot(context.Tokens[..3]),
                [Dot, Count, LeftSquareBracket, RightSquareBracket] => new CountRoot(context.Tokens[..4]),
                _ => default
            };

        protected override void FinishImpl(IParsingContext context)
        {
            //count has no children, so no need to Finish checks
        }
    }

    public sealed class Applier(IMethodsCache methodsCache) : RootApplierBase<CountRoot>
    {
        protected override IQueryableContext ApplyImpl(IQueryableContext context, CountRoot where,
            string hLinqQuery)
        {
            var method = methodsCache.GetStaticGeneric(typeof(Queryable), nameof(Queryable.Count),
                infos => infos.Length == 1, context.CurrentResultType);

            return new QueryableContext(null, typeof(int), (int)method.Invoke(null, [context.Queryable])!);
        }
    }
}
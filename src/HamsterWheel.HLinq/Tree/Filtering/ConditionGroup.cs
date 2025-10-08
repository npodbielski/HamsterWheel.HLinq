using HamsterWheel.HLinq.Builders;
using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Parsers;
using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Filtering;

namespace HamsterWheel.HLinq.Tree.Filtering;

public sealed class ConditionGroup : TreeBranch, ILogicalOperationGroupBranch
{
    public ConditionGroup(LeftCircleBracket bracket) : base([bracket])
    {
    }

    public ConditionGroup(IConditionalLogicalOperationToken logical, LeftCircleBracket bracket) :
        base([(TokenBase)logical, bracket])
    {
    }

    public IConditionalLogicalOperationToken? ConditionalLogicalOp =>
        Tokens.OfType<IConditionalLogicalOperationToken>().SingleOrDefault();

    public sealed class Parser : ElementParserBase<ConditionGroup>
    {
        protected override Type[] ValidParents => [typeof(WhereRoot), typeof(ConditionGroup)];

        public override IToken[] ExampleTokens { get; } =
        [
            new LeftCircleBracket(default), .. Condition.Parser.ConditionExampleTokens, new RightCircleBracket(default)
        ];

        protected override ConditionGroup? BuildBranch(IParsingContext context)
        {
            return context.Tokens switch
            {
                [IConditionalLogicalOperationToken logical, LeftCircleBracket circleBracket, ..] =>
                    new ConditionGroup(logical, circleBracket),
                [LeftCircleBracket circleBracket, ..] => new ConditionGroup(circleBracket),
                _ => null
            };
        }

        protected override void FinishImpl(IParsingContext context)
        {
            if (context.Tokens is not [RightCircleBracket bracket, ..])
            {
                throw new InvalidTokenCollectionException(context.SourceQueryString, context.Tokens.Take(5).ToArray(),
                    [new RightCircleBracket(default)]);
            }

            context.CurrentBranch?.Finish(context, [bracket]);
            context.RemoveTokensFromStart(1);
        }
    }

    public sealed class Converter : ConditionalLogicalOperationConverter<ConditionGroup>;
}
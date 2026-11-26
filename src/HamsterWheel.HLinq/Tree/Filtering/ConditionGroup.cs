using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Pipeline.Applier.Builders;
using HamsterWheel.HLinq.Pipeline.Parser;
using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Filtering;

namespace HamsterWheel.HLinq.Tree.Filtering;

public sealed class ConditionGroup : TreeBranch, ILogicalOperationGroupBranch
{
    private ConditionGroup(LeftCircleBracket bracket) : base([bracket])
    {
    }

    private ConditionGroup(ILogicalOperatorToken logical, LeftCircleBracket bracket) :
        base([(TokenBase)logical, bracket])
    {
    }

    public ILogicalOperatorToken? LogicalOpToken => Tokens.OfType<ILogicalOperatorToken>().SingleOrDefault();

    // ReSharper disable once UnusedType.Global -> used by Parser
    public sealed class Parser : ElementParserBase<ConditionGroup>
    {
        public override Type[] ValidParents { get; } = [typeof(WhereRoot), typeof(ConditionGroup)];

        public override IToken[] ExampleTokens { get; } =
            [LeftCircleBracket.Empty, .. Condition.Parser.ConditionExampleTokens, RightCircleBracket.Empty];

        protected override ConditionGroup? BuildBranch(IParsingContext context) =>
            context.Tokens switch
            {
                [ILogicalOperatorToken logical, LeftCircleBracket circleBracket, ..] =>
                    new ConditionGroup(logical, circleBracket),
                [LeftCircleBracket circleBracket, ..] => new ConditionGroup(circleBracket),
                _ => null
            };

        protected override void FinishImpl(IParsingContext context)
        {
            if (context.Tokens is not [RightCircleBracket bracket, ..])
            {
                throw new InvalidTokenCollectionException(context.SourceQueryString, context.Tokens,
                    [RightCircleBracket.Empty]);
            }

            context.CurrentBranch?.Finish(context, [bracket]);
            context.RemoveStartTokens(1);
        }
    }

    // ReSharper disable once UnusedType.Global -> used by Applier
    public sealed class Converter : ConditionalLogicalOperationConverter<ConditionGroup>;
}
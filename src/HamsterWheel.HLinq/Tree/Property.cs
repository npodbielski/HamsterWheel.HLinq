using System.Linq.Expressions;
using HamsterWheel.HLinq.Pipeline.Applier.Builders;
using HamsterWheel.HLinq.Pipeline.Parser;
using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Filtering;
using HamsterWheel.HLinq.Tokens.Selecting;
using HamsterWheel.HLinq.Tree.Filtering;
using HamsterWheel.HLinq.Tree.Ordering;
using HamsterWheel.HLinq.Tree.Selecting;

namespace HamsterWheel.HLinq.Tree;

public sealed class Property(IToken[] tokens) : TreeLeaf(tokens), IMethodParamElement
{
    public string[] GetPath(string hLinqQuery) =>
        Tokens.OfType<PropertyName>().Select(t => t.GetValue(hLinqQuery)).ToArray();

    public string GetValue(string hLinqQuery) => string.Join('.', GetPath(hLinqQuery));

    public sealed class Parser : ElementParserBase<Property>
    {
        public override IToken[] ExampleTokens { get; } = [Entity.Empty, Dot.Empty, PropertyName.Empty];

        protected override Type[] ValidParents { get; } =
        [
            typeof(Condition), typeof(Method), typeof(OrderByRoot), typeof(OrderByDescendingRoot), typeof(ThenByRoot),
            typeof(ThenByDescendingRoot), typeof(PropertyAssignment)
        ];

        protected override Property? BuildBranch(IParsingContext context)
        {
            var index = 0;

            if (context.Tokens.First() is Assignment)
            {
                index += 1;
            }

            if (context.Tokens[index..] is [Entity, IComparisonToken, ..])
            {
                return new Property(context.Tokens[..1]);
            }

            if (context.Tokens[index..] is not [Entity, Dot, PropertyName, .. var r1])
            {
                return null;
            }

            var rest = r1;

            index += 3;

            while (rest is [Dot, PropertyName, .. var r2])
            {
                rest = r2;
                index += 2;
            }

            return new Property(context.Tokens[..index]);
        }
    }

    public sealed class Converter : ElementToExpressionConverter<Property>
    {
        protected override Expression Build(IBuilderContext context, Property element)
        {
            var path = element.GetPath(context.HLinqQuery);
            var memberExpression = context.Builder.GetProperty(context.Type, context.Param, path).Member;
            switch (context)
            {
                case IArithmeticComparisonConditionBuilderContext conditionBuilderContext:
                    conditionBuilderContext.ComparisonType = memberExpression.Type;
                    break;
                case IMethodCallConditionBuilderContext methodCallConditionBuilderContext:
                    methodCallConditionBuilderContext.MethodSource = memberExpression;
                    break;
            }

            return memberExpression;
        }
    }
}
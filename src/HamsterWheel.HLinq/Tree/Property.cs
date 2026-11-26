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

public sealed class Property(IToken[] tokens) : TreeLeaf(tokens), IProperty
{
    public string[] GetPath(string hLinqQuery) =>
        [.. this.GetTokensOf<PropertyName>().Select(t => t.GetValue(hLinqQuery))];

    public string GetValue(string hLinqQuery) => GetPath(hLinqQuery).JoinWithDot();

    /// <summary>
    /// The last segment of the path: <c>contact</c> in <c>x.customer.address.contact</c>.
    /// </summary>
    public string GetLastName(string hLinqQuery) =>
        this.GetTokensOf<PropertyName>().Last().GetValue(hLinqQuery);

    // ReSharper disable once UnusedType.Global -> used by parser
    public sealed class Parser : ElementParserBase<Property>
    {
        public override IToken[] ExampleTokens { get; } = [Entity.Empty, Dot.Empty, PropertyName.Empty];

        public override Type[] ValidParents { get; } =
        [
            typeof(Condition), typeof(Method), typeof(OrderByRoot), typeof(OrderByDescendingRoot),
            typeof(ThenByRoot), typeof(ThenByDescendingRoot), typeof(PropertyAssignment),
            typeof(NestedSelect), typeof(MathTerm), typeof(MathGroup)
        ];

        protected override Property? BuildBranch(IParsingContext context)
        {
            var index = 0;

            if (context.Tokens.First() is Assignment)
            {
                index += 1;
            }

            if (context.CurrentBranch is Method && context.Tokens[0] is Comma)
            {
                index += 1;
            }

            if (index == 0 && context.Tokens is [Entity, IComparisonToken, ..])
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

    // ReSharper disable once UnusedType.Global -> used by Applier
    public sealed class Converter : ElementToExpressionConverter<Property>
    {
        protected override Expression Build(IBuilderContext context, Property element)
        {
            var path = element.GetPath(context.HLinqQuery);
            var memberExpression = context.Builder.GetProperty(context.Type, context.Param, path, context).Member;
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
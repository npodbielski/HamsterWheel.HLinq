using System.Linq.Expressions;
using HamsterWheel.HLinq.Data.ValueConverters;
using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Pipeline.Applier.Builders;
using HamsterWheel.HLinq.Pipeline.Parser;
using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Filtering;

namespace HamsterWheel.HLinq.Tree.Filtering;

public sealed class ComparisonConst(NameOrValue value) : TreeLeaf([value])
{
    private NameOrValue Value { get; } = value;

    public sealed class Parser : ElementParserBase<ComparisonConst>
    {
        protected override Type[] ValidParents { get; } = [typeof(Condition)];
        public override IToken[] ExampleTokens { get; } = [NameOrValue.Empty];

        protected override ComparisonConst? BuildBranch(IParsingContext context)
        {
            return context.Tokens switch
            {
                [NameOrValue value, IComparisonToken, ..] => new ComparisonConst(value),
                [NameOrValue value, ILogicalOperatorToken or RightCircleBracket or RightSquareBracket, ..]
                    => new ComparisonConst(value),
                _ => null
            };
        }
    }

    public sealed class Converter(IValueConverterFactory factory) : ElementToExpressionConverter<ComparisonConst>
    {
        protected override Expression Build(IBuilderContext context, ComparisonConst element)
        {
            var propType = (context as IArithmeticComparisonConditionBuilderContext)?.ComparisonType ??
                           throw new ElementToExpressionConverterPropertyTypeNullException();

            var stringValue = element.Value.GetValue(context.HLinqQuery);

            object? value;
            if (propType == typeof(string))
            {
                value = stringValue;
                if (stringValue == "null")
                {
                    value = null;
                }
            }
            else
            {
                value = factory.GetConverterFor(propType).Convert(stringValue);
            }

            return Expression.Constant(value, propType);
        }

        private sealed class ElementToExpressionConverterPropertyTypeNullException()
            : HLinqQueryException("At this point property type needs to have value!");
    }
}
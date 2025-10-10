using System.Linq.Expressions;
using HamsterWheel.HLinq.Builders;
using HamsterWheel.HLinq.Exceptions;
using HamsterWheel.HLinq.Parsers;
using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Filtering;
using HamsterWheel.HLinq.ValueConverters;

namespace HamsterWheel.HLinq.Tree.Filtering;

public sealed class ComparisonConstant(NameOrValue value) : TreeLeaf([value])
{
    public NameOrValue Value { get; } = value;

    public sealed class Parser : ElementParserBase<ComparisonConstant>
    {
        protected override Type[] ValidParents { get; } = [typeof(Condition)];
        public override IToken[] ExampleTokens { get; } = [new NameOrValue(default)];

        protected override ComparisonConstant? BuildBranch(IParsingContext context)
        {
            return context.Tokens switch
            {
                [NameOrValue value, IComparisonToken, ..] => new ComparisonConstant(value),
                [NameOrValue value, ILogicalOperatorToken or RightCircleBracket or RightSquareBracket, ..]
                    => new ComparisonConstant(value),
                _ => null
            };
        }
    }

    public sealed class Converter(IValueConverterFactory factory) : ElementToExpressionConverter<ComparisonConstant>
    {
        protected override Expression Build(IBuilderContext context, ComparisonConstant element)
        {
            var propType = context.ComparisonPropertyType ??
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
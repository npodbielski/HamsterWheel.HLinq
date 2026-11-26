using System.Globalization;
using System.Linq.Expressions;
using HamsterWheel.HLinq.Data.Converters;
using HamsterWheel.HLinq.Pipeline.Applier.Builders;
using HamsterWheel.HLinq.Pipeline.Parser;
using HamsterWheel.HLinq.Tokens;
using HamsterWheel.HLinq.Tokens.Selecting;

namespace HamsterWheel.HLinq.Tree.Selecting;

/// <summary>
/// A written operand read as the literal the C# compiler would give the same text.
/// </summary>
public sealed class MathConstant(IToken[] tokens) : TreeLeaf(tokens)
{
    // ReSharper disable once UnusedType.Global -> used by the Parser
    public sealed class Parser : ElementParserBase<MathConstant>
    {
        public override Type[] ValidParents { get; } = [typeof(MathTerm), typeof(MathGroup)];

        public override IToken[] ExampleTokens { get; } = [MathOperand.Empty];

        protected override MathConstant? BuildBranch(IParsingContext context) =>
            context.Tokens switch
            {
                [MathOperand, ..] => new MathConstant(context.Tokens[..1]),
                [NameOrValue { } value, ..]
                    when IsQuotedTextOrWrittenNumber(value.GetValue(context.SourceQueryString))
                    => new MathConstant(context.Tokens[..1]),
                _ => null
            };

        private static bool IsQuotedTextOrWrittenNumber(string text) =>
            text.StartsWith('"') || text.All(char.IsDigit);
    }

    // ReSharper disable once UnusedType.Global -> used by the Applier
    public sealed class Converter(IDefaultConverter defaultConverter)
        : ElementToExpressionConverter<MathConstant>
    {
        protected override Expression Build(IBuilderContext context, MathConstant element)
        {
            var text = element.Tokens[0].GetValue(context.HLinqQuery);
            if (text.StartsWith('"'))
            {
                return Expression.Constant(text[1..^1], typeof(string));
            }

            var type = TypeOfNumber(text, context.Culture);
            return Expression.Constant(defaultConverter.ConvertTo(type, text, context.Culture)!, type);
        }

        //the written number is read in the culture the query names, the way every other value of
        //the query is read: where the language's separator is a comma, `1,5` is the number and
        //`1.5` is not one
        private static Type TypeOfNumber(string text, CultureInfo culture)
        {
            if (text.Contains(culture.NumberFormat.NumberDecimalSeparator, StringComparison.Ordinal))
            {
                return typeof(double);
            }

            if (int.TryParse(text, NumberStyles.Integer, culture, out _))
            {
                return typeof(int);
            }

            return long.TryParse(text, NumberStyles.Integer, culture, out _)
                ? typeof(long)
                : typeof(double);
        }
    }
}

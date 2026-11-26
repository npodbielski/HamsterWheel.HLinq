using System.Linq.Expressions;
using HamsterWheel.HLinq.Pipeline.Parser;
using HamsterWheel.HLinq.Tree;
using HamsterWheel.HLinq.Tree.Filtering;
using HamsterWheel.HLinq.Tree.Selecting;

namespace HamsterWheel.HLinq.Pipeline.Applier.Builders;

//The value a clause reads: a filter's side of a comparison, a call standing on its own, the one
//value of an ordering. A call over a member is two children of its clause - the member and the
//call - so a value is named by what answers it and never by a position among the children.
public sealed partial class ExpressionBuilder
{
    public Expression BuildValue(IBuilderContext context, IReadOnlyList<ITreeElement> value)
    {
        if (value.Any(element => element is MathTerm or MathGroup))
        {
            var folded = FoldMath(context, value);

            //a run that hangs on the row is the expression of that row, whatever it computes; a
            //run of nothing but written values computes one value the query could have written
            return AnswersWithItsOwnType(value)
                ? folded
                : FoldedConstant(folded, context);
        }

        if (value.OfType<Method>().SingleOrDefault() is not { } call)
        {
            return value.Count switch
            {
                1 when value[0] is EntityLeaf => context.Param,
                1 => context.ToExpression(value[0]),
                _ => throw new MathValueHoldsNothingException()
            };
        }

        //the member a call hangs on is what the call is built over, so it is named as the source
        //of the context the call reads; leaving it to the member's converter to write into the
        //context would build an expression of the member and throw it away to pass one value
        //sideways
        var source = value.TakeWhile(element => element is not Method).OfType<IProperty>().SingleOrDefault();
        var callContext = new MethodCallConditionBuilderContext(context)
        {
            MethodSource = source is null ? null : GetCallMember(context, source)
        };

        return callContext.ToExpression(call);
    }

    public bool AnswersWithItsOwnType(IReadOnlyList<ITreeElement> value) => value.Any(element => element switch
    {
        //a member answers the type it has, a call the type it returns, the row answers the type
        //the clause reads, and a math run answers what the value it hangs on answers, however
        //deep a group it stands in; written values alone answer nothing of their own
        Property or EntityLeaf or Method => true,
        MathGroup group => AnswersWithItsOwnType(group.Children),
        MathTerm term => AnswersWithItsOwnType(term.Children),
        _ => false
    });

    /// <summary>
    /// Turns a math run of written values into the constant it computes, read as a written value is.
    /// </summary>
    private Expression FoldedConstant(Expression folded, IBuilderContext context)
    {
        //the run holds no member and no call, so the fold is arithmetic over written values alone:
        //what the compiler itself would have computed for the same text written by hand
        var value = Expression.Lambda(folded).Compile().DynamicInvoke()!;

        //the converters read a text, so the computed number travels as the text the query would
        //have written it as, in the culture of the query - the year rule and every override a
        //host writes then read a computed number exactly as they read a written one
        if (context is IArithmeticComparisonConditionBuilderContext { ComparisonType: { } compared } comparison
            && compared != folded.Type)
        {
            var text = value is IFormattable formattable ? formattable.ToString(null, context.Culture) : value;
            return Expression.Constant(comparison.Converter.ConvertTo(compared, text, context.Culture), compared);
        }

        return Expression.Constant(value, folded.Type);
    }
}

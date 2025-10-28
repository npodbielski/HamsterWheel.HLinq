using System.Linq.Expressions;
using HamsterWheel.HLinq.Pipeline.Applier.Builders;
using HamsterWheel.HLinq.Reflection;
using HamsterWheel.HLinq.Tree.Filtering;

namespace HamsterWheel.HLinq.Demo.Extensions.Filters;

public class CustomFilterConverter(IPropertiesCache propertiesCache) : IStaticMethodToExpressionConverter
{
    private readonly StaticMethodToExpressionConverter _converter = new();

    public Expression BuildStatic(IBuilderContext context, IMethod method, IParametersConverter parametersConverter)
    {
        if (method.GetName(context.HLinqQuery) != "hasFullName")
        {
            return _converter.BuildStatic(context, method, parametersConverter);
        }

        var fullNameSearchConstant = method.Children[1].Tokens[0].GetValue(context.HLinqQuery);

        //the below expression is equivalent to:
        //LambdaExpression condition = (Person p) => p.FirstName + " " + p.LastName == fullNameSearchConstant;

        var stringConcatMethod = typeof(string).GetMethod("Concat", [typeof(string), typeof(string)]);

        var firstNamePlusSpace = Expression.Add(Expression.Property(context.Param, propertiesCache.Single(context.Type, "FirstName")!), Expression.Constant(" "), stringConcatMethod);
        var firstNameSpaceAndLastName = Expression.Add(firstNamePlusSpace, Expression.Property(context.Param, propertiesCache.Single(context.Type, "LastName")!), stringConcatMethod);
        return Expression.Equal(firstNameSpaceAndLastName, Expression.Constant(fullNameSearchConstant));
    }
}
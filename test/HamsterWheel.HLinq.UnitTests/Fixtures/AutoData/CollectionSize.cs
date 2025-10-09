using System.Reflection;
using AutoFixture;
using AutoFixture.Kernel;
using AutoFixture.Xunit2;

namespace HamsterWheel.HLinq.UnitTests.Fixtures.AutoData;

public class CollectionSizeAttribute(int size) : CustomizeAttribute
{
    public override ICustomization GetCustomization(ParameterInfo parameter)
    {
        ArgumentNullException.ThrowIfNull(parameter);

        Type? objectType;
        if (!parameter.ParameterType.IsArray)
        {
            objectType = parameter.ParameterType.GetGenericArguments()[0];

            var isTypeCompatible =
                    parameter.ParameterType.IsGenericType
                    && parameter.ParameterType.GetGenericTypeDefinition().MakeGenericType(objectType)
                        .IsAssignableFrom(typeof(List<>).MakeGenericType(objectType));

            if (!isTypeCompatible)
            {
                throw new InvalidOperationException(
                    $"{nameof(CollectionSizeAttribute)} specified for type incompatible with List: {parameter.ParameterType} {parameter.Name}");
            }

            var customizationType = typeof(CollectionSizeCustomization<>).MakeGenericType(objectType);
            return (ICustomization)Activator.CreateInstance(customizationType, parameter, size)!;
        }
        else
        {
            objectType = parameter.ParameterType.GetElementType() ??
                         throw new TypeAccessException("Parameter of test method cannot have not type!");

            var customizationType = typeof(ArraySizeCustomization<>).MakeGenericType(objectType);
            return (ICustomization)Activator.CreateInstance(customizationType, parameter, size)!;
        }
    }

    private class CollectionSizeCustomization<T>(ParameterInfo parameter, int repeatCount) : ICustomization
    {
        public void Customize(IFixture fixture)
        {
            fixture.Customizations.Add(new FilteringSpecimenBuilder(
                new FixedBuilder(fixture.CreateMany<T>(repeatCount).ToList()),
                new EqualRequestSpecification(parameter)
            ));
        }
    }

    private class ArraySizeCustomization<T>(ParameterInfo parameter, int repeatCount) : ICustomization
    {
        public void Customize(IFixture fixture)
        {
            fixture.Customizations.Add(new FilteringSpecimenBuilder(
                new FixedBuilder(fixture.CreateMany<T>(repeatCount).ToArray()),
                new EqualRequestSpecification(parameter)
            ));
        }
    }
}
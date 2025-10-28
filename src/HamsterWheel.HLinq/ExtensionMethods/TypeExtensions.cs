namespace HamsterWheel.HLinq;

public static class TypeExtensions
{
    public static bool IsGenericOf(this Type type, Type expectedGeneric)
    {
        if (!expectedGeneric.IsGenericType)
        {
            throw new ArgumentException($"{nameof(expectedGeneric)} should be generic type!");
        }

        return type.IsGenericType && type.GetGenericTypeDefinition() == expectedGeneric.GetGenericTypeDefinition();
    }

    public static bool IsNullable(this Type type) => type.IsGenericOf(typeof(Nullable<>));

    public static Type GetNullableArgument(this Type type) =>
        type.IsNullable() ? type.GetGenericArguments().First() : type;
}
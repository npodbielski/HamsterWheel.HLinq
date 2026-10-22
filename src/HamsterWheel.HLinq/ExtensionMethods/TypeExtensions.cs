namespace HamsterWheel.HLinq;

public static class TypeExtensions
{
    extension(Type type)
    {
        public bool IsGenericOf(Type expectedGeneric)
        {
            if (!expectedGeneric.IsGenericType)
            {
                throw new ArgumentException($"{nameof(expectedGeneric)} should be generic type!");
            }

            return type.IsGenericType && type.GetGenericTypeDefinition() == expectedGeneric.GetGenericTypeDefinition();
        }

        public bool IsNullable() => type.IsGenericOf(typeof(Nullable<>));

        public Type GetNullableArgument() => type.IsNullable() ? type.GetGenericArguments().First() : type;

        /// <summary>
        /// Returns true if two types are compatible to use via Expression
        /// (i.e., if property can be used as a method parameter)
        /// </summary>
        public bool AcceptsArgumentOfType(Type argumentType) =>
            type == argumentType
            || (!argumentType.IsValueType && !type.IsValueType && type.IsAssignableFrom(argumentType));
    }
}

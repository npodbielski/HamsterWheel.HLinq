#if NETSTANDARD2_0
// ReSharper disable once CheckNamespace; Reason: compiler requires those attributes in this exact namespace  
// ReSharper disable UnusedType.Global; Reason those files are imported in other projects and used indirectly there via compiler
namespace System.Diagnostics.CodeAnalysis;

/// <summary>Specifies that this constructor sets all required members for the current type, and callers do not need to set any required members themselves.</summary>
[AttributeUsage(AttributeTargets.Constructor)]
internal sealed class SetsRequiredMembersAttribute : Attribute;
#endif

#if NETSTANDARD2_0
// ReSharper disable once CheckNamespace; Reason: compiler requires those attributes in this exact namespace  
// ReSharper disable UnusedType.Global; Reason those files are imported in other projects and used indirectly there via compiler
using System.Diagnostics.CodeAnalysis;
namespace System.Runtime.CompilerServices;

//"This is added just to enable interpolated string parameters for .net standard version of the library and this code is not actually used."
[ExcludeFromCodeCoverage]
/// <summary>Indicates the attributed type is to be used as an interpolated string handler.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
internal sealed class InterpolatedStringHandlerAttribute : Attribute;
#endif
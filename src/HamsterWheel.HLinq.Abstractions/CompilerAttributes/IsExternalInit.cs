#if NETSTANDARD2_0
using System.Diagnostics.CodeAnalysis;

// ReSharper disable once CheckNamespace; Reason: compiler requires those attributes in this exact namespace  
// ReSharper disable UnusedType.Global; Reason those files are imported in other projects and used indirectly there via compiler
namespace System.Runtime.CompilerServices;

//"This is added just to enable init setters for .net standard version of the library and this code is not actually used."
/// <summary>Reserved to be used by the compiler for tracking metadata.
/// This class should not be used by developers in source code.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class IsExternalInit;
#endif
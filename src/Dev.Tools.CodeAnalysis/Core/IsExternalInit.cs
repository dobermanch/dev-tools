// ReSharper disable once CheckNamespace
namespace System.Runtime.CompilerServices;

/// <summary>
/// The compiler needs this type to emit an <c>init</c> accessor, and netstandard2.0 - which
/// analyzers and source generators have to target - does not ship it. Declaring it here is the
/// standard polyfill and lets the generator models be positional records.
/// </summary>
internal static class IsExternalInit;

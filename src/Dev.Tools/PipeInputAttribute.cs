namespace Dev.Tools;

/// <summary>
/// Marks a property on a tool's Args record as the primary input
/// that receives piped data from a previous tool's output.
/// Only one property per Args type should have this attribute.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class PipeInputAttribute : Attribute;

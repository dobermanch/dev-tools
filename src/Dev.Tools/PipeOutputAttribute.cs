namespace Dev.Tools;

/// <summary>
/// Marks a property on a tool's Result record as the primary output
/// that can be piped to the next tool's input.
/// Only one property per Result type should have this attribute.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class PipeOutputAttribute : Attribute;

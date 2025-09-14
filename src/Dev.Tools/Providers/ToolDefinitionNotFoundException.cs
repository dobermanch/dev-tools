namespace Dev.Tools.Providers;

public sealed class ToolDefinitionNotFoundException(string name) 
    : Exception($"The {name} tool definition not found");
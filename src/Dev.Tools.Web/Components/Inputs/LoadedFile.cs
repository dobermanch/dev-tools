namespace Dev.Tools.Web.Components.Inputs;

/// <summary>A file the user loaded into a field, handed to a page whose tool takes <see cref="ToolContent"/>.</summary>
public sealed record LoadedFile(string Name, ToolContent Content);

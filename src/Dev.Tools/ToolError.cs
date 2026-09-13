using System.Reflection;

namespace Dev.Tools;

/// <summary>
/// One reason a tool could not produce its result.
/// </summary>
/// <param name="Code">
/// The culture-neutral reason. The Web UI translates it; every other frontend shows <see cref="Message"/>.
/// </param>
/// <param name="Target">
/// The name of the <c>Args</c> property the error is about, or null when it is about the call as a whole.
/// Each frontend spells it the way its users do: <c>--key</c> on the command line, <c>key</c> in the API
/// request body and the MCP input schema.
/// </param>
public sealed record ToolError(ErrorCode Code, string? Target = null)
{
    private static readonly IReadOnlyDictionary<ErrorCode, string> Messages = Enum
        .GetValues<ErrorCode>()
        .ToDictionary(
            it => it,
            it => typeof(ErrorCode).GetField(it.ToString())?.GetCustomAttribute<DescriptionAttribute>()?.Description
                  ?? it.ToString());

    /// <summary>The English message authored on the <see cref="ErrorCode"/> member.</summary>
    public string Message => Messages.TryGetValue(Code, out var message) ? message : Code.ToString();
}

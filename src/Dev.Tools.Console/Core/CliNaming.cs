using System.Text;

namespace Dev.Tools.Console.Core;

/// <summary>
/// How an argument is spelled on the command line: kebab-case, the way a shell user types it -
/// <c>EncodeNonAscii</c> is <c>--encode-non-ascii</c>. Compiled into both the generator, which writes
/// the option templates, and the CLI, which names an argument in an error, so the two cannot drift apart.
/// </summary>
internal static class CliNaming
{
    /// <summary>
    /// <c>UrlSafe</c> → <c>url-safe</c>, <c>Utf8Bytes</c> → <c>utf8-bytes</c>, <c>IPAddress</c> → <c>ip-address</c>.
    /// </summary>
    public static string Kebab(string propertyName)
    {
        var builder = new StringBuilder(propertyName.Length + 4);
        for (var i = 0; i < propertyName.Length; i++)
        {
            var ch = propertyName[i];
            if (!char.IsUpper(ch))
            {
                builder.Append(ch);
                continue;
            }

            var previous = i > 0 ? propertyName[i - 1] : '\0';
            var startsWord = char.IsLower(previous) || char.IsDigit(previous);
            var endsAcronym = char.IsUpper(previous) && i + 1 < propertyName.Length && char.IsLower(propertyName[i + 1]);
            if (startsWord || endsAcronym)
            {
                builder.Append('-');
            }

            builder.Append(char.ToLowerInvariant(ch));
        }

        return builder.ToString();
    }
}

using System.Text.RegularExpressions;
using Net.Codecrete.QrCodeGenerator;

namespace Dev.Tools.Tools;

[ToolDefinition(
    Name = "qr-code",
    Title = "QR Code Generator",
    Description =
        """
        Generates a QR code as an SVG image, for any text or for joining a Wi-Fi network.

        Text is encoded as UTF-8 in the smallest QR code that holds it; a URL, a phone number or plain text
        all work, since the scanner decides what to do with what it reads. The Wi-Fi content encodes the
        network name, password, security and whether the network is hidden in the WIFI: format phones read to
        join a network, with the characters that format reserves escaped.

        ErrorCorrection is how much of the code can be damaged or covered and still scan, from 7% (Low) to 30%
        (High), and more of it makes the code larger; the level used is raised above the one asked for when it
        fits in the same size. Border is the quiet zone around the code in modules, and scanners expect 4.
        Colors are #rgb or #rrggbb, and a dark color on a light one scans best.

        The SVG is a complete document whose viewBox is one unit per module, so it scales to any size without
        blurring. Up to 2,953 bytes of text fit with Low error correction, fewer with more.
        """,
    Usage =
        "Use this to produce a QR code as SVG text - to embed in HTML or save as a .svg file - for a URL, some text or Wi-Fi credentials. It only generates codes; it cannot read an existing one.",
    Aliases = ["qr"],
    Keywords = [Keyword.Qr, Keyword.Generate, Keyword.Url],
    Categories = [Category.Converter, Category.Web]
)]
public sealed partial class QrCodeTool : ToolBase<QrCodeTool.Args, QrCodeTool.Result>
{
    private const int MaxBorder = 20;

    // The colors are written into the SVG's attributes, so nothing but a hex color may reach them.
    [GeneratedRegex("^#([0-9a-fA-F]{3}|[0-9a-fA-F]{6})$")]
    private static partial Regex ColorFormat();

    protected override Result Execute(Args args)
    {
        var wifi = args.Content == QrContent.WiFi;

        var errors = new List<ToolError>();
        if (string.IsNullOrEmpty(args.Text))
        {
            errors.Add(new ToolError(ErrorCode.TextEmpty, nameof(Args.Text)));
        }

        if (wifi && args.Security != WifiSecurity.None && string.IsNullOrEmpty(args.Password))
        {
            errors.Add(new ToolError(ErrorCode.TextEmpty, nameof(Args.Password)));
        }

        if (!ColorFormat().IsMatch(args.DarkColor ?? string.Empty))
        {
            errors.Add(new ToolError(ErrorCode.InputNotValid, nameof(Args.DarkColor)));
        }

        if (!ColorFormat().IsMatch(args.LightColor ?? string.Empty))
        {
            errors.Add(new ToolError(ErrorCode.InputNotValid, nameof(Args.LightColor)));
        }

        if (errors.Count > 0)
        {
            throw new ToolException(errors);
        }

        var payload = wifi ? WifiPayload(args) : args.Text!;

        QrCode code;
        try
        {
            code = QrCode.EncodeText(payload, EccOf(args.ErrorCorrection));
        }
        catch (DataTooLongException)
        {
            throw new ToolException(ErrorCode.DataTooLong, nameof(Args.Text));
        }

        var svg = code.ToSvgString(
            Math.Clamp(args.Border, 0, MaxBorder),
            args.DarkColor!.ToLowerInvariant(),
            args.LightColor!.ToLowerInvariant());

        return new Result(svg, payload, code.Version, code.Size, LevelOf(code.ErrorCorrectionLevel));
    }

    // The format ZXing defined and phone cameras read: WIFI:T:<security>;S:<name>;P:<password>;H:true;;
    private static string WifiPayload(Args args)
    {
        var payload = new StringBuilder("WIFI:T:").Append(args.Security switch
        {
            WifiSecurity.Wep => "WEP",
            WifiSecurity.None => "nopass",
            _ => "WPA"
        });

        payload.Append(";S:").Append(Escape(args.Text!));
        if (args.Security != WifiSecurity.None)
        {
            payload.Append(";P:").Append(Escape(args.Password!));
        }

        if (args.HiddenNetwork)
        {
            payload.Append(";H:true");
        }

        return payload.Append(";;").ToString();
    }

    // A backslash before each character the format reserves. Values that look like hex are not put in quotes,
    // as some generators do: readers differ on whether the quotes are then part of the password.
    private static string Escape(string value)
        => string.Concat(value.Select(it => it is '\\' or ';' or ',' or ':' or '"' ? "\\" + it : it.ToString()));

    private static QrCode.Ecc EccOf(QrErrorCorrection level) => level switch
    {
        QrErrorCorrection.Low => QrCode.Ecc.Low,
        QrErrorCorrection.Quartile => QrCode.Ecc.Quartile,
        QrErrorCorrection.High => QrCode.Ecc.High,
        _ => QrCode.Ecc.Medium
    };

    private static QrErrorCorrection LevelOf(QrCode.Ecc ecc)
        => ecc == QrCode.Ecc.Low ? QrErrorCorrection.Low
            : ecc == QrCode.Ecc.Quartile ? QrErrorCorrection.Quartile
            : ecc == QrCode.Ecc.High ? QrErrorCorrection.High
            : QrErrorCorrection.Medium;

    public enum QrContent
    {
        [Description("Any text, such as a URL")]
        Text,

        [Description("A Wi-Fi network to join")]
        WiFi
    }

    public enum QrErrorCorrection
    {
        [Description("Low: survives 7% damage")]
        Low,

        [Description("Medium: survives 15% damage")]
        Medium,

        [Description("Quartile: survives 25% damage")]
        Quartile,

        [Description("High: survives 30% damage")]
        High
    }

    public enum WifiSecurity
    {
        [Description("WPA, WPA2 or WPA3, with a password")]
        Wpa,

        [Description("WEP, with a password")]
        Wep,

        [Description("An open network with no password")]
        None
    }

    public sealed record Args(
        [property: PipeInput,
                   Description("The text to encode, such as a URL; for Wi-Fi, the network name (SSID)")]
        string? Text,
        [property: Description("What the code holds: the text itself, or a Wi-Fi network to join")]
        QrContent Content = QrContent.Text,
        [property: Description("How much damage the code survives; more makes it larger")]
        QrErrorCorrection ErrorCorrection = QrErrorCorrection.Medium,
        [property: Description("The quiet zone around the code, in modules; scanners expect 4. Held between 0 and 20")]
        int Border = 4,
        [property: Description("Color of the dark modules, as #rgb or #rrggbb")]
        string DarkColor = "#000000",
        [property: Description("Color of the light modules and the border, as #rgb or #rrggbb")]
        string LightColor = "#ffffff",
        [property: Description("Wi-Fi only: the network password; not needed for an open network")]
        string? Password = null,
        [property: Description("Wi-Fi only: how the network is secured")]
        WifiSecurity Security = WifiSecurity.Wpa,
        [property: Description("Wi-Fi only: whether the network does not broadcast its name")]
        bool HiddenNetwork = false
    );

    public sealed record Result(
        [property: PipeOutput,
                   DisplayName("SVG"),
                   Description("The QR code as a complete SVG document, one viewBox unit per module")]
        string Svg,
        [property: Description("The exact text the code holds: the input, or the WIFI: string built from the network details")]
        string Payload,
        [property: Description("The QR code version, from 1 to 40; each version adds 4 modules a side")]
        int Version,
        [property: Description("Modules along one side, without the border")]
        int Size,
        [property: Description("The error correction used, which may be above the level asked for")]
        QrErrorCorrection ErrorCorrection
    ) : ToolResult
    {
        public Result() : this(string.Empty, string.Empty, 0, 0, QrErrorCorrection.Medium)
        {
        }
    }
}

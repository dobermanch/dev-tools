using System.Globalization;
using Dev.Tools.Colors;

namespace Dev.Tools.Tools;

[ToolDefinition(
    Name = "color-converter",
    Title = "Color Converter",
    Description =
        """
        Converts a color between the notations CSS and design tools use: hex, RGB, HSL, HWB, CMYK and the CSS
        color names.

        The input is any of them: #rgb, #rgba, #rrggbb or #rrggbbaa, with or without the #; rgb() and rgba(),
        hsl() and hsla(), and hwb(), with commas or spaces between the values and an optional alpha after a
        slash; cmyk() with percentages, or device-cmyk() with numbers from 0 to 1; or one of the 148 CSS color
        names, or transparent. A hue may carry deg, grad, rad or turn, and a value outside its range is
        clamped, as CSS does.

        Every output is written from the color rounded to 8 bits a channel, so they all describe the color the
        hex does; HSL, HWB and CMYK are rounded to one decimal place. An alpha below 1 is kept in the hex, RGB,
        HSL and HWB forms. CMYK is the plain formula with no color profile, so it is not what a printer makes of
        the color, and it has no alpha. Name is the CSS name of exactly this color, when it has one.
        """,
    Usage =
        "Use this instead of converting color values in your head: HSL and HWB arithmetic and hex with alpha are easy to get slightly wrong. The CMYK here is a formula, not a print-accurate separation.",
    Aliases = ["color"],
    Keywords = [Keyword.Color, Keyword.Convert, Keyword.Web],
    Categories = [Category.Converter, Category.Web]
)]
public sealed class ColorConverterTool : ToolBase<ColorConverterTool.Args, ColorConverterTool.Result>
{
    protected override Result Execute(Args args)
    {
        if (string.IsNullOrWhiteSpace(args.Color))
        {
            throw new ToolException(ErrorCode.TextEmpty, nameof(Args.Color));
        }

        var color = Parse(args.Color.Trim().ToLowerInvariant())
                    ?? throw new ToolException(ErrorCode.InputNotValid, nameof(Args.Color));

        return Describe(color);
    }

    // Channels from 0 to 1.
    private readonly record struct Rgba(double R, double G, double B, double A);

    private static Rgba? Parse(string text)
    {
        if (text == "transparent")
        {
            return new Rgba(0, 0, 0, 0);
        }

        if (CssColorNames.ByName.TryGetValue(text, out var named))
        {
            return new Rgba((named >> 16) / 255.0, ((named >> 8) & 0xFF) / 255.0, (named & 0xFF) / 255.0, 1);
        }

        var hex = text.StartsWith('#') ? text[1..] : text;
        if (hex.Length is 3 or 4 or 6 or 8 && hex.All(char.IsAsciiHexDigit))
        {
            return ParseHex(hex);
        }

        var open = text.IndexOf('(');
        if (open <= 0 || !text.EndsWith(')'))
        {
            return null;
        }

        var function = text[..open].Trim();
        var values = Values(text[(open + 1)..^1], function is "cmyk" or "device-cmyk" ? 4 : 3);
        if (values is null)
        {
            return null;
        }

        var (tokens, alphaToken) = values.Value;
        double? alpha = alphaToken is null ? 1 : Fraction(alphaToken);
        if (alpha is null)
        {
            return null;
        }

        return function switch
        {
            "rgb" or "rgba" => Rgb(tokens, alpha.Value),
            "hsl" or "hsla" => Hsl(tokens, alpha.Value),
            "hwb" => Hwb(tokens, alpha.Value),
            "cmyk" => Cmyk(tokens, percentByDefault: true),
            "device-cmyk" => Cmyk(tokens, percentByDefault: false),
            _ => null
        };
    }

    private static Rgba ParseHex(string hex)
    {
        // #rgb and #rgba repeat each digit.
        if (hex.Length <= 4)
        {
            hex = string.Concat(hex.Select(it => new string(it, 2)));
        }

        int Channel(int index) => int.Parse(hex.AsSpan(index, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);

        return new Rgba(Channel(0) / 255.0, Channel(2) / 255.0, Channel(4) / 255.0, hex.Length == 8 ? Channel(6) / 255.0 : 1);
    }

    // The values between the parentheses, separated by commas or spaces, and the alpha: after a slash, or as a
    // fourth value in the comma form (rgba(1, 2, 3, 0.5)).
    private static (string[] Tokens, string? Alpha)? Values(string body, int count)
    {
        string? alpha = null;
        var slash = body.Split('/');
        if (slash.Length > 2)
        {
            return null;
        }

        if (slash.Length == 2)
        {
            alpha = slash[1].Trim();
            if (alpha.Length == 0)
            {
                return null;
            }
        }

        var tokens = slash[0].Split([',', ' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
        if (alpha is null && tokens.Length == count + 1)
        {
            alpha = tokens[^1];
            tokens = tokens[..^1];
        }

        return tokens.Length == count ? (tokens, alpha) : null;
    }

    private static Rgba? Rgb(string[] tokens, double alpha)
    {
        var channels = tokens
            .Select(token => token.EndsWith('%') ? Number(token[..^1]) / 100 : Number(token) / 255)
            .ToArray();

        return channels.Any(it => it is null)
            ? null
            : new Rgba(Clamp(channels[0]!.Value), Clamp(channels[1]!.Value), Clamp(channels[2]!.Value), alpha);
    }

    private static Rgba? Hsl(string[] tokens, double alpha)
    {
        var hue = Hue(tokens[0]);
        var saturation = Fraction(tokens[1], percentByDefault: true);
        var lightness = Fraction(tokens[2], percentByDefault: true);
        if (hue is null || saturation is null || lightness is null)
        {
            return null;
        }

        var (r, g, b) = HslToRgb(hue.Value, saturation.Value, lightness.Value);
        return new Rgba(r, g, b, alpha);
    }

    private static Rgba? Hwb(string[] tokens, double alpha)
    {
        var hue = Hue(tokens[0]);
        var whiteness = Fraction(tokens[1], percentByDefault: true);
        var blackness = Fraction(tokens[2], percentByDefault: true);
        if (hue is null || whiteness is null || blackness is null)
        {
            return null;
        }

        var (w, bk) = (whiteness.Value, blackness.Value);
        if (w + bk >= 1)
        {
            var gray = w / (w + bk);
            return new Rgba(gray, gray, gray, alpha);
        }

        var (r, g, b) = HslToRgb(hue.Value, 1, 0.5);
        return new Rgba(r * (1 - w - bk) + w, g * (1 - w - bk) + w, b * (1 - w - bk) + w, alpha);
    }

    private static Rgba? Cmyk(string[] tokens, bool percentByDefault)
    {
        var values = tokens.Select(token => Fraction(token, percentByDefault)).ToArray();
        if (values.Any(it => it is null))
        {
            return null;
        }

        var (c, m, y, k) = (values[0]!.Value, values[1]!.Value, values[2]!.Value, values[3]!.Value);
        return new Rgba((1 - c) * (1 - k), (1 - m) * (1 - k), (1 - y) * (1 - k), 1);
    }

    // A percentage, or a number that is a fraction from 0 to 1 - or a percentage without its sign, where the
    // notation reads it so (hsl(210, 100, 50), cmyk(88, 44, 0, 0)). "none" is 0. Clamped to 0..1.
    private static double? Fraction(string token, bool percentByDefault = false)
    {
        if (token == "none")
        {
            return 0;
        }

        var percent = token.EndsWith('%');
        var number = Number(percent ? token[..^1] : token);

        return number is null ? null : Clamp(percent || percentByDefault ? number.Value / 100 : number.Value);
    }

    // Degrees from 0 up to 360.
    private static double? Hue(string token)
    {
        if (token == "none")
        {
            return 0;
        }

        (string Unit, double Degrees)[] units = [("deg", 1), ("grad", 0.9), ("rad", 180 / Math.PI), ("turn", 360)];
        foreach (var (unit, degrees) in units)
        {
            if (token.EndsWith(unit, StringComparison.Ordinal))
            {
                return Number(token[..^unit.Length]) is { } value ? Wrap(value * degrees) : null;
            }
        }

        return Number(token) is { } plain ? Wrap(plain) : null;

        static double Wrap(double degrees) => (degrees % 360 + 360) % 360;
    }

    private static double? Number(string token)
        => double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) && double.IsFinite(value)
            ? value
            : null;

    private static double Clamp(double value) => Math.Clamp(value, 0, 1);

    private static (double R, double G, double B) HslToRgb(double hue, double saturation, double lightness)
    {
        var chroma = (1 - Math.Abs(2 * lightness - 1)) * saturation;
        var x = chroma * (1 - Math.Abs(hue / 60 % 2 - 1));
        var m = lightness - chroma / 2;

        var (r, g, b) = (int)(hue / 60) switch
        {
            0 => (chroma, x, 0.0),
            1 => (x, chroma, 0.0),
            2 => (0.0, chroma, x),
            3 => (0.0, x, chroma),
            4 => (x, 0.0, chroma),
            _ => (chroma, 0.0, x)
        };

        return (r + m, g + m, b + m);
    }

    private static Result Describe(Rgba color)
    {
        var red = Byte(color.R);
        var green = Byte(color.G);
        var blue = Byte(color.B);
        var alpha = Math.Round(Math.Clamp(color.A, 0, 1), 3, MidpointRounding.AwayFromZero);
        var opaque = alpha >= 1;

        // Every notation is worked out from the 8-bit channels, so all of them agree with the hex.
        var (r, g, b) = (red / 255.0, green / 255.0, blue / 255.0);
        var max = Math.Max(r, Math.Max(g, b));
        var min = Math.Min(r, Math.Min(g, b));
        var delta = max - min;

        var hue = delta == 0 ? 0
            : max == r ? 60 * (((g - b) / delta % 6 + 6) % 6)
            : max == g ? 60 * ((b - r) / delta + 2)
            : 60 * ((r - g) / delta + 4);
        var lightness = (max + min) / 2;
        var saturation = delta == 0 ? 0 : delta / (1 - Math.Abs(2 * lightness - 1));
        var black = 1 - max;

        var a = alpha.ToString("0.###", CultureInfo.InvariantCulture);
        var rgbValue = (red << 16) | (green << 8) | blue;

        return new Result(
            Hex: Invariant($"#{red:x2}{green:x2}{blue:x2}") + (opaque ? string.Empty : Invariant($"{Byte(alpha):x2}")),
            Rgb: opaque ? Invariant($"rgb({red}, {green}, {blue})") : Invariant($"rgba({red}, {green}, {blue}, {a})"),
            Hsl: opaque
                ? $"hsl({Round(hue)}, {Round(saturation * 100)}%, {Round(lightness * 100)}%)"
                : $"hsla({Round(hue)}, {Round(saturation * 100)}%, {Round(lightness * 100)}%, {a})",
            Hwb: $"hwb({Round(hue)} {Round(min * 100)}% {Round(black * 100)}%" + (opaque ? ")" : $" / {a})"),
            Cmyk: black >= 1
                ? "cmyk(0%, 0%, 0%, 100%)"
                : $"cmyk({Round((1 - r - black) / (1 - black) * 100)}%, {Round((1 - g - black) / (1 - black) * 100)}%, {Round((1 - b - black) / (1 - black) * 100)}%, {Round(black * 100)}%)",
            Name: opaque
                ? CssColorNames.ByValue.GetValueOrDefault(rgbValue)
                : alpha == 0 && rgbValue == 0 ? "transparent" : null,
            Red: red,
            Green: green,
            Blue: blue,
            Alpha: alpha);

        static int Byte(double fraction) => (int)Math.Round(Math.Clamp(fraction, 0, 1) * 255, MidpointRounding.AwayFromZero);

        // One decimal place at most, and never "-0".
        static string Round(double value)
        {
            var rounded = Math.Round(value, 1, MidpointRounding.AwayFromZero);
            return (rounded == 0 ? 0 : rounded).ToString("0.#", CultureInfo.InvariantCulture);
        }

        static string Invariant(FormattableString text) => text.ToString(CultureInfo.InvariantCulture);
    }

    public sealed record Args(
        [property: PipeInput,
                   Description("A color: hex (#1e90ff), rgb(), hsl(), hwb(), cmyk() or device-cmyk(), a CSS color name, or transparent")]
        string? Color
    );

    public sealed record Result(
        [property: Description("#rrggbb, or #rrggbbaa when the color has an alpha below 1")]
        string Hex,
        [property: DisplayName("RGB"),
                   Description("rgb(r, g, b), or rgba(r, g, b, alpha), each channel from 0 to 255")]
        string Rgb,
        [property: DisplayName("HSL"),
                   Description("hsl(hue, saturation%, lightness%), or hsla with an alpha")]
        string Hsl,
        [property: DisplayName("HWB"),
                   Description("hwb(hue whiteness% blackness%), with / alpha when below 1")]
        string Hwb,
        [property: DisplayName("CMYK"),
                   Description("cmyk(c%, m%, y%, k%) by the plain formula, without a color profile or alpha")]
        string Cmyk,
        [property: DisplayName("CSS Name"),
                   Description("The CSS name of exactly this color, or transparent; none when it has no name")]
        string? Name,
        [property: Description("Red channel, 0 to 255")]
        int Red,
        [property: Description("Green channel, 0 to 255")]
        int Green,
        [property: Description("Blue channel, 0 to 255")]
        int Blue,
        [property: Description("Opacity from 0 (transparent) to 1 (opaque), rounded to three decimal places")]
        double Alpha
    ) : ToolResult
    {
        public Result() : this(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, null, 0, 0, 0, 0)
        {
        }
    }
}

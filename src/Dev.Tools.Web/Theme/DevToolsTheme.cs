using MudBlazor;

namespace Dev.Tools.Web.Theme;

/// <summary>
/// The MudBlazor side of the design tokens in <c>wwwroot/css/tokens.css</c>: MudBlazor derives shades
/// and hover colours from its palette in C#, so the values it needs are repeated here.
/// </summary>
public static class DevToolsTheme
{
    // Space Grotesk has no Cyrillic; Onest is self-hosted for those glyphs (see wwwroot/css/fonts.css),
    // and Golos Text is the designer's further fallback for anyone who happens to have it.
    private static readonly string[] Sans = ["Space Grotesk", "Onest", "Golos Text", "Helvetica", "Arial", "sans-serif"];
    private static readonly string[] Mono = ["JetBrains Mono", "ui-monospace", "SFMono-Regular", "Menlo", "Consolas", "monospace"];

    public static MudTheme Theme { get; } = new()
    {
        PaletteLight = new PaletteLight
        {
            // Light needs the accent split in two: a readable lime for text, a bright one for fills.
            Primary = "#55670a",
            PrimaryContrastText = "#f6f7f1",
            PrimaryLighten = "#c5e825",
            PrimaryDarken = "#3f4d05",
            Secondary = "#a6b845",
            SecondaryContrastText = "#15180f",
            Tertiary = "#3f4636",
            TertiaryContrastText = "#f6f7f1",
            Dark = "#dce1cf",
            DarkContrastText = "#15180f",
            Info = "#2f6f8f",
            InfoContrastText = "#ffffff",
            Success = "#4f7a12",
            SuccessContrastText = "#ffffff",
            Warning = "#8a5a00",
            WarningContrastText = "#ffffff",
            Error = "#a3450d",
            ErrorContrastText = "#ffffff",

            Background = "#f6f7f1",
            BackgroundGray = "#f1f3e8",
            Surface = "#ffffff",
            AppbarBackground = "#f6f7f1",
            AppbarText = "#15180f",
            DrawerBackground = "#f6f7f1",
            DrawerText = "#3f4636",
            DrawerIcon = "#5d6550",

            TextPrimary = "#15180f",
            TextSecondary = "#5a6149",
            TextDisabled = "#6b7360",
            ActionDefault = "#3f4636",
            ActionDisabled = "#8d9581",
            ActionDisabledBackground = "#e7ebdc",

            LinesDefault = "#dce1cf",
            LinesInputs = "#d3d9c3",
            Divider = "#dce1cf",
            DividerLight = "#e7ebdc",
            TableLines = "#e7ebdc",
            TableStriped = "#f1f3e8",
            TableHover = "#f2f6e2",
            Skeleton = "#eaf2cd",
        },

        PaletteDark = new PaletteDark
        {
            Primary = "#beee0c",
            PrimaryContrastText = "#0c0d0b",
            PrimaryLighten = "#d4ff2b",
            Secondary = "#7b8a0c",
            SecondaryContrastText = "#0c0d0b",
            Tertiary = "#b5bca8",
            TertiaryContrastText = "#0c0d0b",
            Dark = "#22251e",
            DarkContrastText = "#b5bca8",
            Info = "#8aa4b8",
            InfoContrastText = "#0c0d0b",
            Success = "#9cc93a",
            SuccessContrastText = "#0c0d0b",
            Warning = "#e0b85a",
            WarningContrastText = "#0c0d0b",
            Error = "#e08a5a",
            ErrorContrastText = "#0c0d0b",

            Background = "#0c0d0b",
            BackgroundGray = "#14160f",
            Surface = "#111309",
            AppbarBackground = "#0c0d0b",
            AppbarText = "#e8ebe3",
            DrawerBackground = "#0c0d0b",
            DrawerText = "#b5bca8",
            DrawerIcon = "#8d947f",

            TextPrimary = "#e8ebe3",
            TextSecondary = "#9aa189",
            TextDisabled = "#6f7563",
            ActionDefault = "#b5bca8",
            ActionDisabled = "#4d5343",
            ActionDisabledBackground = "#1a1d15",

            LinesDefault = "#22251e",
            LinesInputs = "#2a2e23",
            Divider = "#22251e",
            DividerLight = "#1a1d15",
            TableLines = "#1a1d15",
            TableStriped = "#111309",
            TableHover = "#14170d",
            Skeleton = "#16180f",
        },

        Typography = new Typography
        {
            Default = new DefaultTypography { FontFamily = Sans, FontSize = ".875rem", FontWeight = "400", LineHeight = "1.55" },
            H1 = new H1Typography { FontFamily = Sans, FontSize = "clamp(2.125rem, 4.4vw, 3.5rem)", FontWeight = "500", LineHeight = "1.04", LetterSpacing = "-.02em" },
            H2 = new H2Typography { FontFamily = Sans, FontSize = "2.25rem", FontWeight = "500", LineHeight = "1.1", LetterSpacing = "-.02em" },
            H3 = new H3Typography { FontFamily = Sans, FontSize = "2rem", FontWeight = "500", LineHeight = "1.15", LetterSpacing = "-.01em" },
            H4 = new H4Typography { FontFamily = Sans, FontSize = "1.75rem", FontWeight = "500", LineHeight = "1.2", LetterSpacing = "-.01em" },
            H5 = new H5Typography { FontFamily = Sans, FontSize = "1.375rem", FontWeight = "500", LineHeight = "1.3", LetterSpacing = "normal" },
            H6 = new H6Typography { FontFamily = Sans, FontSize = "1.0625rem", FontWeight = "500", LineHeight = "1.4", LetterSpacing = ".01em" },
            Subtitle1 = new Subtitle1Typography { FontFamily = Sans, FontSize = ".9375rem", FontWeight = "500", LineHeight = "1.5", LetterSpacing = "normal" },
            Subtitle2 = new Subtitle2Typography { FontFamily = Sans, FontSize = ".875rem", FontWeight = "500", LineHeight = "1.5", LetterSpacing = "normal" },
            Body1 = new Body1Typography { FontFamily = Sans, FontSize = ".875rem", FontWeight = "400", LineHeight = "1.6", LetterSpacing = "normal" },
            Body2 = new Body2Typography { FontFamily = Sans, FontSize = ".8125rem", FontWeight = "400", LineHeight = "1.55", LetterSpacing = "normal" },
            Button = new ButtonTypography { FontFamily = Sans, FontSize = ".84375rem", FontWeight = "500", LineHeight = "1.5", LetterSpacing = "normal", TextTransform = "none" },
            Caption = new CaptionTypography { FontFamily = Sans, FontSize = ".6875rem", FontWeight = "400", LineHeight = "1.5", LetterSpacing = "normal" },
            // The mono microlabel (OPTIONS, INPUT · JSON) of the design.
            Overline = new OverlineTypography { FontFamily = Mono, FontSize = ".65625rem", FontWeight = "400", LineHeight = "1.5", LetterSpacing = ".14em", TextTransform = "uppercase" },
        },

        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "8px",
        },

        // No shadows anywhere: depth comes from 1px borders and surface steps.
        Shadows = new Shadow
        {
            Elevation = Enumerable.Repeat("none", new Shadow().Elevation.Length).ToArray(),
        },
    };
}

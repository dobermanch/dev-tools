using Dev.Tools.Tools;

namespace Dev.Tools.Tests.Tools;

public class ColorConverterToolTests
{
    private static Task<ColorConverterTool.Result> RunAsync(string? color)
        => new ColorConverterTool().RunAsync(new ColorConverterTool.Args(color), CancellationToken.None);

    // Every notation on one line, so a failing test shows the whole result.
    private static string Describe(ColorConverterTool.Result result)
        => $"{result.Hex} | {result.Rgb} | {result.Hsl} | {result.Hwb} | {result.Cmyk} | {result.Name ?? "-"}";

    private const string DodgerBlue =
        "#1e90ff | rgb(30, 144, 255) | hsl(209.6, 100%, 55.9%) | hwb(209.6 11.8% 0%) | cmyk(88.2%, 43.5%, 0%, 0%) | dodgerblue";

    [Test]
    [Arguments("#1e90ff")]
    [Arguments("1E90FF")]
    [Arguments("#1e90ffff")]
    [Arguments("DodgerBlue")]
    [Arguments("rgb(30, 144, 255)")]
    [Arguments("rgb(30 144 255)")]
    [Arguments("rgba(30,144,255,1)")]
    [Arguments("rgb(30 144 255 / 100%)")]
    [Arguments("rgb(11.76% 56.47% 100%)")]
    [Arguments("hsl(209.6, 100%, 55.9%)")]
    [Arguments("hsl(209.6deg 100 55.9)")]
    [Arguments("hsl(3.6582rad 100% 55.9%)")]
    [Arguments("hwb(209.6 11.8% 0%)")]
    [Arguments("cmyk(88.2%, 43.5%, 0%, 0%)")]
    [Arguments("cmyk(88.2, 43.5, 0, 0)")]
    [Arguments("device-cmyk(0.882 0.435 0 0)")]
    [Arguments("  rgb( 30 , 144 , 255 )  ")]
    public async Task ShouldReadEveryNotationOfTheSameColor(string color)
    {
        var result = await RunAsync(color);

        await Assert.That(result.HasErrors).IsFalse();
        await Assert.That(Describe(result)).IsEqualTo(DodgerBlue);
        await Assert.That((result.Red, result.Green, result.Blue, result.Alpha)).IsEqualTo((30, 144, 255, 1.0));
    }

    [Test]
    [Arguments("#1e90ff80", "#1e90ff80 | rgba(30, 144, 255, 0.502) | hsla(209.6, 100%, 55.9%, 0.502) | hwb(209.6 11.8% 0% / 0.502) | cmyk(88.2%, 43.5%, 0%, 0%) | -")]
    [Arguments("rgb(30 144 255 / 50%)", "#1e90ff80 | rgba(30, 144, 255, 0.5) | hsla(209.6, 100%, 55.9%, 0.5) | hwb(209.6 11.8% 0% / 0.5) | cmyk(88.2%, 43.5%, 0%, 0%) | -")]
    [Arguments("transparent", "#00000000 | rgba(0, 0, 0, 0) | hsla(0, 0%, 0%, 0) | hwb(0 0% 100% / 0) | cmyk(0%, 0%, 0%, 100%) | transparent")]
    public async Task ShouldKeepTheAlpha(string color, string expected)
    {
        var result = await RunAsync(color);

        await Assert.That(Describe(result)).IsEqualTo(expected);
    }

    [Test]
    [Arguments("#abc", "#aabbcc")]
    [Arguments("#abcd", "#aabbccdd")]
    [Arguments("rgb(300, -20, 128)", "#ff0080")]
    [Arguments("hsl(-120, 100%, 50%)", "#0000ff")]
    [Arguments("hsl(480, 100%, 50%)", "#00ff00")]
    [Arguments("hsl(0.5turn 100% 50%)", "#00ffff")]
    [Arguments("hsl(200grad 100% 50%)", "#00ffff")]
    [Arguments("hsl(none 0% 50%)", "#808080")]
    [Arguments("hwb(0 60% 60%)", "#808080")]
    [Arguments("rgba(0, 0, 255, 2)", "#0000ff")]
    public async Task ShouldReadShortHexWrapHuesAndClampValues(string color, string hex)
    {
        var result = await RunAsync(color);

        await Assert.That(result.Hex).IsEqualTo(hex);
    }

    [Test]
    [Arguments("#00ffff", "aqua")]
    [Arguments("#808080", "gray")]
    [Arguments("#663399", "rebeccapurple")]
    [Arguments("#1e90fe", null)]
    public async Task ShouldNameOnlyAnExactCssColor(string color, string? name)
    {
        var result = await RunAsync(color);

        await Assert.That(result.Name).IsEqualTo(name);
    }

    [Test]
    [Arguments("black", "#000000 | rgb(0, 0, 0) | hsl(0, 0%, 0%) | hwb(0 0% 100%) | cmyk(0%, 0%, 0%, 100%) | black")]
    [Arguments("white", "#ffffff | rgb(255, 255, 255) | hsl(0, 0%, 100%) | hwb(0 100% 0%) | cmyk(0%, 0%, 0%, 0%) | white")]
    [Arguments("red", "#ff0000 | rgb(255, 0, 0) | hsl(0, 100%, 50%) | hwb(0 0% 0%) | cmyk(0%, 100%, 100%, 0%) | red")]
    [Arguments("#ff00ff", "#ff00ff | rgb(255, 0, 255) | hsl(300, 100%, 50%) | hwb(300 0% 0%) | cmyk(0%, 100%, 0%, 0%) | fuchsia")]
    public async Task ShouldConvertTheEdgesOfTheColorSpace(string color, string expected)
    {
        var result = await RunAsync(color);

        await Assert.That(Describe(result)).IsEqualTo(expected);
    }

    [Test]
    [Arguments(null)]
    [Arguments("")]
    [Arguments("   ")]
    public async Task ShouldReturnError_WhenColorIsEmpty(string? color)
    {
        var result = await RunAsync(color);

        await Assert.That(result.Errors[0].Code).IsEqualTo(ErrorCode.TextEmpty);
    }

    [Test]
    [Arguments("notacolor")]
    [Arguments("#12")]
    [Arguments("#12345")]
    [Arguments("#ggg")]
    [Arguments("rgb(1, 2)")]
    [Arguments("rgb(1, 2, 3, 4, 5)")]
    [Arguments("rgb(1 2 3 / )")]
    [Arguments("rgb(1 2 3 / 4 / 5)")]
    [Arguments("rgb(1, 2, 3")]
    [Arguments("hsl(abc, 1%, 2%)")]
    [Arguments("hsl(10px, 1%, 2%)")]
    [Arguments("foo(1, 2, 3)")]
    [Arguments("rgb(NaN, 2, 3)")]
    public async Task ShouldReturnError_WhenColorIsNotValid(string color)
    {
        var result = await RunAsync(color);

        await Assert.That(result.Errors[0].Code).IsEqualTo(ErrorCode.InputNotValid);
        await Assert.That(result.Errors[0].Target).IsEqualTo(nameof(ColorConverterTool.Args.Color));
    }
}

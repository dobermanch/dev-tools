using Dev.Tools.Tools;
using static Dev.Tools.Tools.QrCodeTool;

namespace Dev.Tools.Tests.Tools;

public class QrCodeToolTests
{
    private static Task<Result> RunAsync(Args args)
        => new QrCodeTool().RunAsync(args, CancellationToken.None);

    private static Args Wifi(string ssid, string? password = null, WifiSecurity security = WifiSecurity.Wpa, bool hidden = false)
        => new(ssid, QrContent.WiFi, Password: password, Security: security, HiddenNetwork: hidden);

    [Test]
    public async Task ShouldEncodeTextAsAnSvgDocument()
    {
        var result = await RunAsync(new Args("hello"));

        await Assert.That(result.HasErrors).IsFalse();
        await Assert.That(result.Svg).StartsWith("<?xml");
        await Assert.That(result.Svg).Contains("<svg");
        // 21 modules and a border of 4 on each side.
        await Assert.That(result.Svg).Contains("viewBox=\"0 0 29 29\"");
        await Assert.That(result.Svg).Contains("#000000");
        await Assert.That(result.Svg).Contains("#ffffff");
        await Assert.That(result.Payload).IsEqualTo("hello");
        await Assert.That(result.Version).IsEqualTo(1);
        await Assert.That(result.Size).IsEqualTo(21);
    }

    [Test]
    public async Task ShouldRaiseTheErrorCorrection_WhenItFitsInTheSameSize()
    {
        var result = await RunAsync(new Args("hello", ErrorCorrection: QrErrorCorrection.Medium));

        await Assert.That(result.ErrorCorrection).IsEqualTo(QrErrorCorrection.High);
    }

    [Test]
    public async Task ShouldGrowTheCode_WhenMoreErrorCorrectionIsAskedFor()
    {
        var text = new string('x', 200);

        var low = await RunAsync(new Args(text, ErrorCorrection: QrErrorCorrection.Low));
        var high = await RunAsync(new Args(text, ErrorCorrection: QrErrorCorrection.High));

        await Assert.That(high.Version).IsGreaterThan(low.Version);
    }

    [Test]
    public async Task ShouldGiveTheSameCodeForTheSameInput()
    {
        var first = await RunAsync(new Args("https://example.com"));
        var second = await RunAsync(new Args("https://example.com"));

        await Assert.That(second.Svg).IsEqualTo(first.Svg);
    }

    [Test]
    public async Task ShouldEncodeTextBeyondAscii()
    {
        var result = await RunAsync(new Args("Привіт, світе 👋"));

        await Assert.That(result.HasErrors).IsFalse();
        await Assert.That(result.Payload).IsEqualTo("Привіт, світе 👋");
    }

    [Test]
    [Arguments(0, "0 0 21 21")]
    [Arguments(-3, "0 0 21 21")]
    [Arguments(50, "0 0 61 61")]
    public async Task ShouldHoldTheBorderInRange(int border, string viewBox)
    {
        var result = await RunAsync(new Args("hello", Border: border));

        await Assert.That(result.Svg).Contains($"viewBox=\"{viewBox}\"");
    }

    [Test]
    public async Task ShouldUseTheColorsGiven()
    {
        var result = await RunAsync(new Args("hello", DarkColor: "#1E90FF", LightColor: "#FFF"));

        await Assert.That(result.Svg).Contains("#1e90ff");
        await Assert.That(result.Svg).Contains("\"#fff\"");
    }

    [Test]
    [Arguments("red")]
    [Arguments("#12345")]
    [Arguments("#ggg")]
    [Arguments("")]
    [Arguments("#000\" onload=\"alert(1)")]
    public async Task ShouldRefuseAColorThatIsNotHex(string color)
    {
        var dark = await RunAsync(new Args("hello", DarkColor: color));
        var light = await RunAsync(new Args("hello", LightColor: color));

        await Assert.That(dark.Errors.Select(it => $"{it.Code} {it.Target}")).IsEquivalentTo(["InputNotValid DarkColor"]);
        await Assert.That(light.Errors.Select(it => $"{it.Code} {it.Target}")).IsEquivalentTo(["InputNotValid LightColor"]);
    }

    [Test]
    [Arguments("Home", "secret", WifiSecurity.Wpa, false, "WIFI:T:WPA;S:Home;P:secret;;")]
    [Arguments("Home", "secret", WifiSecurity.Wep, true, "WIFI:T:WEP;S:Home;P:secret;H:true;;")]
    [Arguments("Guest", null, WifiSecurity.None, false, "WIFI:T:nopass;S:Guest;;")]
    [Arguments("Guest", "ignored", WifiSecurity.None, false, "WIFI:T:nopass;S:Guest;;")]
    [Arguments("My;Net", "p@ss:wo,rd\"\\", WifiSecurity.Wpa, false, "WIFI:T:WPA;S:My\\;Net;P:p@ss\\:wo\\,rd\\\"\\\\;;")]
    [Arguments("Home", "12345678", WifiSecurity.Wpa, false, "WIFI:T:WPA;S:Home;P:12345678;;")]
    public async Task ShouldBuildTheWifiPayload(string ssid, string? password, WifiSecurity security, bool hidden, string expected)
    {
        var result = await RunAsync(Wifi(ssid, password, security, hidden));

        await Assert.That(result.HasErrors).IsFalse();
        await Assert.That(result.Payload).IsEqualTo(expected);
    }

    [Test]
    [Arguments(WifiSecurity.Wpa)]
    [Arguments(WifiSecurity.Wep)]
    public async Task ShouldReturnError_WhenASecuredNetworkHasNoPassword(WifiSecurity security)
    {
        var result = await RunAsync(Wifi("Home", security: security));

        await Assert.That(result.Errors.Select(it => $"{it.Code} {it.Target}")).IsEquivalentTo(["TextEmpty Password"]);
    }

    [Test]
    public async Task ShouldReturnDataTooLong_WhenTheTextDoesNotFit()
    {
        var tooLong = await RunAsync(new Args(new string('x', 3000)));
        var largest = await RunAsync(new Args(new string('x', 2953), ErrorCorrection: QrErrorCorrection.Low));

        await Assert.That(tooLong.Errors.Select(it => $"{it.Code} {it.Target}")).IsEquivalentTo(["DataTooLong Text"]);
        await Assert.That(largest.HasErrors).IsFalse();
        await Assert.That(largest.Version).IsEqualTo(40);
    }

    [Test]
    public async Task ShouldReportEveryInvalidArgument()
    {
        var result = await RunAsync(new Args("", DarkColor: "black"));

        await Assert.That(result.Errors.Select(it => $"{it.Code} {it.Target}"))
            .IsEquivalentTo(["TextEmpty Text", "InputNotValid DarkColor"]);
    }
}

using System.Xml.Linq;

namespace Dev.Tools.Tests;

/// <summary>
/// The Web UI's own labels, in src/Dev.Tools.Web/Locals. The neutral file is what any culture without a
/// translation falls back to, so it has to be English. It held ":Input:"-style placeholders and was 36
/// keys behind en-US, so an unresolved culture showed placeholders and, for the newer pages, nothing.
/// </summary>
public class WebResourcesTests
{
    private static readonly string LocalsDirectory = Path.Combine(RepositoryRoot(), "src", "Dev.Tools.Web", "Locals");

    [Test]
    [Arguments("Resources.en-US.resx")]
    [Arguments("Resources.uk-UA.resx")]
    public async Task EveryCultureShouldCarryTheSameKeysAsTheNeutralFile(string cultureFile)
    {
        var neutral = Read("Resources.resx");
        var culture = Read(cultureFile);

        await Assert.That(culture.Keys.Except(neutral.Keys)).IsEmpty();
        await Assert.That(neutral.Keys.Except(culture.Keys)).IsEmpty();
    }

    [Test]
    public async Task NeutralFileShouldHoldTheEnglishText()
    {
        var neutral = Read("Resources.resx");
        var english = Read("Resources.en-US.resx");

        var different = neutral.Where(it => english.GetValueOrDefault(it.Key) != it.Value).Select(it => it.Key);

        await Assert.That(different).IsEmpty();
    }

    private static Dictionary<string, string> Read(string fileName)
        => XDocument
            .Load(Path.Combine(LocalsDirectory, fileName))
            .Root!
            .Elements("data")
            .ToDictionary(it => (string)it.Attribute("name")!, it => (string?)it.Element("value") ?? string.Empty);

    private static string RepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Dev.Tools.slnx")))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException("Dev.Tools.slnx was not found above " + AppContext.BaseDirectory);
    }
}

using Dev.Tools.Cryptography;
using Dev.Tools.Tools;

namespace Dev.Tools.Tests;

/// <summary>
/// A cancelled run has to end in <see cref="OperationCanceledException"/>. Not a result carrying
/// <c>ErrorCode.Unknown</c>, which is what the catch-all in ToolBase turned it into, and not a normal
/// result either, which is what ignoring the token produces.
/// </summary>
public class CancellationTests
{
    private static readonly Dictionary<string, Func<CancellationToken, Task>> Runs = new()
    {
        // A tool with no reason to override the token-aware Execute, checked by ToolBase alone.
        ["base64-encoder"] = token => new Base64EncoderTool().RunAsync(new Base64EncoderTool.Args("hello"), token),
        ["passphrase-generator"] = token => new PassphraseGeneratorTool().RunAsync(new PassphraseGeneratorTool.Args(), token),
        ["token-generator"] = token => new TokenGeneratorTool().RunAsync(new TokenGeneratorTool.Args(), token),
        ["ulid-generator"] = token => new UlidGeneratorTool().RunAsync(new UlidGeneratorTool.Args(), token),
        ["uuid-generator"] = token => new UuidGeneratorTool(new Md5Hash()).RunAsync(new UuidGeneratorTool.Args(), token),
    };

    [Test]
    public async Task AnAlreadyCancelledRunShouldDoNoWork()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        var notCancelled = new List<string>();
        foreach (var (name, run) in Runs)
        {
            try
            {
                await run(cancellation.Token);
                notCancelled.Add(name);
            }
            catch (OperationCanceledException)
            {
            }
        }

        await Assert.That(notCancelled).IsEmpty();
    }

    [Test]
    public async Task ALongGenerationShouldStopPartWay()
    {
        // The token is live when the run starts, so ToolBase's up-front check lets it through and only
        // the token reaching Parallel.For can stop it. Uncancelled, this is tens of millions of random
        // draws; sized to stay within memory if cancellation ever regresses and it runs to the end.
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(25));
        var args = new TokenGeneratorTool.Args(TokenLength: 4096, TokenCount: 20_000);

        await Assert.That(async () => await new TokenGeneratorTool().RunAsync(args, cancellation.Token))
            .Throws<OperationCanceledException>();
    }
}

using Dev.Tools.Cryptography;

namespace Dev.Tools.Tests.Helpers;

/// <summary>
/// Replays scripted numbers and fills every byte buffer with one value, so a test can say exactly what
/// a generated value should be. Numbers repeat from the start once exhausted.
/// </summary>
/// <remarks>
/// Draws are taken in order but not ordered across threads, so a script longer than one value is only
/// meaningful where the tool draws from a single thread - a count of one in the Parallel.For generators.
/// </remarks>
internal sealed class ScriptedRandomSource(byte fill, params int[] numbers) : IRandomSource
{
    private int _drawn = -1;

    public int Next(int maxExclusive)
        => numbers.Length == 0 ? 0 : numbers[Interlocked.Increment(ref _drawn) % numbers.Length] % maxExclusive;

    public void Fill(Span<byte> buffer) => buffer.Fill(fill);
}

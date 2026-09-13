using System.Security.Cryptography;

namespace Dev.Tools.Cryptography;

/// <summary>
/// Where the tools that generate values get their randomness. A seam, like <see cref="IMd5Hash"/>:
/// the default is cryptographically secure, and a test can substitute a scripted source to assert
/// the exact value a tool produces instead of only its shape.
/// </summary>
/// <remarks>
/// Implementations must be thread-safe - several tools draw from <c>Parallel.For</c> loops.
/// </remarks>
public interface IRandomSource
{
    /// <summary>A uniformly distributed integer from 0 up to, but not including, <paramref name="maxExclusive"/>.</summary>
    int Next(int maxExclusive);

    /// <summary>Fills <paramref name="buffer"/> with random bytes.</summary>
    void Fill(Span<byte> buffer);
}

/// <summary>
/// <see cref="RandomNumberGenerator"/>: thread-safe, and suitable for passwords, tokens, nonces and IVs.
/// </summary>
public sealed class CryptographicRandomSource : IRandomSource
{
    public static CryptographicRandomSource Instance { get; } = new();

    public int Next(int maxExclusive) => RandomNumberGenerator.GetInt32(maxExclusive);

    public void Fill(Span<byte> buffer) => RandomNumberGenerator.Fill(buffer);
}

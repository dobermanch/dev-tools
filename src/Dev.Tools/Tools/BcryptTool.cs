using System.Globalization;
using System.Text.RegularExpressions;
using Dev.Tools.Cryptography;
using BCryptNet = BCrypt.Net.BCrypt;

namespace Dev.Tools.Tools;

[ToolDefinition(
    Name = "bcrypt",
    Title = "Bcrypt Hash",
    Description =
        """
        Hashes a password with bcrypt, or checks a password against a bcrypt hash.

        A hash is written in the modular crypt format - $2b$, the cost, then 22 characters of salt and 31 of
        hash - with a new random 128-bit salt every time, so the same password hashed twice gives two hashes
        that both verify. The cost is the base-2 logarithm of the key expansion rounds: each step up doubles
        the time. Checking reads the version, cost and salt from the hash itself, and accepts the $2a$, $2b$
        and $2y$ versions other libraries write.

        bcrypt reads no more than the first 72 bytes of a password's UTF-8 encoding and ignores the rest,
        which the result flags. The cost is held between 4 and 15, and a hash with a higher cost is refused,
        so one request cannot keep a server or a browser busy for minutes.
        """,
    Usage =
        "Use this to create a bcrypt hash for seed data or a test fixture, or to check whether a password matches a stored hash. A password is a credential: only pass one the user has given you for this purpose.",
    Aliases = [],
    Keywords = [Keyword.Hash, Keyword.Password],
    Categories = [Category.Crypto, Category.Security]
)]
public sealed partial class BcryptTool(IRandomSource? random = null)
    : ToolBase<BcryptTool.Args, BcryptTool.Result>
{
    private const int MinCost = 4;
    private const int MaxCost = 15;
    private const int MaxPasswordBytes = 72;
    private const int SaltBytes = 16;
    private const string Version = "2b";

    // bcrypt's own Base64 alphabet, and the standard one it replaces index for index.
    private const string BcryptAlphabet = "./ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
    private const string StandardAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";

    private readonly IRandomSource _random = random ?? CryptographicRandomSource.Instance;

    [GeneratedRegex(@"^\$(?<version>2[abxy])\$(?<cost>\d\d)\$(?<salt>[./A-Za-z0-9]{22})[./A-Za-z0-9]{31}$")]
    private static partial Regex HashFormat();

    protected override Result Execute(Args args)
    {
        var verifying = args.Operation == BcryptOperation.Verify;

        var errors = new List<ToolError>();
        if (string.IsNullOrEmpty(args.Password))
        {
            errors.Add(new ToolError(ErrorCode.TextEmpty, nameof(Args.Password)));
        }

        if (verifying && string.IsNullOrWhiteSpace(args.Hash))
        {
            errors.Add(new ToolError(ErrorCode.TextEmpty, nameof(Args.Hash)));
        }

        if (errors.Count > 0)
        {
            throw new ToolException(errors);
        }

        var truncated = Encoding.UTF8.GetByteCount(args.Password!) > MaxPasswordBytes;

        return verifying ? Verify(args.Password!, args.Hash!.Trim(), truncated) : Hash(args.Password!, args.Cost, truncated);
    }

    private Result Hash(string password, int cost, bool truncated)
    {
        cost = Math.Clamp(cost, MinCost, MaxCost);

        var hash = BCryptNet.HashPassword(password, string.Create(CultureInfo.InvariantCulture, $"${Version}${cost:00}${NewSalt()}"));
        var parts = HashFormat().Match(hash);

        return new Result(hash, null, Version, cost, parts.Groups["salt"].Value, truncated);
    }

    private static Result Verify(string password, string hash, bool truncated)
    {
        var parts = HashFormat().Match(hash);
        if (!parts.Success)
        {
            throw new ToolException(ErrorCode.InputNotValid, nameof(Args.Hash));
        }

        var cost = int.Parse(parts.Groups["cost"].Value, CultureInfo.InvariantCulture);
        if (cost is < MinCost or > MaxCost)
        {
            throw new ToolException(ErrorCode.InputNotValid, nameof(Args.Hash));
        }

        bool matches;
        try
        {
            matches = BCryptNet.Verify(password, hash);
        }
        catch
        {
            throw new ToolException(ErrorCode.InputNotValid, nameof(Args.Hash));
        }

        return new Result(hash, matches, parts.Groups["version"].Value, cost, parts.Groups["salt"].Value, truncated);
    }

    // 16 random bytes as bcrypt's 22-character salt: standard Base64 without the padding, in bcrypt's alphabet.
    private string NewSalt()
    {
        Span<byte> bytes = stackalloc byte[SaltBytes];
        _random.Fill(bytes);

        return string.Concat(Convert.ToBase64String(bytes)[..22].Select(it => BcryptAlphabet[StandardAlphabet.IndexOf(it)]));
    }

    public enum BcryptOperation
    {
        [Description("Hash the password with a new random salt")]
        Hash,

        [Description("Check the password against a bcrypt hash")]
        Verify
    }

    public sealed record Args(
        [property: PipeInput,
                   Description("The password to hash or to check")]
        string? Password,
        [property: Description("Whether to hash the password or to check it against Hash")]
        BcryptOperation Operation = BcryptOperation.Hash,
        [property: Description("Verify only: the bcrypt hash to check the password against, such as $2b$10$...")]
        string? Hash = null,
        [property: Description("Hash only: the cost, each step doubling the time; held between 4 and 15")]
        int Cost = 10
    );

    public sealed record Result(
        [property: Description("The new hash, or the hash the password was checked against")]
        string Hash,
        [property: Description("Verify only: whether the password matches the hash")]
        bool? Matches,
        [property: Description("The bcrypt version from the hash, such as 2b")]
        string Version,
        [property: Description("The cost from the hash")]
        int Cost,
        [property: Description("The 22-character salt from the hash")]
        string Salt,
        [property: Description("Whether the password is longer than 72 UTF-8 bytes, the most bcrypt reads; the rest does not change the hash")]
        bool PasswordTruncated
    ) : ToolResult
    {
        public Result() : this(string.Empty, null, string.Empty, 0, string.Empty, false)
        {
        }
    }
}

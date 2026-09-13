using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using Dev.Tools.Cryptography;

namespace Dev.Tools.Tools;

[ToolDefinition(
    Name = "otp",
    Title = "OTP Generator",
    Description =
        """
        Generates and checks one-time passwords - the codes authenticator apps show for two-factor sign-in -
        and builds the otpauth:// URI that adds an account to such an app.

        TOTP (RFC 6238) derives the code from the current time and changes it every period; HOTP (RFC 4226)
        derives it from a counter that moves on each time a code is used. The secret is Base32, the form a
        service shows beside its QR code; without one, a new random 160-bit secret is generated. The result
        has the code for the current step with the previous and next ones, and for TOTP the seconds the
        current code has left.

        Given a Code, the result says whether it is valid and how many steps from the current one it
        matched: a TOTP code may be up to Window periods early or late, to allow for clock drift, and an
        HOTP code up to Window counters ahead, for codes that were generated but never used.

        Nearly every service uses SHA-1, 6 digits and 30 seconds, and some authenticator apps ignore other
        values in the URI.
        """,
    Usage =
        "Use this to compute or check an authenticator code from a secret instead of working out the HMAC truncation by hand, or to create a secret and otpauth:// URI for testing two-factor sign-in. A secret is a credential: only use one the user has given you for this purpose.",
    Aliases = ["totp", "hotp"],
    Keywords = [Keyword.Otp, Keyword.Password, Keyword.Token, Keyword.Generate],
    Categories = [Category.Security, Category.Crypto]
)]
public sealed class OtpTool(IRandomSource? random = null, TimeProvider? time = null)
    : ToolBase<OtpTool.Args, OtpTool.Result>
{
    // RFC 4226 recommends 160 bits, the size of a SHA-1 output.
    private const int GeneratedSecretBytes = 20;
    private const int MinDigits = 6;
    private const int MaxDigits = 10;
    private const int MaxWindow = 10;
    private const string Base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    private readonly IRandomSource _random = random ?? CryptographicRandomSource.Instance;
    private readonly TimeProvider _time = time ?? TimeProvider.System;

    protected override Result Execute(Args args)
    {
        byte[] key;
        if (string.IsNullOrWhiteSpace(args.Secret))
        {
            key = new byte[GeneratedSecretBytes];
            _random.Fill(key);
        }
        else
        {
            key = DecodeBase32(args.Secret) ?? throw new ToolException(ErrorCode.InputNotValid, nameof(Args.Secret));
        }

        var digits = Math.Clamp(args.Digits, MinDigits, MaxDigits);
        var period = Math.Max(args.Period, 1);
        var window = Math.Clamp(args.Window, 0, MaxWindow);
        var isTotp = args.Type == OtpType.Totp;

        long counter;
        int? secondsRemaining = null;
        if (isTotp)
        {
            var seconds = _time.GetUtcNow().ToUnixTimeSeconds();
            counter = seconds / period;
            secondsRemaining = (int)(period - seconds % period);
        }
        else
        {
            counter = Math.Max(args.Counter, 0);
        }

        string CodeAt(long step) => ComputeCode(key, step, args.Algorithm, digits);

        bool? isValid = null;
        int? matchOffset = null;
        if (!string.IsNullOrWhiteSpace(args.Code))
        {
            var code = string.Concat(args.Code.Where(it => !char.IsWhiteSpace(it)));

            // The current step first, then outwards, so a code valid at two steps reports the nearer one.
            var offsets = isTotp
                ? Enumerable.Range(-window, 2 * window + 1).OrderBy(Math.Abs)
                : Enumerable.Range(0, window + 1);
            matchOffset = offsets
                .Where(offset => counter + offset >= 0)
                .Select(offset => (int?)offset)
                .FirstOrDefault(offset => SameCode(CodeAt(counter + offset!.Value), code));
            isValid = matchOffset is not null;
        }

        var secret = EncodeBase32(key);

        return new Result(
            Code: CodeAt(counter),
            PreviousCode: counter > 0 ? CodeAt(counter - 1) : null,
            NextCode: CodeAt(counter + 1),
            Counter: counter,
            SecondsRemaining: secondsRemaining,
            IsValid: isValid,
            MatchOffset: matchOffset,
            Secret: secret,
            SecretHex: Convert.ToHexStringLower(key),
            Uri: BuildUri(args, secret, digits, period, counter)
        );
    }

    // RFC 4226 section 5.3: HMAC of the counter, then dynamic truncation to a 31-bit number.
    private static string ComputeCode(byte[] key, long counter, OtpAlgorithm algorithm, int digits)
    {
        Span<byte> message = stackalloc byte[8];
        BinaryPrimitives.WriteInt64BigEndian(message, counter);

        var hash = algorithm switch
        {
            OtpAlgorithm.Sha256 => HMACSHA256.HashData(key, message),
            OtpAlgorithm.Sha512 => HMACSHA512.HashData(key, message),
            _ => HMACSHA1.HashData(key, message)
        };

        var offset = hash[^1] & 0x0F;
        var number = (hash[offset] & 0x7F) << 24 | hash[offset + 1] << 16 | hash[offset + 2] << 8 | hash[offset + 3];

        var modulus = 1L;
        for (var i = 0; i < digits; i++)
        {
            modulus *= 10;
        }

        return (number % modulus).ToString(CultureInfo.InvariantCulture).PadLeft(digits, '0');
    }

    private static bool SameCode(string expected, string actual)
        => CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(expected), Encoding.ASCII.GetBytes(actual));

    // RFC 4648 Base32, forgiving the way secrets are typed: any case, grouped with spaces or dashes, padded or not.
    private static byte[]? DecodeBase32(string text)
    {
        var bytes = new List<byte>(text.Length * 5 / 8);
        var buffer = 0;
        var bits = 0;
        var padded = false;
        foreach (var character in text)
        {
            if (char.IsWhiteSpace(character) || character == '-')
            {
                continue;
            }

            if (character == '=')
            {
                padded = true;
                continue;
            }

            var value = Base32Alphabet.IndexOf(char.ToUpperInvariant(character));
            if (value < 0 || padded)
            {
                return null;
            }

            buffer = buffer << 5 | value;
            bits += 5;
            if (bits >= 8)
            {
                bits -= 8;
                bytes.Add((byte)(buffer >> bits));
                buffer &= (1 << bits) - 1;
            }
        }

        return bytes.Count > 0 ? bytes.ToArray() : null;
    }

    private static string EncodeBase32(ReadOnlySpan<byte> bytes)
    {
        var text = new StringBuilder((bytes.Length * 8 + 4) / 5);
        var buffer = 0;
        var bits = 0;
        foreach (var value in bytes)
        {
            buffer = buffer << 8 | value;
            bits += 8;
            while (bits >= 5)
            {
                bits -= 5;
                text.Append(Base32Alphabet[buffer >> bits & 31]);
            }

            buffer &= (1 << bits) - 1;
        }

        if (bits > 0)
        {
            text.Append(Base32Alphabet[buffer << (5 - bits) & 31]);
        }

        return text.ToString();
    }

    // The Key Uri Format authenticator apps read: otpauth://type/issuer:account?secret=...&issuer=...
    private static string BuildUri(Args args, string secret, int digits, int period, long counter)
    {
        var issuer = args.Issuer?.Trim();
        var account = args.AccountName?.Trim();
        var label = string.Join(':', new[] { issuer, account }
            .Where(it => !string.IsNullOrEmpty(it))
            .Select(it => Uri.EscapeDataString(it!)));

        var uri = new StringBuilder($"otpauth://{(args.Type == OtpType.Totp ? "totp" : "hotp")}/{label}?secret={secret}");
        if (!string.IsNullOrEmpty(issuer))
        {
            uri.Append("&issuer=").Append(Uri.EscapeDataString(issuer));
        }

        uri.Append("&algorithm=").Append(args.Algorithm switch
        {
            OtpAlgorithm.Sha256 => "SHA256",
            OtpAlgorithm.Sha512 => "SHA512",
            _ => "SHA1"
        });
        uri.Append("&digits=").Append(digits.ToString(CultureInfo.InvariantCulture));
        uri.Append(args.Type == OtpType.Totp
            ? "&period=" + period.ToString(CultureInfo.InvariantCulture)
            : "&counter=" + counter.ToString(CultureInfo.InvariantCulture));

        return uri.ToString();
    }

    public enum OtpType
    {
        [Description("TOTP, time-based")]
        Totp,

        [Description("HOTP, counter-based")]
        Hotp
    }

    public enum OtpAlgorithm
    {
        [Description("SHA-1")]
        Sha1,

        [Description("SHA-256")]
        Sha256,

        [Description("SHA-512")]
        Sha512
    }

    public sealed record Args(
        [property: PipeInput,
                   Description("Shared secret in Base32 (RFC 4648); case, spaces, dashes and padding are ignored. A new random 160-bit secret is generated when omitted")]
        string? Secret,
        [property: Description("Time-based (TOTP) or counter-based (HOTP)")]
        OtpType Type = OtpType.Totp,
        [property: Description("Hash function inside the HMAC")]
        OtpAlgorithm Algorithm = OtpAlgorithm.Sha1,
        [property: Description("Length of the code; values below 6 are treated as 6, above 10 as 10")]
        int Digits = 6,
        [property: Description("TOTP only: seconds each code lasts; values below 1 are treated as 1")]
        int Period = 30,
        [property: Description("HOTP only: the counter the code is for; values below 0 are treated as 0")]
        long Counter = 0,
        [property: Description("A code to check; spaces are ignored. Without one nothing is checked")]
        string? Code = null,
        [property: Description("How far from the current step a checked code may be: for TOTP that many periods before or after, for HOTP that many counters ahead; values are held between 0 and 10")]
        int Window = 1,
        [property: Description("Service name for the otpauth:// URI, such as GitHub")]
        string? Issuer = null,
        [property: Description("Account name for the otpauth:// URI, such as an email address")]
        string? AccountName = null
    );

    public sealed record Result(
        [property: Description("The code for the current time step (TOTP) or the counter (HOTP)")]
        string Code,
        [property: Description("The code for the step before; none at counter 0")]
        string? PreviousCode,
        [property: Description("The code for the step after")]
        string NextCode,
        [property: Description("The step the code is for: for TOTP, Unix seconds divided by the period; for HOTP, the counter")]
        long Counter,
        [property: Description("TOTP only: seconds until the code changes")]
        int? SecondsRemaining,
        [property: DisplayName("Valid"),
                   Description("Whether the checked Code is valid within the window; none when no code was given")]
        bool? IsValid,
        [property: Description("How many steps from the current one the checked code matched, negative for an earlier TOTP period; none when it did not match or no code was given")]
        int? MatchOffset,
        [property: Description("The secret in Base32 without padding: the one given, normalized, or the one generated")]
        string Secret,
        [property: DisplayName("Secret (Hex)"),
                   Description("The secret's bytes as lowercase hexadecimal")]
        string SecretHex,
        [property: DisplayName("URI"),
                   Description("The otpauth:// URI an authenticator app reads, usually from a QR code")]
        string Uri
    ) : ToolResult
    {
        public Result() : this(string.Empty, null, string.Empty, 0, null, null, null, string.Empty, string.Empty, string.Empty)
        {
        }
    }
}

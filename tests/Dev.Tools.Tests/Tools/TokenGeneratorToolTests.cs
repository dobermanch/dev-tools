using Dev.Tools.Tools;

namespace Dev.Tools.Tests.Tools;

public class TokenGeneratorToolTests
{
    [Test]
    [Arguments(null)]
    [Arguments(1)]
    [Arguments(2)]
    public async Task WhenCountProvided_ShouldGenerateRightAmountOfTokens(int? expectedCount)
    {
        var args = expectedCount is null
            ? new TokenGeneratorTool.Args()
            : new TokenGeneratorTool.Args
            {
                TokenCount = expectedCount.Value
            };

        var result = await new TokenGeneratorTool().RunAsync(args, CancellationToken.None);

        await Assert.That(result.Tokens.Length).IsEqualTo(expectedCount ?? args.TokenCount);
    }
    
    [Test]
    [Arguments(null)]
    [Arguments(1)]
    [Arguments(10)]
    public async Task WhenLengthProvided_ShouldGenerateTokenOfRightLength(int? expectedLength)
    {
        var args = expectedLength is null
            ? new TokenGeneratorTool.Args()
            : new TokenGeneratorTool.Args
            {
                TokenLength = expectedLength.Value
            };

        var result = await new TokenGeneratorTool().RunAsync(args, CancellationToken.None);

        await Assert.That(result.Tokens[0].Length).IsEqualTo(expectedLength ?? args.TokenLength);
    }
    
    [Test]
    public async Task WhenExcludeSymbolsProvided_ShouldGenerateTokenWithoutThem()
    {
        char[] symbols = ['B', 'C'];
        var args = new TokenGeneratorTool.Args
        {
            ExcludeSymbols = new string(symbols),
            Alphabet = "ABCD"
        };

        var result = await new TokenGeneratorTool().RunAsync(args, CancellationToken.None);

        await Assert.That(result.Tokens[0].ToArray()).DoesNotContain(it => symbols.Contains(it));
    }
    
    [Test]
    public async Task WhenUppercaseSpecified_TokenShouldContainsOnlyUppercase()
    {
        var args = new TokenGeneratorTool.Args
        { 
            Numbers = false,
            Lowercase = false,
            Uppercase = true,
            Symbols = false,
            TokenLength = 10
        };

        var result = await new TokenGeneratorTool().RunAsync(args, CancellationToken.None);

        foreach (var ch in result.Tokens[0])
        {
            await Assert.That(char.IsUpper(ch)).IsTrue();
        }
    }
    
    [Test]
    public async Task WhenLowercaseSpecified_TokenShouldContainsOnlyLowercase()
    {
        var args = new TokenGeneratorTool.Args
        { 
            Numbers = false,
            Lowercase = true,
            Uppercase = false,
            Symbols = false,
            TokenLength = 10
        };

        var result = await new TokenGeneratorTool().RunAsync(args, CancellationToken.None);

        foreach (var ch in result.Tokens[0])
        {
            await Assert.That(char.IsLower(ch)).IsTrue();
        }
    }
    
    [Test]
    public async Task WhenNumbersSpecified_TokenShouldContainsOnlyNumbers()
    {
        var args = new TokenGeneratorTool.Args
        { 
            Numbers = true,
            Lowercase = false,
            Uppercase = false,
            Symbols = false,
            TokenLength = 10
        };

        var result = await new TokenGeneratorTool().RunAsync(args, CancellationToken.None);

        foreach (var ch in result.Tokens[0])
        {
            await Assert.That(char.IsDigit(ch)).IsTrue();
        }
    }
    
    [Test]
    public async Task WhenSymbolsSpecified_TokenShouldContainsOnlySymbols()
    {
        var args = new TokenGeneratorTool.Args
        { 
            Numbers = false,
            Lowercase = false,
            Uppercase = false,
            Symbols = true,
            TokenLength = 10
        };

        var result = await new TokenGeneratorTool().RunAsync(args, CancellationToken.None);

        foreach (var ch in result.Tokens[0])
        {
            await Assert.That(char.IsSymbol(ch) || char.IsPunctuation(ch)).IsTrue();
        }
    }

    [Test]
    public async Task WhenAlphabetProvided_TokenShouldContainsSymbolsFromAlphabet()
    {
        char[] symbols = ['B', 'C'];
        var args = new TokenGeneratorTool.Args
        {
            TokenLength = 30,
            Alphabet = new string(symbols)
        };

        var result = await new TokenGeneratorTool().RunAsync(args, CancellationToken.None);

        await Assert.That(result.Tokens[0].ToArray()).Contains(it => symbols.Contains(it));
    }
}

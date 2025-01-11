using Dev.Tools.Tools;
using FluentAssertions;

namespace Dev.Tools.Tests.Tools;

public class TokenGeneratorToolTests
{
    [Theory]
    [InlineData(null)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task WhenCountProvided_ShouldGenerateRightAmountOfTokens(int? expectedCount)
    {
        var args = expectedCount is null
            ? new TokenGeneratorTool.Args()
            : new TokenGeneratorTool.Args
            {
                TokenCount = expectedCount.Value
            };

        var result = await new TokenGeneratorTool().RunAsync(args, CancellationToken.None);

        Assert.Equal(expectedCount ?? args.TokenCount, result.Tokens.Length);
    }
    
    [Theory]
    [InlineData(null)]
    [InlineData(1)]
    [InlineData(10)]
    public async Task WhenLengthProvided_ShouldGenerateTokenOfRightLength(int? expectedLength)
    {
        var args = expectedLength is null
            ? new TokenGeneratorTool.Args()
            : new TokenGeneratorTool.Args
            {
                TokenLength = expectedLength.Value
            };

        var result = await new TokenGeneratorTool().RunAsync(args, CancellationToken.None);

        Assert.Equal(expectedLength ?? args.TokenLength, result.Tokens[0].Length);
    }
    
    [Fact]
    public async Task WhenExcludeSymbolsProvided_ShouldGenerateTokenWithoutThem()
    {
        char[] symbols = ['B', 'C'];
        var args = new TokenGeneratorTool.Args
        {
            ExcludeSymbols = new string(symbols),
            Alphabet = "ABCD"
        };

        var result = await new TokenGeneratorTool().RunAsync(args, CancellationToken.None);

        result.Tokens[0].ToArray().Should().NotContain(symbols);
    }
    
    [Fact]
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
            Assert.True(char.IsUpper(ch));
        }
    }
    
    [Fact]
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
            Assert.True(char.IsLower(ch));
        }
    }
    
    [Fact]
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
            Assert.True(char.IsDigit(ch));
        }
    }
    
    [Fact]
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
            Assert.True(char.IsSymbol(ch) || char.IsPunctuation(ch));
        }
    }

    [Fact]
    public async Task WhenAlphabetProvided_TokenShouldContainsSymbolsFromAlphabet()
    {
        char[] symbols = ['B', 'C'];
        var args = new TokenGeneratorTool.Args
        {
            TokenLength = 30,
            Alphabet = new string(symbols)
        };

        var result = await new TokenGeneratorTool().RunAsync(args, CancellationToken.None);

        result.Tokens[0].ToArray().Should().Contain(symbols);
    }
}

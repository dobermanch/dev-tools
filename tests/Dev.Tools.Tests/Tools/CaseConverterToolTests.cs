using Dev.Tools.Tools;
using CaseType = Dev.Tools.Tools.CaseConverterTool.CaseType;

namespace Dev.Tools.Tests.Tools;

public class CaseConverterToolTests
{
    private const string TestString = "ThisIs_A Test$string#123_with-Mixed.Cases;тестінг";
    
    [Theory]
    [InlineData(CaseType.LowerCase, TestString, "thisis_a test$string#123_with-mixed.cases;тестінг")]
    [InlineData(CaseType.UpperCase, TestString, "THISIS_A TEST$STRING#123_WITH-MIXED.CASES;ТЕСТІНГ")]
    [InlineData(CaseType.CamelCase, TestString, "thisIsATestString123WithMixedCasesТестінг")]
    [InlineData(CaseType.CapitalCase, TestString, "This Is A Test String 123 With Mixed Cases Тестінг")]
    [InlineData(CaseType.ConstantCase, TestString, "THIS_IS_A_TEST_STRING_123_WITH_MIXED_CASES_ТЕСТІНГ")]
    [InlineData(CaseType.DotCase, TestString, "this.is.a.test.string.123.with.mixed.cases.тестінг")]
    [InlineData(CaseType.HeaderCase, TestString, "This-Is-A-Test-String-123-With-Mixed-Cases-Тестінг")]
    [InlineData(CaseType.PathForwardCase, TestString, "this/is/a/test/string/123/with/mixed/cases/тестінг")]
    [InlineData(CaseType.MockingCase, TestString, "ThIsIs_a tEsT$StRiNg#123_wItH-MiXeD.CaSeS;ТеСтІнГ")]
    [InlineData(CaseType.NoCase, TestString, "this is a test string 123 with mixed cases тестінг")]
    [InlineData(CaseType.ParamCase, TestString, "this-is-a-test-string-123-with-mixed-cases-тестінг")]
    [InlineData(CaseType.PascalCase, TestString, "ThisIsATestString123WithMixedCasesТестінг")]
    [InlineData(CaseType.SentenceCase, TestString, "This is a test string 123 with mixed cases тестінг")]
    [InlineData(CaseType.SnakeCase, TestString, "this_is_a_test_string_123_with_mixed_cases_тестінг")]
    [InlineData(CaseType.PathBackwardCase, TestString, "this\\is\\a\\test\\string\\123\\with\\mixed\\cases\\тестінг")]
    public async Task Should_Convert_Test_To_Specified_Case(CaseType caseType, string text, string expected)
    {
        var args = new CaseConverterTool.Args
        {
            Text = text,
            Type = caseType,
        };
        
        var result = await new CaseConverterTool().RunAsync(args, CancellationToken.None);

        Assert.Equal(expected, result.Text);
    }
}
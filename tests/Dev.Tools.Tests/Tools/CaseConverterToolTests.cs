using Dev.Tools.Tools;
using CaseType = Dev.Tools.Tools.CaseConverterTool.CaseType;

namespace Dev.Tools.Tests.Tools;

public class CaseConverterToolTests
{
    private const string TestString = "ThisIs_A Test$string#123_with-Mixed.Cases;тестінг";
    
    [Test]
    [Arguments(CaseType.LowerCase, TestString, "thisis_a test$string#123_with-mixed.cases;тестінг")]
    [Arguments(CaseType.UpperCase, TestString, "THISIS_A TEST$STRING#123_WITH-MIXED.CASES;ТЕСТІНГ")]
    [Arguments(CaseType.CamelCase, TestString, "thisIsATestString123WithMixedCasesТестінг")]
    [Arguments(CaseType.CapitalCase, TestString, "This Is A Test String 123 With Mixed Cases Тестінг")]
    [Arguments(CaseType.ConstantCase, TestString, "THIS_IS_A_TEST_STRING_123_WITH_MIXED_CASES_ТЕСТІНГ")]
    [Arguments(CaseType.DotCase, TestString, "this.is.a.test.string.123.with.mixed.cases.тестінг")]
    [Arguments(CaseType.HeaderCase, TestString, "This-Is-A-Test-String-123-With-Mixed-Cases-Тестінг")]
    [Arguments(CaseType.PathForwardCase, TestString, "this/is/a/test/string/123/with/mixed/cases/тестінг")]
    [Arguments(CaseType.MockingCase, TestString, "ThIsIs_a tEsT$StRiNg#123_wItH-MiXeD.CaSeS;ТеСтІнГ")]
    [Arguments(CaseType.NoCase, TestString, "this is a test string 123 with mixed cases тестінг")]
    [Arguments(CaseType.ParamCase, TestString, "this-is-a-test-string-123-with-mixed-cases-тестінг")]
    [Arguments(CaseType.PascalCase, TestString, "ThisIsATestString123WithMixedCasesТестінг")]
    [Arguments(CaseType.SentenceCase, TestString, "This is a test string 123 with mixed cases тестінг")]
    [Arguments(CaseType.SnakeCase, TestString, "this_is_a_test_string_123_with_mixed_cases_тестінг")]
    [Arguments(CaseType.PathBackwardCase, TestString, "this\\is\\a\\test\\string\\123\\with\\mixed\\cases\\тестінг")]
    public async Task Should_Convert_Test_To_Specified_Case(CaseType caseType, string text, string expected)
    {
        var args = new CaseConverterTool.Args
        {
            Text = text,
            Type = caseType,
        };
        
        var result = await new CaseConverterTool().RunAsync(args, CancellationToken.None);

        await Assert.That(result.Text).IsEqualTo(expected);
    }
}
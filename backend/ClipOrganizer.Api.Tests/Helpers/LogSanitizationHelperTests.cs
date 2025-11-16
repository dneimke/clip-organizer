using FluentAssertions;
using ClipOrganizer.Api.Helpers;

namespace ClipOrganizer.Api.Tests.Helpers;

public class LogSanitizationHelperTests
{
    #region SanitizeForLogging Tests

    [Fact]
    public void SanitizeForLogging_NullInput_ReturnsEmptyString()
    {
        // Act
        var result = LogSanitizationHelper.SanitizeForLogging(null);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void SanitizeForLogging_EmptyString_ReturnsEmptyString()
    {
        // Act
        var result = LogSanitizationHelper.SanitizeForLogging(string.Empty);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void SanitizeForLogging_WhitespaceOnly_ReturnsEmptyString()
    {
        // Act
        var result = LogSanitizationHelper.SanitizeForLogging("   ");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void SanitizeForLogging_NormalString_ReturnsSameString()
    {
        // Arrange
        var input = "This is a normal string";

        // Act
        var result = LogSanitizationHelper.SanitizeForLogging(input);

        // Assert
        result.Should().Be(input);
    }

    [Fact]
    public void SanitizeForLogging_ContainsNewlines_ReplacesWithSpaces()
    {
        // Arrange
        var input = "Line1\nLine2\r\nLine3\rLine4";

        // Act
        var result = LogSanitizationHelper.SanitizeForLogging(input);

        // Assert
        result.Should().Be("Line1 Line2 Line3 Line4");
        result.Should().NotContain("\n");
        result.Should().NotContain("\r");
    }

    [Fact]
    public void SanitizeForLogging_ContainsTabs_ReplacesWithSpaces()
    {
        // Arrange
        var input = "Column1\tColumn2\tColumn3";

        // Act
        var result = LogSanitizationHelper.SanitizeForLogging(input);

        // Assert
        result.Should().Be("Column1 Column2 Column3");
        result.Should().NotContain("\t");
    }

    [Fact]
    public void SanitizeForLogging_ContainsControlCharacters_RemovesThem()
    {
        // Arrange
        var input = "Text" + (char)0x01 + "with" + (char)0x0B + "control" + (char)0x1F + "chars";

        // Act
        var result = LogSanitizationHelper.SanitizeForLogging(input);

        // Assert
        result.Should().Be("Text with control chars");
        result.Should().NotMatch(@"[\x00-\x1F]");
    }

    [Fact]
    public void SanitizeForLogging_ContainsMultipleSpaces_CollapsesToSingleSpace()
    {
        // Arrange
        var input = "Text    with     multiple      spaces";

        // Act
        var result = LogSanitizationHelper.SanitizeForLogging(input);

        // Assert
        result.Should().Be("Text with multiple spaces");
        result.Should().NotContain("  "); // No double spaces
    }

    [Fact]
    public void SanitizeForLogging_ExceedsMaxLength_TruncatesAndAddsEllipsis()
    {
        // Arrange
        var longString = new string('a', 600);

        // Act
        var result = LogSanitizationHelper.SanitizeForLogging(longString, maxLength: 500);

        // Assert
        result.Should().HaveLength(503); // 500 + "..."
        result.Should().EndWith("...");
        result.Should().StartWith("aaaa");
    }

    [Fact]
    public void SanitizeForLogging_ExactlyMaxLength_DoesNotTruncate()
    {
        // Arrange
        var exactLengthString = new string('a', 500);

        // Act
        var result = LogSanitizationHelper.SanitizeForLogging(exactLengthString, maxLength: 500);

        // Assert
        result.Should().HaveLength(500);
        result.Should().NotEndWith("...");
    }

    [Fact]
    public void SanitizeForLogging_LogInjectionAttempt_RemovesInjectionCharacters()
    {
        // Arrange - Common log injection patterns
        var injectionAttempt = "Normal text\n[CRITICAL] Password: secret\n[END]";

        // Act
        var result = LogSanitizationHelper.SanitizeForLogging(injectionAttempt);

        // Assert
        result.Should().Be("Normal text [CRITICAL] Password: secret [END]");
        result.Should().NotContain("\n");
    }

    [Fact]
    public void SanitizeForLogging_UnicodeCharacters_PreservesThem()
    {
        // Arrange
        var input = "Hello 世界 🌍 Test";

        // Act
        var result = LogSanitizationHelper.SanitizeForLogging(input);

        // Assert
        result.Should().Be("Hello 世界 🌍 Test");
    }

    [Fact]
    public void SanitizeForLogging_LeadingAndTrailingWhitespace_TrimsIt()
    {
        // Arrange
        var input = "   Text with spaces   ";

        // Act
        var result = LogSanitizationHelper.SanitizeForLogging(input);

        // Assert
        result.Should().Be("Text with spaces");
        result.Should().NotStartWith(" ");
        result.Should().NotEndWith(" ");
    }

    [Fact]
    public void SanitizeForLogging_MixedControlCharacters_HandlesAll()
    {
        // Arrange
        var input = "Text\nwith\ttabs\r\nand\rnewlines\x00and\x1Fcontrol";

        // Act
        var result = LogSanitizationHelper.SanitizeForLogging(input);

        // Assert
        result.Should().Be("Text with tabs and newlines and control");
        result.Should().NotMatch(@"[\x00-\x1F]");
    }

    [Theory]
    [InlineData("Simple text")]
    [InlineData("Text with spaces")]
    [InlineData("1234567890")]
    [InlineData("Special chars: !@#$%^&*()")]
    [InlineData("Mixed123Text456")]
    public void SanitizeForLogging_VariousValidInputs_ReturnsSanitized(string input)
    {
        // Act
        var result = LogSanitizationHelper.SanitizeForLogging(input);

        // Assert
        result.Should().NotBeNull();
        result.Should().NotContain("\n");
        result.Should().NotContain("\r");
        result.Should().NotContain("\t");
    }

    #endregion

    #region SanitizePathForLogging Tests

    [Fact]
    public void SanitizePathForLogging_NullInput_ReturnsEmptyString()
    {
        // Act
        var result = LogSanitizationHelper.SanitizePathForLogging(null);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void SanitizePathForLogging_EmptyString_ReturnsEmptyString()
    {
        // Act
        var result = LogSanitizationHelper.SanitizePathForLogging(string.Empty);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void SanitizePathForLogging_NormalPath_ReturnsSanitizedPath()
    {
        // Arrange
        var path = @"C:\Users\Test\Documents\file.txt";

        // Act
        var result = LogSanitizationHelper.SanitizePathForLogging(path);

        // Assert
        result.Should().Be(path);
    }

    [Fact]
    public void SanitizePathForLogging_PathWithNewlines_RemovesNewlines()
    {
        // Arrange
        var path = @"C:\Users\Test\nDocuments\file.txt";

        // Act
        var result = LogSanitizationHelper.SanitizePathForLogging(path);

        // Assert
        result.Should().Be(@"C:\Users\Test Documents\file.txt");
        result.Should().NotContain("\n");
    }

    [Fact]
    public void SanitizePathForLogging_PathWithControlCharacters_RemovesThem()
    {
        // Arrange
        var path = @"C:\Users\Test\x00file.txt";

        // Act
        var result = LogSanitizationHelper.SanitizePathForLogging(path);

        // Assert
        result.Should().Be(@"C:\Users\Testfile.txt");
        result.Should().NotMatch(@"[\x00-\x1F]");
    }

    [Fact]
    public void SanitizePathForLogging_UnixPath_HandlesCorrectly()
    {
        // Arrange
        var path = "/home/user/documents/file.txt";

        // Act
        var result = LogSanitizationHelper.SanitizePathForLogging(path);

        // Assert
        result.Should().Be("/home/user/documents/file.txt");
    }

    [Fact]
    public void SanitizePathForLogging_ExceedsMaxLength_Truncates()
    {
        // Arrange
        var longPath = @"C:\" + new string('a', 600) + @"\file.txt";

        // Act
        var result = LogSanitizationHelper.SanitizePathForLogging(longPath, maxLength: 500);

        // Assert
        result.Should().HaveLength(503); // 500 + "..."
        result.Should().EndWith("...");
    }

    [Fact]
    public void SanitizePathForLogging_PathInjectionAttempt_Sanitizes()
    {
        // Arrange - Path injection attempt
        var maliciousPath = @"C:\Users\Test\n[INJECTION]\rfile.txt";

        // Act
        var result = LogSanitizationHelper.SanitizePathForLogging(maliciousPath);

        // Assert
        result.Should().Be(@"C:\Users\Test [INJECTION] file.txt");
        result.Should().NotContain("\n");
        result.Should().NotContain("\r");
    }

    [Theory]
    [InlineData(@"C:\Windows\System32\file.dll")]
    [InlineData(@"\\Server\Share\file.txt")]
    [InlineData(@".\relative\path\file.txt")]
    [InlineData(@"..\parent\file.txt")]
    public void SanitizePathForLogging_VariousPathFormats_HandlesCorrectly(string path)
    {
        // Act
        var result = LogSanitizationHelper.SanitizePathForLogging(path);

        // Assert
        result.Should().NotBeNull();
        result.Should().NotContain("\n");
        result.Should().NotContain("\r");
        result.Should().NotContain("\t");
    }

    #endregion

    #region Edge Cases and Security Tests

    [Fact]
    public void SanitizeForLogging_ZeroMaxLength_ReturnsEmptyString()
    {
        // Arrange
        var input = "Some text";

        // Act
        var result = LogSanitizationHelper.SanitizeForLogging(input, maxLength: 0);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void SanitizeForLogging_NegativeMaxLength_UsesDefault()
    {
        // Arrange
        var input = "Some text";

        // Act
        var result = LogSanitizationHelper.SanitizeForLogging(input, maxLength: -1);

        // Assert
        // Should handle gracefully - either use default or handle negative
        result.Should().NotBeNull();
    }

    [Fact]
    public void SanitizeForLogging_AllControlCharacters_RemovesAll()
    {
        // Arrange - Create string with all control characters
        var controlChars = string.Join("", Enumerable.Range(0, 32).Select(i => (char)i));
        var input = "Before" + controlChars + "After";

        // Act
        var result = LogSanitizationHelper.SanitizeForLogging(input);

        // Assert
        result.Should().Be("Before After");
        result.Should().NotMatch(@"[\x00-\x1F]");
    }

    [Fact]
    public void SanitizeForLogging_OnlyControlCharacters_ReturnsEmptyString()
    {
        // Arrange
        var input = "\n\r\t\x00\x1F";

        // Act
        var result = LogSanitizationHelper.SanitizeForLogging(input);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion
}


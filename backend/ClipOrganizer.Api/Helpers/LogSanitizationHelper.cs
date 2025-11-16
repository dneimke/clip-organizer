namespace ClipOrganizer.Api.Helpers;

/// <summary>
/// Helper class for sanitizing user input before logging to prevent log injection attacks.
/// </summary>
public static class LogSanitizationHelper
{
    /// <summary>
    /// Sanitizes a string for safe logging by removing or escaping control characters
    /// that could be used for log injection attacks.
    /// </summary>
    /// <param name="input">The input string to sanitize</param>
    /// <param name="maxLength">Maximum length to truncate to (default: 500 characters)</param>
    /// <returns>Sanitized string safe for logging</returns>
    public static string SanitizeForLogging(string? input, int maxLength = 500)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }

        // Handle invalid maxLength values
        if (maxLength < 0)
        {
            maxLength = 500; // Use default for negative values
        }

        if (maxLength == 0)
        {
            return string.Empty;
        }

        // Truncate if too long
        var sanitized = input.Length > maxLength 
            ? input.Substring(0, maxLength) + "..." 
            : input;

        // Remove or replace control characters that could be used for log injection
        // Replace newlines, carriage returns, and tabs with spaces
        sanitized = sanitized
            .Replace("\r\n", " ")
            .Replace("\n", " ")
            .Replace("\r", " ")
            .Replace("\t", " ");

        // Replace other control characters (0x00-0x1F except space) with spaces
        sanitized = System.Text.RegularExpressions.Regex.Replace(
            sanitized, 
            @"[\x00-\x1F]", 
            " ");

        // Collapse multiple spaces into one
        sanitized = System.Text.RegularExpressions.Regex.Replace(
            sanitized, 
            @"\s+", 
            " ");

        return sanitized.Trim();
    }

    /// <summary>
    /// Sanitizes a file path for safe logging by removing sensitive path information
    /// if needed and sanitizing the path string.
    /// </summary>
    /// <param name="filePath">The file path to sanitize</param>
    /// <param name="maxLength">Maximum length to truncate to (default: 500 characters)</param>
    /// <returns>Sanitized file path safe for logging</returns>
    public static string SanitizePathForLogging(string? filePath, int maxLength = 500)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            return string.Empty;
        }

        // Use the general sanitization first
        var sanitized = SanitizeForLogging(filePath, maxLength);

        // Additional path-specific sanitization could go here if needed
        // For now, the general sanitization is sufficient

        return sanitized;
    }
}


using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using ClipOrganizer.Api.Services;

namespace ClipOrganizer.Api.Tests.Services;

public class YouTubeServiceTests
{
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger<YouTubeService>> _mockLogger;
    private const string TestApiKey = "test-api-key";

    public YouTubeServiceTests()
    {
        _mockConfiguration = new Mock<IConfiguration>();
        _mockConfiguration.Setup(c => c["YouTube:ApiKey"]).Returns(TestApiKey);
        _mockLogger = new Mock<ILogger<YouTubeService>>();
    }

    private YouTubeService CreateService()
    {
        return new YouTubeService(_mockConfiguration.Object, _mockLogger.Object);
    }

    #region ExtractVideoId Tests

    [Theory]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://youtu.be/dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/embed/dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ&t=30s", "dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/watch?feature=player_embedded&v=dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    public void ExtractVideoId_VariousValidFormats_ReturnsVideoId(string url, string expectedVideoId)
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.ExtractVideoId(url);

        // Assert
        result.Should().Be(expectedVideoId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void ExtractVideoId_EmptyOrWhitespace_ReturnsEmptyString(string? url)
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.ExtractVideoId(url!);

        // Assert
        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData("https://example.com/video")]
    [InlineData("not-a-youtube-url")]
    [InlineData("https://www.youtube.com/watch")]
    [InlineData("https://www.youtube.com/watch?v=")]
    [InlineData("https://youtu.be/")]
    [InlineData("dQw4w9WgXc")] // 10 characters, not 11
    [InlineData("dQw4w9WgXcQQ")] // 12 characters, not 11
    public void ExtractVideoId_InvalidUrls_ReturnsEmptyString(string url)
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.ExtractVideoId(url);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region IsValidYouTubeUrl Tests

    [Theory]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("https://youtu.be/dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/embed/dQw4w9WgXcQ")]
    [InlineData("dQw4w9WgXcQ")]
    public void IsValidYouTubeUrl_ValidUrls_ReturnsTrue(string url)
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.IsValidYouTubeUrl(url);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("https://example.com/video")]
    [InlineData("not-a-youtube-url")]
    [InlineData("https://www.youtube.com/watch")]
    public void IsValidYouTubeUrl_InvalidUrls_ReturnsFalse(string? url)
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.IsValidYouTubeUrl(url!);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region GetThumbnailUrl Tests

    [Fact]
    public void GetThumbnailUrl_ValidVideoId_ReturnsThumbnailUrl()
    {
        // Arrange
        var service = CreateService();
        var videoId = "dQw4w9WgXcQ";
        var expectedUrl = $"https://img.youtube.com/vi/{videoId}/hqdefault.jpg";

        // Act
        var result = service.GetThumbnailUrl(videoId);

        // Assert
        result.Should().Be(expectedUrl);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void GetThumbnailUrl_EmptyOrNull_ReturnsEmptyString(string? videoId)
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.GetThumbnailUrl(videoId!);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetThumbnailUrl_Whitespace_ReturnsThumbnailUrl()
    {
        // Arrange
        var service = CreateService();
        var whitespace = "   ";

        // Act
        var result = service.GetThumbnailUrl(whitespace);

        // Assert
        // The service doesn't trim whitespace, so it will create a URL with whitespace
        result.Should().Contain(whitespace);
    }

    #endregion

    #region GetVideoMetadataAsync Tests

    [Fact]
    public void GetVideoMetadataAsync_InvalidUrl_ThrowsArgumentException()
    {
        // Arrange
        var service = CreateService();
        var invalidUrl = "not-a-youtube-url";

        // Act
        var act = async () => await service.GetVideoMetadataAsync(invalidUrl);

        // Assert
        act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Invalid YouTube URL or video ID*")
            .WithParameterName("url");
    }

    [Fact]
    public void GetVideoMetadataAsync_EmptyUrl_ThrowsArgumentException()
    {
        // Arrange
        var service = CreateService();
        var emptyUrl = string.Empty;

        // Act
        var act = async () => await service.GetVideoMetadataAsync(emptyUrl);

        // Assert
        act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Invalid YouTube URL or video ID*")
            .WithParameterName("url");
    }

    // Note: Testing GetVideoMetadataAsync with successful API calls would require
    // mocking the Google YouTube API client, which is created in the constructor.
    // This would benefit from dependency injection refactoring to inject the
    // GoogleYouTubeService as a dependency. For now, we test error cases and
    // the method's validation logic.

    #endregion

    #region ParseDuration Tests (tested indirectly via GetVideoMetadataAsync)

    // Note: ParseDuration is a private method. To test it directly, we would need
    // to either:
    // 1. Make it internal and use InternalsVisibleTo
    // 2. Test it indirectly through GetVideoMetadataAsync
    // 3. Use reflection
    
    // For now, we rely on integration tests or refactoring to test ParseDuration.
    // The following test cases document expected behavior:
    // - PT1H2M10S should parse to 3730 seconds (1*3600 + 2*60 + 10)
    // - PT5M should parse to 300 seconds
    // - PT30S should parse to 30 seconds
    // - PT1H should parse to 3600 seconds
    // - PT2H30M should parse to 9000 seconds
    // - PT0S should parse to 0
    // - Invalid formats should return 0

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_MissingApiKey_ThrowsInvalidOperationException()
    {
        // Arrange
        var mockConfig = new Mock<IConfiguration>();
        mockConfig.Setup(c => c["YouTube:ApiKey"]).Returns((string?)null);
        var mockLogger = new Mock<ILogger<YouTubeService>>();

        // Act
        var act = () => new YouTubeService(mockConfig.Object, mockLogger.Object);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("YouTube API key is not configured*");
    }

    [Fact]
    public void Constructor_EmptyApiKey_ThrowsInvalidOperationException()
    {
        // Arrange
        var mockConfig = new Mock<IConfiguration>();
        mockConfig.Setup(c => c["YouTube:ApiKey"]).Returns(string.Empty);
        var mockLogger = new Mock<ILogger<YouTubeService>>();

        // Act
        var act = () => new YouTubeService(mockConfig.Object, mockLogger.Object);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("YouTube API key is not configured*");
    }

    [Fact]
    public void Constructor_ValidApiKey_CreatesService()
    {
        // Arrange & Act
        var service = CreateService();

        // Assert
        service.Should().NotBeNull();
    }

    #endregion
}


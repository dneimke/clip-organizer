using FluentAssertions;
using Moq;
using ClipOrganizer.Api.Models;
using ClipOrganizer.Api.Services;

namespace ClipOrganizer.Api.Tests.Services;

public class ClipValidationServiceTests
{
    private readonly Mock<IYouTubeService> _mockYouTubeService;
    private readonly ClipValidationService _service;

    public ClipValidationServiceTests()
    {
        _mockYouTubeService = new Mock<IYouTubeService>();
        _service = new ClipValidationService(_mockYouTubeService.Object);
    }

    #region DetermineStorageType Tests

    [Fact]
    public void DetermineStorageType_YouTubeUrl_ReturnsYouTube()
    {
        // Arrange
        var url = "https://www.youtube.com/watch?v=dQw4w9WgXcQ";
        _mockYouTubeService.Setup(x => x.IsValidYouTubeUrl(url)).Returns(true);

        // Act
        var result = _service.DetermineStorageType(url);

        // Assert
        result.Should().Be(StorageType.YouTube);
        _mockYouTubeService.Verify(x => x.IsValidYouTubeUrl(url), Times.Once);
    }

    [Theory]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("https://youtu.be/dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/embed/dQw4w9WgXcQ")]
    [InlineData("dQw4w9WgXcQ")]
    public void DetermineStorageType_VariousYouTubeFormats_ReturnsYouTube(string url)
    {
        // Arrange
        _mockYouTubeService.Setup(x => x.IsValidYouTubeUrl(url)).Returns(true);

        // Act
        var result = _service.DetermineStorageType(url);

        // Assert
        result.Should().Be(StorageType.YouTube);
    }

    [Fact]
    public void DetermineStorageType_LocalPath_ReturnsLocal()
    {
        // Arrange
        var path = @"C:\Videos\clip.mp4";
        _mockYouTubeService.Setup(x => x.IsValidYouTubeUrl(path)).Returns(false);

        // Act
        var result = _service.DetermineStorageType(path);

        // Assert
        result.Should().Be(StorageType.Local);
    }

    [Fact]
    public void DetermineStorageType_EmptyString_ThrowsArgumentException()
    {
        // Arrange
        var emptyString = string.Empty;

        // Act
        var act = () => _service.DetermineStorageType(emptyString);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Location string cannot be empty*")
            .WithParameterName("locationString");
    }

    [Fact]
    public void DetermineStorageType_Whitespace_ThrowsArgumentException()
    {
        // Arrange
        var whitespace = "   ";

        // Act
        var act = () => _service.DetermineStorageType(whitespace);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Location string cannot be empty*")
            .WithParameterName("locationString");
    }

    [Fact]
    public void DetermineStorageType_Null_ThrowsArgumentException()
    {
        // Arrange
        string? nullString = null;

        // Act
        var act = () => _service.DetermineStorageType(nullString!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Location string cannot be empty*")
            .WithParameterName("locationString");
    }

    #endregion

    #region ValidateLocalPath Tests

    [Fact]
    public void ValidateLocalPath_ExistingAbsolutePath_ReturnsTrue()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        try
        {
            // Act
            var result = _service.ValidateLocalPath(tempFile);

            // Assert
            result.Should().BeTrue();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void ValidateLocalPath_NonExistentAbsolutePath_ReturnsFalse()
    {
        // Arrange
        var nonExistentPath = @"C:\NonExistent\File.mp4";

        // Act
        var result = _service.ValidateLocalPath(nonExistentPath);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateLocalPath_RelativePath_ReturnsFalse()
    {
        // Arrange
        var relativePath = "videos/clip.mp4";

        // Act
        var result = _service.ValidateLocalPath(relativePath);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateLocalPath_EmptyString_ReturnsFalse()
    {
        // Arrange
        var emptyString = string.Empty;

        // Act
        var result = _service.ValidateLocalPath(emptyString);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateLocalPath_Whitespace_ReturnsFalse()
    {
        // Arrange
        var whitespace = "   ";

        // Act
        var result = _service.ValidateLocalPath(whitespace);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateLocalPath_Null_ReturnsFalse()
    {
        // Arrange
        string? nullString = null;

        // Act
        var result = _service.ValidateLocalPath(nullString!);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateLocalPath_InvalidPathCharacters_ReturnsFalse()
    {
        // Arrange
        var invalidPath = "C:\\test<>file.mp4"; // Contains invalid characters

        // Act
        var result = _service.ValidateLocalPath(invalidPath);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region ValidateYouTubeUrl Tests

    [Fact]
    public void ValidateYouTubeUrl_ValidUrl_ReturnsTrue()
    {
        // Arrange
        var url = "https://www.youtube.com/watch?v=dQw4w9WgXcQ";
        _mockYouTubeService.Setup(x => x.IsValidYouTubeUrl(url)).Returns(true);

        // Act
        var result = _service.ValidateYouTubeUrl(url);

        // Assert
        result.Should().BeTrue();
        _mockYouTubeService.Verify(x => x.IsValidYouTubeUrl(url), Times.Once);
    }

    [Theory]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("https://youtu.be/dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/embed/dQw4w9WgXcQ")]
    [InlineData("dQw4w9WgXcQ")]
    public void ValidateYouTubeUrl_VariousValidFormats_ReturnsTrue(string url)
    {
        // Arrange
        _mockYouTubeService.Setup(x => x.IsValidYouTubeUrl(url)).Returns(true);

        // Act
        var result = _service.ValidateYouTubeUrl(url);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateYouTubeUrl_InvalidUrl_ReturnsFalse()
    {
        // Arrange
        var invalidUrl = "https://example.com/video";
        _mockYouTubeService.Setup(x => x.IsValidYouTubeUrl(invalidUrl)).Returns(false);

        // Act
        var result = _service.ValidateYouTubeUrl(invalidUrl);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateYouTubeUrl_EmptyString_ReturnsFalse()
    {
        // Arrange
        var emptyString = string.Empty;
        _mockYouTubeService.Setup(x => x.IsValidYouTubeUrl(emptyString)).Returns(false);

        // Act
        var result = _service.ValidateYouTubeUrl(emptyString);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateYouTubeUrl_Null_ReturnsFalse()
    {
        // Arrange
        string? nullString = null;
        _mockYouTubeService.Setup(x => x.IsValidYouTubeUrl(nullString!)).Returns(false);

        // Act
        var result = _service.ValidateYouTubeUrl(nullString!);

        // Assert
        result.Should().BeFalse();
    }

    #endregion
}


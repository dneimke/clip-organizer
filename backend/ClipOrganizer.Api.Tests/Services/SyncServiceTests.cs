using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using ClipOrganizer.Api.Data;
using ClipOrganizer.Api.DTOs;
using ClipOrganizer.Api.Models;
using ClipOrganizer.Api.Services;
using ClipOrganizer.Api.Tests.Helpers;

namespace ClipOrganizer.Api.Tests.Services;

public class SyncServiceTests : IDisposable
{
    private readonly Mock<IThumbnailService> _mockThumbnailService;
    private readonly Mock<ILogger<SyncService>> _mockLogger;
    private readonly List<string> _tempDirectories = new();

    public SyncServiceTests()
    {
        _mockThumbnailService = new Mock<IThumbnailService>();
        _mockLogger = new Mock<ILogger<SyncService>>();
    }

    private SyncService CreateService(ClipDbContext context)
    {
        return new SyncService(context, _mockThumbnailService.Object, _mockLogger.Object);
    }

    private string CreateTempDirectory()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        _tempDirectories.Add(tempDir);
        return tempDir;
    }

    private string CreateTempFile(string directory, string fileName)
    {
        var filePath = Path.Combine(directory, fileName);
        File.WriteAllText(filePath, "test video content");
        return filePath;
    }

    public void Dispose()
    {
        foreach (var dir in _tempDirectories)
        {
            try
            {
                if (Directory.Exists(dir))
                {
                    Directory.Delete(dir, true);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    #region SyncAsync Tests

    [Fact]
    public async Task SyncAsync_EmptyRootFolderPath_ReturnsError()
    {
        // Arrange
        var context = TestHelpers.CreateInMemoryDbContext();
        var service = CreateService(context);
        var emptyPath = string.Empty;

        // Act
        var result = await service.SyncAsync(emptyPath);

        // Assert
        result.Errors.Should().HaveCount(1);
        result.Errors[0].ErrorMessage.Should().Be("Root folder path is required");
        result.TotalAdded.Should().Be(0);
        result.TotalRemoved.Should().Be(0);
    }

    [Fact]
    public async Task SyncAsync_RelativePath_ReturnsError()
    {
        // Arrange
        var context = TestHelpers.CreateInMemoryDbContext();
        var service = CreateService(context);
        var relativePath = "videos/clips";

        // Act
        var result = await service.SyncAsync(relativePath);

        // Assert
        result.Errors.Should().HaveCount(1);
        result.Errors[0].ErrorMessage.Should().Be("Root folder path must be an absolute path");
    }

    [Fact]
    public async Task SyncAsync_NonExistentDirectory_ReturnsError()
    {
        // Arrange
        var context = TestHelpers.CreateInMemoryDbContext();
        var service = CreateService(context);
        // Use platform-agnostic absolute path that doesn't exist
        var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "NonExistent");

        // Act
        var result = await service.SyncAsync(nonExistentPath);

        // Assert
        result.Errors.Should().HaveCount(1);
        result.Errors[0].ErrorMessage.Should().Be("Root folder does not exist");
    }

    [Fact]
    public async Task SyncAsync_NewFiles_AddsToDatabase()
    {
        // Arrange
        var tempDir = CreateTempDirectory();
        var videoFile = CreateTempFile(tempDir, "test.mp4");
        var context = TestHelpers.CreateInMemoryDbContext();
        var service = CreateService(context);
        _mockThumbnailService.Setup(x => x.GenerateThumbnailAsync(videoFile, It.IsAny<int>()))
            .ReturnsAsync($"/thumbnails/1.jpg");

        // Act
        var result = await service.SyncAsync(tempDir);

        // Assert
        result.TotalAdded.Should().Be(1);
        result.AddedClips.Should().HaveCount(1);
        result.AddedClips[0].FilePath.Should().Be(videoFile);
        result.Errors.Should().BeEmpty();

        var clips = await context.Clips.ToListAsync();
        clips.Should().HaveCount(1);
        clips[0].LocationString.Should().Be(videoFile);
        clips[0].StorageType.Should().Be(StorageType.Local);
    }

    [Fact]
    public async Task SyncAsync_ExistingFiles_DoesNotDuplicate()
    {
        // Arrange
        var tempDir = CreateTempDirectory();
        var videoFile = CreateTempFile(tempDir, "test.mp4");
        var context = TestHelpers.CreateInMemoryDbContext();
        
        // Add existing clip
        var existingClip = new ClipBuilder()
            .WithTitle("test")
            .WithStorageType(StorageType.Local)
            .WithLocationString(videoFile)
            .Build();
        context.Clips.Add(existingClip);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.SyncAsync(tempDir);

        // Assert
        result.TotalAdded.Should().Be(0);
        result.AddedClips.Should().BeEmpty();
        
        var clips = await context.Clips.ToListAsync();
        clips.Should().HaveCount(1);
    }

    [Fact]
    public async Task SyncAsync_MissingFiles_RemovesFromDatabase()
    {
        // Arrange
        var tempDir = CreateTempDirectory();
        var context = TestHelpers.CreateInMemoryDbContext();
        
        // Add clip for file that doesn't exist
        var missingClip = new ClipBuilder()
            .WithTitle("Missing Clip")
            .WithStorageType(StorageType.Local)
            .WithLocationString(Path.Combine(tempDir, "missing.mp4"))
            .Build();
        context.Clips.Add(missingClip);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.SyncAsync(tempDir);

        // Assert
        result.TotalRemoved.Should().Be(1);
        result.RemovedClips.Should().HaveCount(1);
        result.RemovedClips[0].ClipId.Should().Be(missingClip.Id);
        
        var clips = await context.Clips.ToListAsync();
        clips.Should().BeEmpty();
    }

    [Fact]
    public async Task SyncAsync_InvalidFileExtensions_FilteredOut()
    {
        // Arrange
        var tempDir = CreateTempDirectory();
        CreateTempFile(tempDir, "test.txt"); // Not a video file
        CreateTempFile(tempDir, "test.mp4"); // Valid video file
        var context = TestHelpers.CreateInMemoryDbContext();
        var service = CreateService(context);
        _mockThumbnailService.Setup(x => x.GenerateThumbnailAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync((string?)null);

        // Act
        var result = await service.SyncAsync(tempDir);

        // Assert
        result.TotalAdded.Should().Be(1);
        result.AddedClips.Should().HaveCount(1);
        result.AddedClips[0].FilePath.Should().EndWith(".mp4");
    }

    [Fact]
    public async Task SyncAsync_ThumbnailGenerationFailure_DoesNotFailSync()
    {
        // Arrange
        var tempDir = CreateTempDirectory();
        var videoFile = CreateTempFile(tempDir, "test.mp4");
        var context = TestHelpers.CreateInMemoryDbContext();
        var service = CreateService(context);
        _mockThumbnailService.Setup(x => x.GenerateThumbnailAsync(videoFile, It.IsAny<int>()))
            .ThrowsAsync(new Exception("Thumbnail generation failed"));

        // Act
        var result = await service.SyncAsync(tempDir);

        // Assert
        result.TotalAdded.Should().Be(1);
        result.Errors.Should().BeEmpty(); // Thumbnail failure shouldn't add to errors
        var clips = await context.Clips.ToListAsync();
        clips.Should().HaveCount(1);
    }

    [Fact]
    public async Task SyncAsync_CaseInsensitivePathMatching_Works()
    {
        // Arrange
        var tempDir = CreateTempDirectory();
        var videoFile = CreateTempFile(tempDir, "test.mp4");
        var context = TestHelpers.CreateInMemoryDbContext();
        
        // Add clip with different case
        var existingClip = new ClipBuilder()
            .WithTitle("test")
            .WithStorageType(StorageType.Local)
            .WithLocationString(videoFile.ToUpperInvariant())
            .Build();
        context.Clips.Add(existingClip);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.SyncAsync(tempDir);

        // Assert
        result.TotalAdded.Should().Be(0); // Should match existing clip despite case difference
    }

    [Fact]
    public async Task SyncAsync_RecursiveDirectoryScanning_FindsFilesInSubdirectories()
    {
        // Arrange
        var tempDir = CreateTempDirectory();
        var subDir = Path.Combine(tempDir, "subdir");
        Directory.CreateDirectory(subDir);
        var videoFile1 = CreateTempFile(tempDir, "test1.mp4");
        var videoFile2 = CreateTempFile(subDir, "test2.mp4");
        var context = TestHelpers.CreateInMemoryDbContext();
        var service = CreateService(context);
        _mockThumbnailService.Setup(x => x.GenerateThumbnailAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync((string?)null);

        // Act
        var result = await service.SyncAsync(tempDir);

        // Assert
        result.TotalAdded.Should().Be(2);
        result.AddedClips.Should().HaveCount(2);
    }

    #endregion

    #region PreviewSyncAsync Tests

    [Fact]
    public async Task PreviewSyncAsync_EmptyRootFolderPath_ReturnsError()
    {
        // Arrange
        var context = TestHelpers.CreateInMemoryDbContext();
        var service = CreateService(context);
        var emptyPath = string.Empty;

        // Act
        var result = await service.PreviewSyncAsync(emptyPath);

        // Assert
        result.ErrorCount.Should().BeGreaterThan(0);
        result.Items.Should().Contain(i => i.Status == "error");
    }

    [Fact]
    public async Task PreviewSyncAsync_NewFiles_ReturnsNewStatus()
    {
        // Arrange
        var tempDir = CreateTempDirectory();
        var videoFile = CreateTempFile(tempDir, "test.mp4");
        var context = TestHelpers.CreateInMemoryDbContext();
        var service = CreateService(context);

        // Act
        var result = await service.PreviewSyncAsync(tempDir);

        // Assert
        result.NewFilesCount.Should().Be(1);
        result.Items.Should().Contain(i => i.Status == "new" && i.FilePath == videoFile);
    }

    [Fact]
    public async Task PreviewSyncAsync_MatchedFiles_ReturnsMatchedStatus()
    {
        // Arrange
        var tempDir = CreateTempDirectory();
        var videoFile = CreateTempFile(tempDir, "test.mp4");
        var context = TestHelpers.CreateInMemoryDbContext();
        
        var existingClip = new ClipBuilder()
            .WithTitle("test")
            .WithStorageType(StorageType.Local)
            .WithLocationString(videoFile)
            .Build();
        context.Clips.Add(existingClip);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.PreviewSyncAsync(tempDir);

        // Assert
        result.MatchedFilesCount.Should().Be(1);
        result.Items.Should().Contain(i => i.Status == "matched" && i.ClipId == existingClip.Id);
    }

    [Fact]
    public async Task PreviewSyncAsync_MissingFiles_ReturnsMissingStatus()
    {
        // Arrange
        var tempDir = CreateTempDirectory();
        var context = TestHelpers.CreateInMemoryDbContext();
        
        var missingClip = new ClipBuilder()
            .WithTitle("Missing Clip")
            .WithStorageType(StorageType.Local)
            .WithLocationString(Path.Combine(tempDir, "missing.mp4"))
            .Build();
        context.Clips.Add(missingClip);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        // Act
        var result = await service.PreviewSyncAsync(tempDir);

        // Assert
        result.MissingFilesCount.Should().Be(1);
        result.Items.Should().Contain(i => i.Status == "missing" && i.ClipId == missingClip.Id);
    }

    #endregion

    #region SelectiveSyncAsync Tests

    [Fact]
    public async Task SelectiveSyncAsync_AddsSelectedFiles()
    {
        // Arrange
        var tempDir = CreateTempDirectory();
        var videoFile = CreateTempFile(tempDir, "test.mp4");
        var context = TestHelpers.CreateInMemoryDbContext();
        var service = CreateService(context);

        var request = new SelectiveSyncRequestDto
        {
            RootFolderPath = tempDir,
            FilesToAdd = new List<string> { videoFile },
            ClipIdsToRemove = new List<int>()
        };

        // Act
        var result = await service.SelectiveSyncAsync(request);

        // Assert
        result.TotalAdded.Should().Be(1);
        result.AddedClips.Should().HaveCount(1);
        result.AddedClips[0].FilePath.Should().Be(videoFile);
    }

    [Fact]
    public async Task SelectiveSyncAsync_RemovesSelectedClips()
    {
        // Arrange
        var tempDir = CreateTempDirectory();
        var context = TestHelpers.CreateInMemoryDbContext();
        var clipToRemove = new ClipBuilder()
            .WithTitle("To Remove")
            .WithStorageType(StorageType.Local)
            .WithLocationString(Path.Combine(tempDir, "test.mp4"))
            .Build();
        context.Clips.Add(clipToRemove);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var request = new SelectiveSyncRequestDto
        {
            RootFolderPath = tempDir,
            FilesToAdd = new List<string>(),
            ClipIdsToRemove = new List<int> { clipToRemove.Id }
        };

        // Act
        var result = await service.SelectiveSyncAsync(request);

        // Assert
        result.TotalRemoved.Should().Be(1);
        result.RemovedClips.Should().HaveCount(1);
        result.RemovedClips[0].ClipId.Should().Be(clipToRemove.Id);
        
        var clips = await context.Clips.ToListAsync();
        clips.Should().BeEmpty();
    }

    [Fact]
    public async Task SelectiveSyncAsync_NonExistentFile_AddsError()
    {
        // Arrange
        var tempDir = CreateTempDirectory();
        var context = TestHelpers.CreateInMemoryDbContext();
        var service = CreateService(context);

        var request = new SelectiveSyncRequestDto
        {
            RootFolderPath = tempDir,
            FilesToAdd = new List<string> { Path.Combine(tempDir, "nonexistent.mp4") },
            ClipIdsToRemove = new List<int>()
        };

        // Act
        var result = await service.SelectiveSyncAsync(request);

        // Assert
        result.Errors.Should().HaveCount(1);
        result.Errors[0].ErrorMessage.Should().Be("File does not exist");
    }

    [Fact]
    public async Task SelectiveSyncAsync_DuplicateClip_AddsError()
    {
        // Arrange
        var tempDir = CreateTempDirectory();
        var videoFile = CreateTempFile(tempDir, "test.mp4");
        var context = TestHelpers.CreateInMemoryDbContext();
        
        var existingClip = new ClipBuilder()
            .WithTitle("test")
            .WithStorageType(StorageType.Local)
            .WithLocationString(videoFile)
            .Build();
        context.Clips.Add(existingClip);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var request = new SelectiveSyncRequestDto
        {
            RootFolderPath = tempDir,
            FilesToAdd = new List<string> { videoFile },
            ClipIdsToRemove = new List<int>()
        };

        // Act
        var result = await service.SelectiveSyncAsync(request);

        // Assert
        result.Errors.Should().HaveCount(1);
        result.Errors[0].ErrorMessage.Should().Be("A clip with this file path already exists");
    }

    [Fact]
    public async Task SelectiveSyncAsync_NonExistentClipId_AddsError()
    {
        // Arrange
        var tempDir = CreateTempDirectory();
        var context = TestHelpers.CreateInMemoryDbContext();
        var service = CreateService(context);

        var request = new SelectiveSyncRequestDto
        {
            RootFolderPath = tempDir,
            FilesToAdd = new List<string>(),
            ClipIdsToRemove = new List<int> { 999 }
        };

        // Act
        var result = await service.SelectiveSyncAsync(request);

        // Assert
        result.Errors.Should().HaveCount(1);
        result.Errors[0].ErrorMessage.Should().Contain("not found");
    }

    #endregion
}


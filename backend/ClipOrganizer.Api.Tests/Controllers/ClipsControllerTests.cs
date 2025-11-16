using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using ClipOrganizer.Api.Controllers;
using ClipOrganizer.Api.Data;
using ClipOrganizer.Api.DTOs;
using ClipOrganizer.Api.Models;
using ClipOrganizer.Api.Services;
using ClipOrganizer.Api.Tests.Helpers;

namespace ClipOrganizer.Api.Tests.Controllers;

public class ClipsControllerTests : IDisposable
{
    private readonly ClipDbContext _context;
    private readonly Mock<IClipValidationService> _mockValidationService;
    private readonly Mock<IYouTubeService> _mockYouTubeService;
    private readonly Mock<IAIClipGenerationService> _mockAIGenerationService;
    private readonly Mock<IAIQueryService> _mockAIQueryService;
    private readonly Mock<ISyncService> _mockSyncService;
    private readonly Mock<IThumbnailService> _mockThumbnailService;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger<ClipsController>> _mockLogger;
    private readonly ClipsController _controller;
    private readonly List<string> _tempFiles = new();

    public ClipsControllerTests()
    {
        _context = TestHelpers.CreateInMemoryDbContext();
        _mockValidationService = new Mock<IClipValidationService>();
        _mockYouTubeService = new Mock<IYouTubeService>();
        _mockAIGenerationService = new Mock<IAIClipGenerationService>();
        _mockAIQueryService = new Mock<IAIQueryService>();
        _mockSyncService = new Mock<ISyncService>();
        _mockThumbnailService = new Mock<IThumbnailService>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<ClipsController>>();

        _controller = new ClipsController(
            _context,
            _mockValidationService.Object,
            _mockYouTubeService.Object,
            _mockAIGenerationService.Object,
            _mockAIQueryService.Object,
            _mockSyncService.Object,
            _mockThumbnailService.Object,
            _mockConfiguration.Object,
            _mockLogger.Object
        );
    }

    private string CreateTempFile(string fileName)
    {
        var tempFile = Path.Combine(Path.GetTempPath(), fileName);
        File.WriteAllText(tempFile, "test content");
        _tempFiles.Add(tempFile);
        return tempFile;
    }

    public void Dispose()
    {
        _context.Dispose();
        foreach (var file in _tempFiles)
        {
            try
            {
                if (File.Exists(file))
                    File.Delete(file);
            }
            catch { }
        }
    }

    #region GetClips Tests

    [Fact]
    public async Task GetClips_NoFilters_ReturnsAllClips()
    {
        // Arrange
        var tag = new TagBuilder().WithId(1).WithCategory(TagCategory.SkillTactic).WithValue("Flick").Build();
        var clip1 = new ClipBuilder().WithId(1).WithTitle("Clip 1").WithTags(tag).Build();
        var clip2 = new ClipBuilder().WithId(2).WithTitle("Clip 2").Build();
        
        _context.Tags.Add(tag);
        _context.Clips.AddRange(clip1, clip2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetClips(searchTerm: null, tagIds: null, subfolders: null);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var clips = okResult.Value.Should().BeAssignableTo<IEnumerable<ClipDto>>().Subject;
        clips.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetClips_WithSearchTerm_FiltersByTitle()
    {
        // Arrange
        var clip1 = new ClipBuilder().WithId(1).WithTitle("Flick Training").Build();
        var clip2 = new ClipBuilder().WithId(2).WithTitle("Defense Drill").Build();
        
        _context.Clips.AddRange(clip1, clip2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetClips(searchTerm: "Flick", tagIds: null, subfolders: null);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var clips = okResult.Value.Should().BeAssignableTo<IEnumerable<ClipDto>>().Subject;
        clips.Should().HaveCount(1);
        clips.First().Title.Should().Be("Flick Training");
    }

    [Fact]
    public async Task GetClips_WithTagIds_FiltersByTags()
    {
        // Arrange
        var tag1 = new TagBuilder().WithId(1).WithCategory(TagCategory.SkillTactic).WithValue("Flick").Build();
        var tag2 = new TagBuilder().WithId(2).WithCategory(TagCategory.FieldArea).WithValue("Midfield").Build();
        var clip1 = new ClipBuilder().WithId(1).WithTitle("Clip 1").WithTags(tag1).Build();
        var clip2 = new ClipBuilder().WithId(2).WithTitle("Clip 2").WithTags(tag2).Build();
        
        _context.Tags.AddRange(tag1, tag2);
        _context.Clips.AddRange(clip1, clip2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetClips(searchTerm: null, tagIds: new List<int> { 1 }, subfolders: null);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var clips = okResult.Value.Should().BeAssignableTo<IEnumerable<ClipDto>>().Subject;
        clips.Should().HaveCount(1);
        clips.First().Id.Should().Be(1);
    }

    [Fact]
    public async Task GetClips_WithSortByTitle_SortsCorrectly()
    {
        // Arrange
        var clip1 = new ClipBuilder().WithId(1).WithTitle("Zebra").Build();
        var clip2 = new ClipBuilder().WithId(2).WithTitle("Apple").Build();
        
        _context.Clips.AddRange(clip1, clip2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetClips(searchTerm: null, tagIds: null, subfolders: null, sortBy: "title", sortOrder: "asc");

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var clips = okResult.Value.Should().BeAssignableTo<IEnumerable<ClipDto>>().Subject.ToList();
        clips[0].Title.Should().Be("Apple");
        clips[1].Title.Should().Be("Zebra");
    }

    [Fact]
    public async Task GetClips_UnclassifiedOnly_FiltersUnclassified()
    {
        // Arrange
        var tag = new TagBuilder().WithId(1).WithCategory(TagCategory.SkillTactic).WithValue("Flick").Build();
        var clip1 = new ClipBuilder().WithId(1).WithTitle("Classified").WithTags(tag).WithDescription("Has description").Build();
        var clip2 = new ClipBuilder().WithId(2).WithTitle("Unclassified").Build();
        
        _context.Tags.Add(tag);
        _context.Clips.AddRange(clip1, clip2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetClips(searchTerm: null, tagIds: null, subfolders: null, unclassifiedOnly: true);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var clips = okResult.Value.Should().BeAssignableTo<IEnumerable<ClipDto>>().Subject;
        clips.Should().HaveCount(1);
        clips.First().Id.Should().Be(2);
    }

    #endregion

    #region GetClip Tests

    [Fact]
    public async Task GetClip_ExistingId_ReturnsClip()
    {
        // Arrange
        var tag = new TagBuilder().WithId(1).WithCategory(TagCategory.SkillTactic).WithValue("Flick").Build();
        var clip = new ClipBuilder().WithId(1).WithTitle("Test Clip").WithTags(tag).Build();
        
        _context.Tags.Add(tag);
        _context.Clips.Add(clip);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetClip(1);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var clipDto = okResult.Value.Should().BeOfType<ClipDto>().Subject;
        clipDto.Id.Should().Be(1);
        clipDto.Title.Should().Be("Test Clip");
        clipDto.Tags.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetClip_NonExistentId_ReturnsNotFound()
    {
        // Act
        var result = await _controller.GetClip(999);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    #endregion

    #region CreateClip Tests

    [Fact]
    public async Task CreateClip_YouTubeUrl_CreatesYouTubeClip()
    {
        // Arrange
        var dto = new CreateClipDto
        {
            LocationString = "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
            Title = "Test Video"
        };

        _mockValidationService.Setup(x => x.DetermineStorageType(dto.LocationString))
            .Returns(StorageType.YouTube);
        _mockValidationService.Setup(x => x.ValidateYouTubeUrl(dto.LocationString))
            .Returns(true);
        _mockYouTubeService.Setup(x => x.GetVideoMetadataAsync(dto.LocationString))
            .ReturnsAsync(("Video Title", 300, "dQw4w9WgXcQ"));
        _mockYouTubeService.Setup(x => x.GetThumbnailUrl("dQw4w9WgXcQ"))
            .Returns("https://img.youtube.com/vi/dQw4w9WgXcQ/hqdefault.jpg");

        // Act
        var result = await _controller.CreateClip(dto);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var clipDto = createdResult.Value.Should().BeOfType<ClipDto>().Subject;
        clipDto.StorageType.Should().Be("YouTube");
        clipDto.LocationString.Should().Be("dQw4w9WgXcQ");
        
        var clips = await _context.Clips.ToListAsync();
        clips.Should().HaveCount(1);
    }

    [Fact]
    public async Task CreateClip_LocalFile_CreatesLocalClip()
    {
        // Arrange
        var tempFile = CreateTempFile("test.mp4");
        var dto = new CreateClipDto
        {
            LocationString = tempFile,
            Title = "Local Clip"
        };

        _mockValidationService.Setup(x => x.DetermineStorageType(dto.LocationString))
            .Returns(StorageType.Local);
        _mockValidationService.Setup(x => x.ValidateLocalPath(dto.LocationString))
            .Returns(true);
        _mockThumbnailService.Setup(x => x.GenerateThumbnailAsync(tempFile, It.IsAny<int>()))
            .ReturnsAsync((string?)null);

        // Act
        var result = await _controller.CreateClip(dto);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var clipDto = createdResult.Value.Should().BeOfType<ClipDto>().Subject;
        clipDto.StorageType.Should().Be("Local");
        
        var clips = await _context.Clips.ToListAsync();
        clips.Should().HaveCount(1);
    }

    [Fact]
    public async Task CreateClip_InvalidLocationString_ReturnsBadRequest()
    {
        // Arrange
        var dto = new CreateClipDto
        {
            LocationString = string.Empty
        };

        // Act
        var result = await _controller.CreateClip(dto);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CreateClip_WithTags_AssociatesTags()
    {
        // Arrange
        var tempFile = CreateTempFile("test.mp4");
        var tag = new TagBuilder().WithId(1).WithCategory(TagCategory.SkillTactic).WithValue("Flick").Build();
        _context.Tags.Add(tag);
        await _context.SaveChangesAsync();

        var dto = new CreateClipDto
        {
            LocationString = tempFile,
            TagIds = new List<int> { 1 }
        };

        _mockValidationService.Setup(x => x.DetermineStorageType(dto.LocationString))
            .Returns(StorageType.Local);
        _mockValidationService.Setup(x => x.ValidateLocalPath(dto.LocationString))
            .Returns(true);
        _mockThumbnailService.Setup(x => x.GenerateThumbnailAsync(tempFile, It.IsAny<int>()))
            .ReturnsAsync((string?)null);

        // Act
        var result = await _controller.CreateClip(dto);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var clipDto = createdResult.Value.Should().BeOfType<ClipDto>().Subject;
        clipDto.Tags.Should().HaveCount(1);
        clipDto.Tags[0].Id.Should().Be(1);
    }

    #endregion

    #region UpdateClip Tests

    [Fact]
    public async Task UpdateClip_ExistingClip_UpdatesSuccessfully()
    {
        // Arrange
        var tempFile = CreateTempFile("test.mp4");
        var clip = new ClipBuilder()
            .WithId(1)
            .WithTitle("Old Title")
            .WithStorageType(StorageType.Local)
            .WithLocationString(tempFile)
            .Build();
        _context.Clips.Add(clip);
        await _context.SaveChangesAsync();

        var dto = new CreateClipDto
        {
            LocationString = tempFile,
            Title = "New Title",
            Description = "New Description"
        };

        _mockValidationService.Setup(x => x.DetermineStorageType(dto.LocationString))
            .Returns(StorageType.Local);
        _mockValidationService.Setup(x => x.ValidateLocalPath(dto.LocationString))
            .Returns(true);

        // Act
        var result = await _controller.UpdateClip(1, dto);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var clipDto = okResult.Value.Should().BeOfType<ClipDto>().Subject;
        clipDto.Title.Should().Be("New Title");
        clipDto.Description.Should().Be("New Description");
    }

    [Fact]
    public async Task UpdateClip_NonExistentClip_ReturnsNotFound()
    {
        // Arrange
        var dto = new CreateClipDto { LocationString = "test.mp4" };

        // Act
        var result = await _controller.UpdateClip(999, dto);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    #endregion

    #region DeleteClip Tests

    [Fact]
    public async Task DeleteClip_ExistingClip_DeletesSuccessfully()
    {
        // Arrange
        var clip = new ClipBuilder().WithId(1).WithTitle("To Delete").Build();
        _context.Clips.Add(clip);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.DeleteClip(1);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        var clips = await _context.Clips.ToListAsync();
        clips.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteClip_NonExistentClip_ReturnsNotFound()
    {
        // Act
        var result = await _controller.DeleteClip(999);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    #endregion

    #region BulkUpload Tests

    [Fact]
    public async Task BulkUpload_ValidFiles_ProcessesSuccessfully()
    {
        // Arrange
        var tempFile1 = CreateTempFile("test1.mp4");
        var tempFile2 = CreateTempFile("test2.mp4");
        var request = new BulkUploadRequestDto
        {
            FilePaths = new List<string> { tempFile1, tempFile2 }
        };

        _mockValidationService.Setup(x => x.ValidateLocalPath(It.IsAny<string>()))
            .Returns(true);
        _mockThumbnailService.Setup(x => x.GenerateThumbnailAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync((string?)null);

        // Act
        var result = await _controller.BulkUpload(request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<BulkUploadResponseDto>().Subject;
        response.Successes.Should().HaveCount(2);
        response.Failures.Should().BeEmpty();
    }

    [Fact]
    public async Task BulkUpload_DuplicateFiles_AddsToFailures()
    {
        // Arrange
        var tempFile = CreateTempFile("test.mp4");
        var existingClip = new ClipBuilder()
            .WithTitle("Existing")
            .WithStorageType(StorageType.Local)
            .WithLocationString(tempFile)
            .Build();
        _context.Clips.Add(existingClip);
        await _context.SaveChangesAsync();

        var request = new BulkUploadRequestDto
        {
            FilePaths = new List<string> { tempFile }
        };

        _mockValidationService.Setup(x => x.ValidateLocalPath(tempFile))
            .Returns(true);

        // Act
        var result = await _controller.BulkUpload(request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<BulkUploadResponseDto>().Subject;
        response.Failures.Should().HaveCount(1);
        response.Failures[0].ErrorMessage.Should().Contain("already exists");
    }

    [Fact]
    public async Task BulkUpload_InvalidFileExtension_AddsToFailures()
    {
        // Arrange
        var tempFile = CreateTempFile("test.txt");
        var request = new BulkUploadRequestDto
        {
            FilePaths = new List<string> { tempFile }
        };

        _mockValidationService.Setup(x => x.ValidateLocalPath(tempFile))
            .Returns(true);

        // Act
        var result = await _controller.BulkUpload(request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<BulkUploadResponseDto>().Subject;
        response.Failures.Should().HaveCount(1);
        response.Failures[0].ErrorMessage.Should().Contain("Invalid video file extension");
    }

    #endregion

    #region Sync Tests

    [Fact]
    public async Task Sync_ValidRequest_CallsSyncService()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        _tempFiles.Add(tempDir); // Track for cleanup
        
        var request = new SyncRequestDto
        {
            RootFolderPath = tempDir
        };

        var syncResponse = new SyncResponseDto
        {
            TotalAdded = 5,
            TotalRemoved = 2
        };

        _mockSyncService.Setup(x => x.SyncAsync(tempDir))
            .ReturnsAsync(syncResponse);

        // Act
        var result = await _controller.Sync(request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<SyncResponseDto>().Subject;
        response.TotalAdded.Should().Be(5);
        response.TotalRemoved.Should().Be(2);
        _mockSyncService.Verify(x => x.SyncAsync(tempDir), Times.Once);
        
        // Cleanup
        try { Directory.Delete(tempDir, true); } catch { }
    }

    #endregion
}


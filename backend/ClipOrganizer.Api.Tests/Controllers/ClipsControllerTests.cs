using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
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
    private readonly Mock<IWebHostEnvironment> _mockWebHostEnvironment;
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
        _mockWebHostEnvironment = new Mock<IWebHostEnvironment>();
        _mockWebHostEnvironment.Setup(x => x.ContentRootPath).Returns(Path.GetTempPath());

        _controller = new ClipsController(
            _context,
            _mockValidationService.Object,
            _mockYouTubeService.Object,
            _mockAIGenerationService.Object,
            _mockAIQueryService.Object,
            _mockSyncService.Object,
            _mockThumbnailService.Object,
            _mockConfiguration.Object,
            _mockLogger.Object,
            _mockWebHostEnvironment.Object
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

    #region Favorites Tests

    [Fact]
    public async Task GetClips_FavoriteOnly_ReturnsOnlyFavorites()
    {
        // Arrange
        var favClip = new ClipBuilder().WithId(1).WithTitle("Fav").Build();
        favClip.IsFavorite = true;
        var normalClip = new ClipBuilder().WithId(2).WithTitle("Normal").Build();
        _context.Clips.AddRange(favClip, normalClip);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetClips(searchTerm: null, tagIds: null, subfolders: null, favoriteOnly: true);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var clips = okResult.Value.Should().BeAssignableTo<IEnumerable<ClipDto>>().Subject;
        clips.Should().HaveCount(1);
        clips.First().Title.Should().Be("Fav");
        clips.First().IsFavorite.Should().BeTrue();
    }

    [Fact]
    public async Task SetFavorite_TogglesFavoriteFlag()
    {
        // Arrange
        var clip = new ClipBuilder().WithId(1).WithTitle("Clip").Build();
        _context.Clips.Add(clip);
        await _context.SaveChangesAsync();

        // Act - set favorite true
        var setTrueResult = await _controller.SetFavorite(1, new ToggleFavoriteDto { IsFavorite = true });
        var okTrue = setTrueResult.Result.Should().BeOfType<OkObjectResult>().Subject;
        var dtoTrue = okTrue.Value.Should().BeOfType<ClipDto>().Subject;
        dtoTrue.IsFavorite.Should().BeTrue();

        // Act - set favorite false
        var setFalseResult = await _controller.SetFavorite(1, new ToggleFavoriteDto { IsFavorite = false });
        var okFalse = setFalseResult.Result.Should().BeOfType<OkObjectResult>().Subject;
        var dtoFalse = okFalse.Value.Should().BeOfType<ClipDto>().Subject;
        dtoFalse.IsFavorite.Should().BeFalse();
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

    // CreateClip tests removed as creation is no longer supported

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

        var dto = new UpdateClipDto
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
        var dto = new UpdateClipDto { LocationString = "test.mp4" };

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


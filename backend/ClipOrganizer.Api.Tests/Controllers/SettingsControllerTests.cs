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
using ClipOrganizer.Api.Tests.Helpers;

namespace ClipOrganizer.Api.Tests.Controllers;

public class SettingsControllerTests : IDisposable
{
    private readonly ClipDbContext _context;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger<SettingsController>> _mockLogger;
    private readonly SettingsController _controller;
    private readonly List<string> _tempDirectories = new();

    public SettingsControllerTests()
    {
        _context = TestHelpers.CreateInMemoryDbContext();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<SettingsController>>();

        _controller = new SettingsController(
            _context,
            _mockConfiguration.Object,
            _mockLogger.Object
        );
    }

    private string CreateTempDirectory()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        _tempDirectories.Add(tempDir);
        return tempDir;
    }

    public void Dispose()
    {
        _context.Dispose();
        foreach (var dir in _tempDirectories)
        {
            try
            {
                if (Directory.Exists(dir))
                    Directory.Delete(dir, true);
            }
            catch { }
        }
    }

    #region GetRootFolder Tests

    [Fact]
    public async Task GetRootFolder_DatabaseSettingExists_ReturnsDatabaseSetting()
    {
        // Arrange
        var setting = new SettingBuilder()
            .WithKey("VideoLibrary.RootFolder")
            .WithValue(@"C:\Videos")
            .Build();
        _context.Settings.Add(setting);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetRootFolder();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var dto = okResult.Value.Should().BeOfType<RootFolderSettingDto>().Subject;
        dto.RootFolderPath.Should().Be(@"C:\Videos");
    }

    [Fact]
    public async Task GetRootFolder_NoDatabaseSetting_FallsBackToAppSettings()
    {
        // Arrange
        _mockConfiguration.Setup(c => c["VideoLibrary:RootFolder"])
            .Returns(@"C:\AppSettingsVideos");

        // Act
        var result = await _controller.GetRootFolder();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var dto = okResult.Value.Should().BeOfType<RootFolderSettingDto>().Subject;
        dto.RootFolderPath.Should().Be(@"C:\AppSettingsVideos");
    }

    [Fact]
    public async Task GetRootFolder_NoConfiguration_ReturnsEmpty()
    {
        // Arrange
        _mockConfiguration.Setup(c => c["VideoLibrary:RootFolder"])
            .Returns((string?)null);

        // Act
        var result = await _controller.GetRootFolder();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var dto = okResult.Value.Should().BeOfType<RootFolderSettingDto>().Subject;
        dto.RootFolderPath.Should().BeEmpty();
    }

    #endregion

    #region SetRootFolder Tests

    [Fact]
    public async Task SetRootFolder_EmptyPath_ReturnsBadRequest()
    {
        // Arrange
        var dto = new RootFolderSettingDto
        {
            RootFolderPath = string.Empty
        };

        // Act
        var result = await _controller.SetRootFolder(dto);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task SetRootFolder_RelativePath_ReturnsBadRequest()
    {
        // Arrange
        var dto = new RootFolderSettingDto
        {
            RootFolderPath = "videos/clips"
        };

        // Act
        var result = await _controller.SetRootFolder(dto);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
        var badRequest = result.Result.As<BadRequestObjectResult>();
        badRequest.Value.Should().Be("Root folder path must be an absolute path");
    }

    [Fact]
    public async Task SetRootFolder_NonExistentDirectory_ReturnsBadRequest()
    {
        // Arrange
        var dto = new RootFolderSettingDto
        {
            RootFolderPath = @"C:\NonExistent\Directory"
        };

        // Act
        var result = await _controller.SetRootFolder(dto);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
        var badRequest = result.Result.As<BadRequestObjectResult>();
        badRequest.Value.Should().Be("Root folder does not exist");
    }

    [Fact]
    public async Task SetRootFolder_ValidPath_CreatesSetting()
    {
        // Arrange
        var tempDir = CreateTempDirectory();
        var dto = new RootFolderSettingDto
        {
            RootFolderPath = tempDir
        };

        // Act
        var result = await _controller.SetRootFolder(dto);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var responseDto = okResult.Value.Should().BeOfType<RootFolderSettingDto>().Subject;
        responseDto.RootFolderPath.Should().Be(tempDir);

        var settings = await _context.Settings.ToListAsync();
        settings.Should().HaveCount(1);
        settings[0].Key.Should().Be("VideoLibrary.RootFolder");
        settings[0].Value.Should().Be(tempDir);
    }

    [Fact]
    public async Task SetRootFolder_ExistingSetting_UpdatesSetting()
    {
        // Arrange
        var tempDir = CreateTempDirectory();
        var existingSetting = new SettingBuilder()
            .WithKey("VideoLibrary.RootFolder")
            .WithValue(@"C:\OldPath")
            .Build();
        _context.Settings.Add(existingSetting);
        await _context.SaveChangesAsync();

        var dto = new RootFolderSettingDto
        {
            RootFolderPath = tempDir
        };

        // Act
        var result = await _controller.SetRootFolder(dto);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var responseDto = okResult.Value.Should().BeOfType<RootFolderSettingDto>().Subject;
        responseDto.RootFolderPath.Should().Be(tempDir);

        var settings = await _context.Settings.ToListAsync();
        settings.Should().HaveCount(1);
        settings[0].Value.Should().Be(tempDir);
    }

    #endregion

    #region GetSettings Tests

    [Fact]
    public async Task GetSettings_ReturnsAllSettings()
    {
        // Arrange
        var setting1 = new SettingBuilder()
            .WithKey("Setting1")
            .WithValue("Value1")
            .Build();
        var setting2 = new SettingBuilder()
            .WithKey("Setting2")
            .WithValue("Value2")
            .Build();
        _context.Settings.AddRange(setting1, setting2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetSettings();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var settings = okResult.Value.Should().BeAssignableTo<IEnumerable<SettingDto>>().Subject.ToList();
        settings.Should().HaveCount(2);
        settings.Should().Contain(s => s.Key == "Setting1" && s.Value == "Value1");
        settings.Should().Contain(s => s.Key == "Setting2" && s.Value == "Value2");
    }

    [Fact]
    public async Task GetSettings_EmptyDatabase_ReturnsEmptyList()
    {
        // Act
        var result = await _controller.GetSettings();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var settings = okResult.Value.Should().BeAssignableTo<IEnumerable<SettingDto>>().Subject;
        settings.Should().BeEmpty();
    }

    #endregion
}


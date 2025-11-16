using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using ClipOrganizer.Api.Controllers;
using ClipOrganizer.Api.Data;
using ClipOrganizer.Api.DTOs;
using ClipOrganizer.Api.Models;
using ClipOrganizer.Api.Services;
using ClipOrganizer.Api.Tests.Helpers;

namespace ClipOrganizer.Api.Tests.Controllers;

public class SessionPlansControllerTests
{
    private readonly ClipDbContext _context;
    private readonly Mock<ISessionPlanService> _mockSessionPlanService;
    private readonly Mock<ILogger<SessionPlansController>> _mockLogger;
    private readonly SessionPlansController _controller;

    public SessionPlansControllerTests()
    {
        _context = TestHelpers.CreateInMemoryDbContext();
        _mockSessionPlanService = new Mock<ISessionPlanService>();
        _mockLogger = new Mock<ILogger<SessionPlansController>>();

        _controller = new SessionPlansController(
            _context,
            _mockSessionPlanService.Object,
            _mockLogger.Object
        );
    }

    #region GenerateSessionPlan Tests

    [Fact]
    public async Task GenerateSessionPlan_DurationZero_ReturnsBadRequest()
    {
        // Arrange
        var dto = new GenerateSessionPlanDto
        {
            DurationMinutes = 0,
            FocusAreas = new List<string>()
        };

        // Act
        var result = await _controller.GenerateSessionPlan(dto);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GenerateSessionPlan_NoClipsAvailable_ReturnsBadRequest()
    {
        // Arrange
        var dto = new GenerateSessionPlanDto
        {
            DurationMinutes = 60,
            FocusAreas = new List<string>()
        };

        // Act
        var result = await _controller.GenerateSessionPlan(dto);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
        var badRequest = result.Result.As<BadRequestObjectResult>();
        badRequest.Value.Should().Be("No clips available in the library");
    }

    [Fact]
    public async Task GenerateSessionPlan_ValidRequest_CallsService()
    {
        // Arrange
        var tag = new TagBuilder().WithId(1).WithCategory(TagCategory.SkillTactic).WithValue("Flick").Build();
        var clip = new ClipBuilder().WithId(1).WithTitle("Clip 1").WithTags(tag).Build();
        _context.Tags.Add(tag);
        _context.Clips.Add(clip);
        await _context.SaveChangesAsync();

        var dto = new GenerateSessionPlanDto
        {
            DurationMinutes = 60,
            FocusAreas = new List<string> { "Flick" }
        };

        var expectedPlan = new SessionPlanDto
        {
            Title = "Test Plan",
            Summary = "Test Summary",
            ClipIds = new List<int> { 1 },
            CreatedDate = DateTime.UtcNow
        };

        _mockSessionPlanService.Setup(x => x.GenerateSessionPlanAsync(dto, It.IsAny<List<Clip>>()))
            .ReturnsAsync(expectedPlan);

        // Act
        var result = await _controller.GenerateSessionPlan(dto);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var plan = okResult.Value.Should().BeOfType<SessionPlanDto>().Subject;
        plan.Title.Should().Be("Test Plan");
        _mockSessionPlanService.Verify(x => x.GenerateSessionPlanAsync(dto, It.IsAny<List<Clip>>()), Times.Once);
    }

    #endregion

    #region CreateSessionPlan Tests

    [Fact]
    public async Task CreateSessionPlan_EmptyTitle_ReturnsBadRequest()
    {
        // Arrange
        var dto = new CreateSessionPlanDto
        {
            Title = string.Empty,
            ClipIds = new List<int> { 1 }
        };

        // Act
        var result = await _controller.CreateSessionPlan(dto);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CreateSessionPlan_NoClips_ReturnsBadRequest()
    {
        // Arrange
        var dto = new CreateSessionPlanDto
        {
            Title = "Test Plan",
            ClipIds = new List<int>()
        };

        // Act
        var result = await _controller.CreateSessionPlan(dto);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CreateSessionPlan_InvalidClipId_ReturnsBadRequest()
    {
        // Arrange
        var dto = new CreateSessionPlanDto
        {
            Title = "Test Plan",
            ClipIds = new List<int> { 999 }
        };

        // Act
        var result = await _controller.CreateSessionPlan(dto);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CreateSessionPlan_ValidRequest_CreatesPlan()
    {
        // Arrange
        var clip1 = new ClipBuilder().WithId(1).WithTitle("Clip 1").Build();
        var clip2 = new ClipBuilder().WithId(2).WithTitle("Clip 2").Build();
        _context.Clips.AddRange(clip1, clip2);
        await _context.SaveChangesAsync();

        var dto = new CreateSessionPlanDto
        {
            Title = "Test Plan",
            Summary = "Test Summary",
            ClipIds = new List<int> { 1, 2 }
        };

        // Act
        var result = await _controller.CreateSessionPlan(dto);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var planDto = createdResult.Value.Should().BeOfType<SessionPlanDto>().Subject;
        planDto.Title.Should().Be("Test Plan");
        planDto.ClipIds.Should().HaveCount(2);

        var plans = await _context.SessionPlans.Include(sp => sp.Clips).ToListAsync();
        plans.Should().HaveCount(1);
        plans[0].Clips.Should().HaveCount(2);
    }

    #endregion

    #region GetSessionPlans Tests

    [Fact]
    public async Task GetSessionPlans_ReturnsAllPlans()
    {
        // Arrange
        var clip = new ClipBuilder().WithId(1).WithTitle("Clip 1").Build();
        _context.Clips.Add(clip);
        
        var plan1 = new SessionPlanBuilder()
            .WithId(1)
            .WithTitle("Plan 1")
            .WithCreatedDate(DateTime.UtcNow.AddDays(-1))
            .WithClips(clip)
            .Build();
        var plan2 = new SessionPlanBuilder()
            .WithId(2)
            .WithTitle("Plan 2")
            .WithCreatedDate(DateTime.UtcNow)
            .WithClips(clip)
            .Build();
        
        _context.SessionPlans.AddRange(plan1, plan2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetSessionPlans();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var plans = okResult.Value.Should().BeAssignableTo<IEnumerable<SessionPlanDto>>().Subject.ToList();
        plans.Should().HaveCount(2);
        // Should be ordered by date descending (newest first)
        plans[0].Title.Should().Be("Plan 2");
        plans[1].Title.Should().Be("Plan 1");
    }

    #endregion

    #region GetSessionPlan Tests

    [Fact]
    public async Task GetSessionPlan_ExistingId_ReturnsPlan()
    {
        // Arrange
        var clip = new ClipBuilder().WithId(1).WithTitle("Clip 1").Build();
        _context.Clips.Add(clip);
        
        var plan = new SessionPlanBuilder()
            .WithId(1)
            .WithTitle("Test Plan")
            .WithClips(clip)
            .Build();
        _context.SessionPlans.Add(plan);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetSessionPlan(1);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var planDto = okResult.Value.Should().BeOfType<SessionPlanDto>().Subject;
        planDto.Id.Should().Be(1);
        planDto.Title.Should().Be("Test Plan");
        planDto.ClipIds.Should().Contain(1);
    }

    [Fact]
    public async Task GetSessionPlan_NonExistentId_ReturnsNotFound()
    {
        // Act
        var result = await _controller.GetSessionPlan(999);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    #endregion

    #region DeleteSessionPlan Tests

    [Fact]
    public async Task DeleteSessionPlan_ExistingPlan_DeletesSuccessfully()
    {
        // Arrange
        var plan = new SessionPlanBuilder()
            .WithId(1)
            .WithTitle("To Delete")
            .Build();
        _context.SessionPlans.Add(plan);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.DeleteSessionPlan(1);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        var plans = await _context.SessionPlans.ToListAsync();
        plans.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteSessionPlan_NonExistentPlan_ReturnsNotFound()
    {
        // Act
        var result = await _controller.DeleteSessionPlan(999);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    #endregion
}


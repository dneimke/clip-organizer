using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClipOrganizer.Api.Controllers;
using ClipOrganizer.Api.Data;
using ClipOrganizer.Api.DTOs;
using ClipOrganizer.Api.Models;
using ClipOrganizer.Api.Tests.Helpers;

namespace ClipOrganizer.Api.Tests.Controllers;

public class TagsControllerTests
{
    private readonly ClipDbContext _context;
    private readonly TagsController _controller;

    public TagsControllerTests()
    {
        _context = TestHelpers.CreateInMemoryDbContext();
        _controller = new TagsController(_context);
    }

    #region GetTags Tests

    [Fact]
    public async Task GetTags_ReturnsTagsGroupedByCategory()
    {
        // Arrange
        var tag1 = new TagBuilder()
            .WithId(1)
            .WithCategory(TagCategory.SkillTactic)
            .WithValue("Flick")
            .Build();
        var tag2 = new TagBuilder()
            .WithId(2)
            .WithCategory(TagCategory.SkillTactic)
            .WithValue("PC Attack")
            .Build();
        var tag3 = new TagBuilder()
            .WithId(3)
            .WithCategory(TagCategory.FieldArea)
            .WithValue("Midfield")
            .Build();
        
        _context.Tags.AddRange(tag1, tag2, tag3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetTags();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var groupedTags = okResult.Value.Should().BeAssignableTo<Dictionary<string, List<TagDto>>>().Subject;
        groupedTags.Should().HaveCount(2);
        groupedTags["SkillTactic"].Should().HaveCount(2);
        groupedTags["FieldArea"].Should().HaveCount(1);
    }

    [Fact]
    public async Task GetTags_EmptyDatabase_ReturnsEmptyDictionary()
    {
        // Act
        var result = await _controller.GetTags();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var groupedTags = okResult.Value.Should().BeAssignableTo<Dictionary<string, List<TagDto>>>().Subject;
        groupedTags.Should().BeEmpty();
    }

    #endregion

    #region GetTagCategories Tests

    [Fact]
    public void GetTagCategories_ReturnsAllCategories()
    {
        // Act
        var result = _controller.GetTagCategories();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var categories = okResult.Value.Should().BeAssignableTo<IEnumerable<string>>().Subject.ToList();
        categories.Should().Contain("SkillTactic");
        categories.Should().Contain("FieldArea");
        categories.Should().Contain("PlayerRole");
        categories.Should().Contain("OutcomeQuality");
    }

    #endregion

    #region CreateTag Tests

    [Fact]
    public async Task CreateTag_ValidTag_CreatesTag()
    {
        // Arrange
        var dto = new CreateTagDto
        {
            Category = "SkillTactic",
            Value = "New Flick"
        };

        // Act
        var result = await _controller.CreateTag(dto);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var tagDto = createdResult.Value.Should().BeOfType<TagDto>().Subject;
        tagDto.Category.Should().Be("SkillTactic");
        tagDto.Value.Should().Be("New Flick");

        var tags = await _context.Tags.ToListAsync();
        tags.Should().HaveCount(1);
    }

    [Fact]
    public async Task CreateTag_DuplicateTag_ReturnsExistingTag()
    {
        // Arrange
        var existingTag = new TagBuilder()
            .WithId(1)
            .WithCategory(TagCategory.SkillTactic)
            .WithValue("Flick")
            .Build();
        _context.Tags.Add(existingTag);
        await _context.SaveChangesAsync();

        var dto = new CreateTagDto
        {
            Category = "SkillTactic",
            Value = "Flick"
        };

        // Act
        var result = await _controller.CreateTag(dto);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var tagDto = okResult.Value.Should().BeOfType<TagDto>().Subject;
        tagDto.Id.Should().Be(1);
        tagDto.Value.Should().Be("Flick");

        // Should not create duplicate
        var tags = await _context.Tags.ToListAsync();
        tags.Should().HaveCount(1);
    }

    [Fact]
    public async Task CreateTag_InvalidCategory_ReturnsBadRequest()
    {
        // Arrange
        var dto = new CreateTagDto
        {
            Category = "InvalidCategory",
            Value = "Test"
        };

        // Act
        var result = await _controller.CreateTag(dto);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
        var badRequest = result.Result.As<BadRequestObjectResult>();
        badRequest.Value.Should().BeAssignableTo<string>().Which.Should().Contain("Invalid category");
    }

    #endregion
}


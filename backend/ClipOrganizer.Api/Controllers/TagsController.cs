using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClipOrganizer.Api.Data;
using ClipOrganizer.Api.DTOs;
using ClipOrganizer.Api.Models;

namespace ClipOrganizer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TagsController : ControllerBase
{
    private readonly ClipDbContext _context;

    public TagsController(ClipDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<Dictionary<string, List<TagDto>>>> GetTags()
    {
        var tags = await _context.Tags
            .OrderBy(t => t.Category)
            .ThenBy(t => t.Value)
            .ToListAsync();

        var groupedTags = tags
            .GroupBy(t => t.Category.ToString())
            .ToDictionary(
                g => g.Key,
                g => g.Select(t => new TagDto
                {
                    Id = t.Id,
                    Category = t.Category.ToString(),
                    Value = t.Value
                }).ToList()
            );

        return Ok(groupedTags);
    }

    [HttpGet("categories")]
    public ActionResult<IEnumerable<string>> GetTagCategories()
    {
        var categories = Enum.GetValues<TagCategory>()
            .Select(c => c.ToString())
            .ToList();

        return Ok(categories);
    }

    [HttpPost]
    public async Task<ActionResult<TagDto>> CreateTag([FromBody] CreateTagDto dto)
    {
        if (!Enum.TryParse<TagCategory>(dto.Category, out var category))
        {
            return BadRequest($"Invalid category: {dto.Category}");
        }

        // Check if tag already exists
        var existingTag = await _context.Tags
            .FirstOrDefaultAsync(t => t.Category == category && t.Value == dto.Value);

        if (existingTag != null)
        {
            return Ok(new TagDto
            {
                Id = existingTag.Id,
                Category = existingTag.Category.ToString(),
                Value = existingTag.Value
            });
        }

        var tag = new Tag
        {
            Category = category,
            Value = dto.Value
        };

        _context.Tags.Add(tag);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetTags), new { id = tag.Id }, new TagDto
        {
            Id = tag.Id,
            Category = tag.Category.ToString(),
            Value = tag.Value
        });
    }
}


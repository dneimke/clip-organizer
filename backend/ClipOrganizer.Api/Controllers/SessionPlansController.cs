using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClipOrganizer.Api.Data;
using ClipOrganizer.Api.DTOs;
using ClipOrganizer.Api.Models;
using ClipOrganizer.Api.Services;

namespace ClipOrganizer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SessionPlansController : ControllerBase
{
    private readonly ClipDbContext _context;
    private readonly ISessionPlanService _sessionPlanService;
    private readonly ILogger<SessionPlansController> _logger;

    public SessionPlansController(
        ClipDbContext context,
        ISessionPlanService sessionPlanService,
        ILogger<SessionPlansController> logger)
    {
        _context = context;
        _sessionPlanService = sessionPlanService;
        _logger = logger;
    }

    [HttpPost("generate")]
    public async Task<ActionResult<SessionPlanDto>> GenerateSessionPlan([FromBody] GenerateSessionPlanDto dto)
    {
        if (dto.DurationMinutes <= 0)
        {
            return BadRequest("Duration must be greater than 0");
        }

        try
        {
            // Get all clips with tags
            var allClips = await _context.Clips
                .Include(c => c.Tags)
                .ToListAsync();

            if (!allClips.Any())
            {
                return BadRequest("No clips available in the library");
            }

            // Filter clips by focus areas if provided
            List<Clip> filteredClips;
            if (dto.FocusAreas != null && dto.FocusAreas.Any())
            {
                // Load all tags into memory first to avoid LINQ translation issues
                var allTags = await _context.Tags.ToListAsync();
                
                // Find tags that match the focus areas (exact match with space normalization, case-insensitive)
                // Normalize spaces for comparison (e.g., "Goal Keeper" matches "Goalkeeper" and vice versa)
                var matchingTagIds = allTags
                    .Where(t => dto.FocusAreas.Any(fa =>
                    {
                        // Normalize both strings by removing spaces and comparing case-insensitively
                        var normalizedTag = t.Value.Replace(" ", "").Replace("-", "");
                        var normalizedFocusArea = fa.Replace(" ", "").Replace("-", "");
                        
                        return normalizedTag.Equals(normalizedFocusArea, StringComparison.OrdinalIgnoreCase) ||
                               t.Value.Equals(fa, StringComparison.OrdinalIgnoreCase);
                    }))
                    .Select(t => t.Id)
                    .ToList();

                if (!matchingTagIds.Any())
                {
                    return BadRequest($"No tags found matching the selected focus areas: {string.Join(", ", dto.FocusAreas)}");
                }

                // Filter clips to only those that have at least one matching tag
                // Also exclude unclassified clips (clips with no tags)
                filteredClips = allClips
                    .Where(c => c.Tags.Any() && c.Tags.Any(t => matchingTagIds.Contains(t.Id)))
                    .ToList();

                if (!filteredClips.Any())
                {
                    return BadRequest($"No clips found with tags matching the focus areas: {string.Join(", ", dto.FocusAreas)}");
                }
            }
            else
            {
                // If no focus areas specified, use all classified clips (exclude unclassified)
                filteredClips = allClips
                    .Where(c => c.Tags.Any())
                    .ToList();
                
                if (!filteredClips.Any())
                {
                    return BadRequest("No classified clips available in the library. Please classify some clips first.");
                }
            }

            // Generate plan using AI service
            var plan = await _sessionPlanService.GenerateSessionPlanAsync(dto, filteredClips);

            return Ok(plan);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating session plan: {Message}", ex.Message);
            return StatusCode(500, $"An error occurred while generating the session plan: {ex.Message}");
        }
    }

    [HttpPost]
    public async Task<ActionResult<SessionPlanDto>> CreateSessionPlan([FromBody] CreateSessionPlanDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
        {
            return BadRequest("Title is required");
        }

        if (dto.ClipIds == null || !dto.ClipIds.Any())
        {
            return BadRequest("At least one clip is required");
        }

        try
        {
            // Verify all clips exist
            var clips = await _context.Clips
                .Where(c => dto.ClipIds.Contains(c.Id))
                .ToListAsync();

            if (clips.Count != dto.ClipIds.Count)
            {
                return BadRequest("One or more clip IDs are invalid");
            }

            var sessionPlan = new SessionPlan
            {
                Title = dto.Title,
                Summary = dto.Summary ?? string.Empty,
                CreatedDate = DateTime.UtcNow,
                Clips = clips
            };

            _context.SessionPlans.Add(sessionPlan);
            await _context.SaveChangesAsync();

            var result = new SessionPlanDto
            {
                Id = sessionPlan.Id,
                Title = sessionPlan.Title,
                Summary = sessionPlan.Summary,
                CreatedDate = sessionPlan.CreatedDate,
                ClipIds = sessionPlan.Clips.Select(c => c.Id).ToList()
            };

            return CreatedAtAction(nameof(GetSessionPlan), new { id = sessionPlan.Id }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating session plan");
            return StatusCode(500, "An error occurred while creating the session plan");
        }
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SessionPlanDto>>> GetSessionPlans()
    {
        try
        {
            var plans = await _context.SessionPlans
                .Include(sp => sp.Clips)
                .OrderByDescending(sp => sp.CreatedDate)
                .ToListAsync();

            var result = plans.Select(sp => new SessionPlanDto
            {
                Id = sp.Id,
                Title = sp.Title,
                Summary = sp.Summary,
                CreatedDate = sp.CreatedDate,
                ClipIds = sp.Clips.Select(c => c.Id).ToList()
            });

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching session plans");
            return StatusCode(500, "An error occurred while fetching session plans");
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SessionPlanDto>> GetSessionPlan(int id)
    {
        try
        {
            var plan = await _context.SessionPlans
                .Include(sp => sp.Clips)
                .ThenInclude(c => c.Tags)
                .FirstOrDefaultAsync(sp => sp.Id == id);

            if (plan == null)
            {
                return NotFound();
            }

            var result = new SessionPlanDto
            {
                Id = plan.Id,
                Title = plan.Title,
                Summary = plan.Summary,
                CreatedDate = plan.CreatedDate,
                ClipIds = plan.Clips.Select(c => c.Id).ToList()
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching session plan");
            return StatusCode(500, "An error occurred while fetching the session plan");
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSessionPlan(int id)
    {
        try
        {
            var plan = await _context.SessionPlans.FindAsync(id);

            if (plan == null)
            {
                return NotFound();
            }

            _context.SessionPlans.Remove(plan);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting session plan");
            return StatusCode(500, "An error occurred while deleting the session plan");
        }
    }

    // Update a session plan (rename or update summary)
    [HttpPut("{id}")]
    public async Task<ActionResult<SessionPlanDto>> UpdateSessionPlan(int id, [FromBody] UpdateSessionPlanDto dto)
    {
        if (dto == null || (string.IsNullOrWhiteSpace(dto.Title) && string.IsNullOrWhiteSpace(dto.Summary)))
        {
            return BadRequest("Provide a new title and/or summary");
        }

        try
        {
            var plan = await _context.SessionPlans
                .Include(sp => sp.Clips)
                .FirstOrDefaultAsync(sp => sp.Id == id);

            if (plan == null)
            {
                return NotFound();
            }

            if (!string.IsNullOrWhiteSpace(dto.Title))
            {
                plan.Title = dto.Title.Trim();
            }
            if (dto.Summary != null)
            {
                plan.Summary = dto.Summary;
            }

            await _context.SaveChangesAsync();

            var result = new SessionPlanDto
            {
                Id = plan.Id,
                Title = plan.Title,
                Summary = plan.Summary,
                CreatedDate = plan.CreatedDate,
                ClipIds = plan.Clips.Select(c => c.Id).ToList()
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating session plan");
            return StatusCode(500, "An error occurred while updating the session plan");
        }
    }

    // Add a clip to a session plan (collection)
    [HttpPost("{id}/clips")]
    public async Task<ActionResult<SessionPlanDto>> AddClipToSessionPlan(int id, [FromBody] AddClipToSessionPlanDto dto)
    {
        if (dto == null || dto.ClipId <= 0)
        {
            return BadRequest("A valid clipId is required");
        }

        try
        {
            var plan = await _context.SessionPlans
                .Include(sp => sp.Clips)
                .FirstOrDefaultAsync(sp => sp.Id == id);

            if (plan == null)
            {
                return NotFound("Session plan not found");
            }

            var clip = await _context.Clips.FindAsync(dto.ClipId);
            if (clip == null)
            {
                return BadRequest("Invalid clipId");
            }

            if (plan.Clips.Any(c => c.Id == dto.ClipId))
            {
                return Conflict("Clip already exists in this collection");
            }

            plan.Clips.Add(clip);
            await _context.SaveChangesAsync();

            var result = new SessionPlanDto
            {
                Id = plan.Id,
                Title = plan.Title,
                Summary = plan.Summary,
                CreatedDate = plan.CreatedDate,
                ClipIds = plan.Clips.Select(c => c.Id).ToList()
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding clip to session plan");
            return StatusCode(500, "An error occurred while adding the clip to the session plan");
        }
    }

    // Remove a clip from a session plan (collection)
    [HttpDelete("{id}/clips/{clipId}")]
    public async Task<IActionResult> RemoveClipFromSessionPlan(int id, int clipId)
    {
        if (clipId <= 0)
        {
            return BadRequest("A valid clipId is required");
        }

        try
        {
            var plan = await _context.SessionPlans
                .Include(sp => sp.Clips)
                .FirstOrDefaultAsync(sp => sp.Id == id);

            if (plan == null)
            {
                return NotFound("Session plan not found");
            }

            var existing = plan.Clips.FirstOrDefault(c => c.Id == clipId);
            if (existing != null)
            {
                plan.Clips.Remove(existing);
                await _context.SaveChangesAsync();
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing clip from session plan");
            return StatusCode(500, "An error occurred while removing the clip from the session plan");
        }
    }
}


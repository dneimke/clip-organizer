using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClipOrganizer.Api.Data;
using ClipOrganizer.Api.DTOs;
using ClipOrganizer.Api.Models;
using ClipOrganizer.Api.Services;
using System.IO;

namespace ClipOrganizer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClipsController : ControllerBase
{
    private readonly ClipDbContext _context;
    private readonly IClipValidationService _validationService;
    private readonly IYouTubeService _youtubeService;
    private readonly IAIClipGenerationService _aiGenerationService;
    private readonly ISyncService _syncService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ClipsController> _logger;
    private const string RootFolderKey = "VideoLibrary.RootFolder";

    public ClipsController(
        ClipDbContext context,
        IClipValidationService validationService,
        IYouTubeService youtubeService,
        IAIClipGenerationService aiGenerationService,
        ISyncService syncService,
        IConfiguration configuration,
        ILogger<ClipsController> logger)
    {
        _context = context;
        _validationService = validationService;
        _youtubeService = youtubeService;
        _aiGenerationService = aiGenerationService;
        _syncService = syncService;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ClipDto>>> GetClips(
        [FromQuery] string? searchTerm, 
        [FromQuery] List<int>? tagIds,
        [FromQuery] string? sortBy = "dateAdded",
        [FromQuery] string? sortOrder = "desc",
        [FromQuery] bool unclassifiedOnly = false)
    {
        var query = _context.Clips.Include(c => c.Tags).AsQueryable();

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(c => c.Title.Contains(searchTerm));
        }

        // Apply tag filters
        if (tagIds != null && tagIds.Any())
        {
            query = query.Where(c => c.Tags.Any(t => tagIds.Contains(t.Id)));
        }

        // Apply sorting
        var isAscending = sortOrder?.ToLower() == "asc";
        switch (sortBy?.ToLower())
        {
            case "title":
                query = isAscending 
                    ? query.OrderBy(c => c.Title) 
                    : query.OrderByDescending(c => c.Title);
                break;
            case "dateadded":
            default:
                // Use Id as proxy for date added (higher Id = more recent)
                query = isAscending 
                    ? query.OrderBy(c => c.Id) 
                    : query.OrderByDescending(c => c.Id);
                break;
        }

        var clips = await query.ToListAsync();

        // Apply unclassified filter in memory (after loading from DB)
        if (unclassifiedOnly)
        {
            clips = clips.Where(c => IsUnclassified(c)).ToList();
        }

        return Ok(clips.Select(c => MapToDto(c)));
    }

    [HttpGet("unclassified")]
    public async Task<ActionResult<IEnumerable<ClipDto>>> GetUnclassifiedClips()
    {
        try
        {
            var clips = await _context.Clips
                .Include(c => c.Tags)
                .ToListAsync();

            var unclassifiedClips = clips.Where(c => IsUnclassified(c))
                .OrderByDescending(c => c.Id)
                .ToList();

            return Ok(unclassifiedClips.Select(c => MapToDto(c)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching unclassified clips");
            return StatusCode(500, "An error occurred while fetching unclassified clips");
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ClipDto>> GetClip(int id)
    {
        var clip = await _context.Clips
            .Include(c => c.Tags)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (clip == null)
        {
            return NotFound();
        }

        return Ok(MapToDto(clip));
    }

    [HttpGet("{id}/video")]
    public async Task<IActionResult> GetClipVideo(int id)
    {
        var clip = await _context.Clips.FindAsync(id);
        
        if (clip == null)
        {
            return NotFound();
        }
        
        // Only serve videos for local clips
        if (clip.StorageType != StorageType.Local)
        {
            return BadRequest("This endpoint only serves local clips");
        }
        
        // Validate the file path
        if (!_validationService.ValidateLocalPath(clip.LocationString))
        {
            return NotFound("Video file not found");
        }
        
        // Check if file exists
        if (!System.IO.File.Exists(clip.LocationString))
        {
            return NotFound("Video file not found");
        }
        
        // Determine content type based on file extension
        var extension = Path.GetExtension(clip.LocationString).ToLowerInvariant();
        var contentType = extension switch
        {
            ".mp4" => "video/mp4",
            ".webm" => "video/webm",
            ".ogg" => "video/ogg",
            ".mov" => "video/quicktime",
            ".avi" => "video/x-msvideo",
            _ => "application/octet-stream"
        };
        
        // Enable range requests for video seeking
        var fileStream = new FileStream(clip.LocationString, FileMode.Open, FileAccess.Read, FileShare.Read);
        
        Response.Headers["Accept-Ranges"] = "bytes";
        
        return new FileStreamResult(fileStream, contentType)
        {
            EnableRangeProcessing = true
        };
    }

    [HttpPost("generate-metadata")]
    public async Task<ActionResult<GenerateMetadataResponseDto>> GenerateMetadata([FromBody] GenerateMetadataDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Notes))
        {
            return BadRequest("Notes are required");
        }

        try
        {
            // Get all available tags
            var tags = await _context.Tags
                .OrderBy(t => t.Category)
                .ThenBy(t => t.Value)
                .ToListAsync();

            var availableTags = tags.Select(t => new AvailableTag
            {
                Id = t.Id,
                Category = t.Category.ToString(),
                Value = t.Value
            }).ToList();

            // Generate metadata using AI
            var result = await _aiGenerationService.GenerateClipMetadataAsync(dto.Notes, availableTags);

            return Ok(new GenerateMetadataResponseDto
            {
                Title = result.Title,
                Description = result.Description,
                SuggestedTagIds = result.SuggestedTagIds,
                SuggestedNewTags = result.SuggestedNewTags
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating metadata");
            return StatusCode(500, "An error occurred while generating metadata");
        }
    }

    [HttpPost("bulk-upload")]
    public async Task<ActionResult<BulkUploadResponseDto>> BulkUpload([FromBody] BulkUploadRequestDto request)
    {
        if (request.FilePaths == null || !request.FilePaths.Any())
        {
            return BadRequest("FilePaths are required");
        }

        var response = new BulkUploadResponseDto();
        var validVideoExtensions = new[] { ".mp4", ".webm", ".mov", ".avi", ".ogg" };

        // Get existing location strings for duplicate checking (normalize to lowercase for Windows)
        var existingLocations = await _context.Clips
            .Select(c => c.LocationString.ToLowerInvariant())
            .ToListAsync();

        foreach (var filePath in request.FilePaths)
        {
            try
            {
                // Validate file path
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    response.Failures.Add(new BulkUploadErrorDto
                    {
                        FilePath = filePath ?? string.Empty,
                        ErrorMessage = "File path is empty"
                    });
                    continue;
                }

                // Normalize path for duplicate checking (Windows is case-insensitive)
                var normalizedPath = filePath.ToLowerInvariant();

                // Check for duplicates
                if (existingLocations.Contains(normalizedPath))
                {
                    response.Failures.Add(new BulkUploadErrorDto
                    {
                        FilePath = filePath,
                        ErrorMessage = "A clip with this file path already exists"
                    });
                    continue;
                }

                // Validate file exists
                if (!_validationService.ValidateLocalPath(filePath))
                {
                    response.Failures.Add(new BulkUploadErrorDto
                    {
                        FilePath = filePath,
                        ErrorMessage = "File does not exist or is not a valid path"
                    });
                    continue;
                }

                // Validate video file extension
                var extension = Path.GetExtension(filePath).ToLowerInvariant();
                if (!validVideoExtensions.Contains(extension))
                {
                    response.Failures.Add(new BulkUploadErrorDto
                    {
                        FilePath = filePath,
                        ErrorMessage = $"Invalid video file extension. Supported: {string.Join(", ", validVideoExtensions)}"
                    });
                    continue;
                }

                // Create clip
                var title = Path.GetFileNameWithoutExtension(filePath);
                var clip = new Clip
                {
                    Title = title,
                    Description = string.Empty,
                    StorageType = StorageType.Local,
                    LocationString = filePath,
                    Duration = 0
                };

                _context.Clips.Add(clip);
                await _context.SaveChangesAsync();

                // Add to existing locations to prevent duplicates in same batch
                existingLocations.Add(normalizedPath);

                response.Successes.Add(new BulkUploadItemDto
                {
                    ClipId = clip.Id,
                    FilePath = filePath,
                    Title = title
                });
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UNIQUE constraint") == true)
            {
                // Handle unique constraint violation (race condition)
                response.Failures.Add(new BulkUploadErrorDto
                {
                    FilePath = filePath,
                    ErrorMessage = "A clip with this file path already exists"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading clip: {FilePath}", filePath);
                response.Failures.Add(new BulkUploadErrorDto
                {
                    FilePath = filePath,
                    ErrorMessage = $"An error occurred: {ex.Message}"
                });
            }
        }

        return Ok(response);
    }

    [HttpPut("bulk-update")]
    public async Task<ActionResult<BulkUpdateResponseDto>> BulkUpdate([FromBody] BulkUpdateRequestDto request)
    {
        if (request.Updates == null || !request.Updates.Any())
        {
            return BadRequest("Updates are required");
        }

        var response = new BulkUpdateResponseDto();

        foreach (var update in request.Updates)
        {
            try
            {
                var clip = await _context.Clips
                    .Include(c => c.Tags)
                    .FirstOrDefaultAsync(c => c.Id == update.ClipId);

                if (clip == null)
                {
                    response.Failures.Add(new BulkUpdateErrorDto
                    {
                        ClipId = update.ClipId,
                        ErrorMessage = "Clip not found"
                    });
                    continue;
                }

                // Update title if provided
                if (!string.IsNullOrWhiteSpace(update.Title))
                {
                    clip.Title = update.Title;
                }

                // Update description if provided
                if (update.Description != null)
                {
                    clip.Description = update.Description;
                }

                // Update tags if provided
                if (update.TagIds != null)
                {
                    clip.Tags.Clear();
                    if (update.TagIds.Any())
                    {
                        var tags = await _context.Tags
                            .Where(t => update.TagIds.Contains(t.Id))
                            .ToListAsync();

                        foreach (var tag in tags)
                        {
                            clip.Tags.Add(tag);
                        }
                    }
                }

                // Create and add new tags if provided
                if (update.NewTags != null && update.NewTags.Any())
                {
                    var newTags = await GetOrCreateTagsAsync(update.NewTags);
                    foreach (var tag in newTags)
                    {
                        clip.Tags.Add(tag);
                    }
                }

                await _context.SaveChangesAsync();
                response.SuccessCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating clip: {ClipId}", update.ClipId);
                response.Failures.Add(new BulkUpdateErrorDto
                {
                    ClipId = update.ClipId,
                    ErrorMessage = $"An error occurred: {ex.Message}"
                });
            }
        }

        return Ok(response);
    }

    [HttpPost("sync")]
    public async Task<ActionResult<SyncResponseDto>> Sync([FromBody] SyncRequestDto? request)
    {
        string rootFolderPath;

        // If root folder path is provided in request, use it
        if (request != null && !string.IsNullOrWhiteSpace(request.RootFolderPath))
        {
            rootFolderPath = request.RootFolderPath;
        }
        else
        {
            // Otherwise, try to get from database settings
            var setting = await _context.Settings
                .FirstOrDefaultAsync(s => s.Key == RootFolderKey);

            if (setting != null && !string.IsNullOrWhiteSpace(setting.Value))
            {
                rootFolderPath = setting.Value;
            }
            else
            {
                // Fall back to appsettings.json
                var appSettingsPath = _configuration["VideoLibrary:RootFolder"];
                if (!string.IsNullOrWhiteSpace(appSettingsPath))
                {
                    rootFolderPath = appSettingsPath;
                }
                else
                {
                    return BadRequest("Root folder path is not configured. Please configure it in Settings or provide it in the request.");
                }
            }
        }

        try
        {
            // Validate root folder path exists and is accessible
            if (!Path.IsPathRooted(rootFolderPath))
            {
                return BadRequest("Root folder path must be an absolute path");
            }

            if (!Directory.Exists(rootFolderPath))
            {
                return BadRequest("Root folder does not exist");
            }

            var result = await _syncService.SyncAsync(rootFolderPath);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during sync operation");
            return StatusCode(500, "An error occurred while syncing clips");
        }
    }

    [HttpGet("sync-preview")]
    public async Task<ActionResult<SyncPreviewResponseDto>> SyncPreview([FromQuery] string? rootFolderPath)
    {
        string pathToUse;

        // If root folder path is provided in query, use it
        if (!string.IsNullOrWhiteSpace(rootFolderPath))
        {
            pathToUse = rootFolderPath;
        }
        else
        {
            // Otherwise, try to get from database settings
            var setting = await _context.Settings
                .FirstOrDefaultAsync(s => s.Key == RootFolderKey);

            if (setting != null && !string.IsNullOrWhiteSpace(setting.Value))
            {
                pathToUse = setting.Value;
            }
            else
            {
                // Fall back to appsettings.json
                var appSettingsPath = _configuration["VideoLibrary:RootFolder"];
                if (!string.IsNullOrWhiteSpace(appSettingsPath))
                {
                    pathToUse = appSettingsPath;
                }
                else
                {
                    return BadRequest("Root folder path is not configured. Please configure it in Settings or provide it in the query parameter.");
                }
            }
        }

        try
        {
            var result = await _syncService.PreviewSyncAsync(pathToUse);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during preview sync operation");
            return StatusCode(500, "An error occurred while previewing sync");
        }
    }

    [HttpPost("selective-sync")]
    public async Task<ActionResult<SyncResponseDto>> SelectiveSync([FromBody] SelectiveSyncRequestDto request)
    {
        try
        {
            var result = await _syncService.SelectiveSyncAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during selective sync operation");
            return StatusCode(500, "An error occurred while performing selective sync");
        }
    }

    [HttpPost]
    public async Task<ActionResult<ClipDto>> CreateClip([FromBody] CreateClipDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.LocationString))
        {
            return BadRequest("LocationString is required");
        }

        try
        {
            var storageType = _validationService.DetermineStorageType(dto.LocationString);
            string title;
            int duration;
            string locationString = dto.LocationString;

            if (storageType == StorageType.YouTube)
            {
                if (!_validationService.ValidateYouTubeUrl(dto.LocationString))
                {
                    return BadRequest("Invalid YouTube URL");
                }

                var (videoTitle, videoDuration, videoId) = await _youtubeService.GetVideoMetadataAsync(dto.LocationString);
                // Use provided title if available, otherwise use video title
                title = !string.IsNullOrWhiteSpace(dto.Title) ? dto.Title : videoTitle;
                duration = videoDuration;
                locationString = videoId; // Store just the video ID
            }
            else
            {
                if (!_validationService.ValidateLocalPath(dto.LocationString))
                {
                    return BadRequest("Local file path does not exist");
                }

                // Use provided title if available, otherwise extract filename as title
                title = !string.IsNullOrWhiteSpace(dto.Title) ? dto.Title : Path.GetFileNameWithoutExtension(dto.LocationString);
                
                // For local files, we can't determine duration without additional libraries
                // Setting to 0 as placeholder
                duration = 0;
            }

            var clip = new Clip
            {
                Title = title,
                Description = dto.Description ?? string.Empty,
                StorageType = storageType,
                LocationString = locationString,
                Duration = duration
            };

            // Add existing tags if provided
            if (dto.TagIds != null && dto.TagIds.Any())
            {
                var tags = await _context.Tags
                    .Where(t => dto.TagIds.Contains(t.Id))
                    .ToListAsync();

                foreach (var tag in tags)
                {
                    clip.Tags.Add(tag);
                }
            }

            // Create and add new tags if provided
            if (dto.NewTags != null && dto.NewTags.Any())
            {
                var newTags = await GetOrCreateTagsAsync(dto.NewTags);
                foreach (var tag in newTags)
                {
                    clip.Tags.Add(tag);
                }
            }

            _context.Clips.Add(clip);
            await _context.SaveChangesAsync();

            // Reload with tags
            await _context.Entry(clip).Collection(c => c.Tags).LoadAsync();

            return CreatedAtAction(nameof(GetClip), new { id = clip.Id }, MapToDto(clip));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating clip");
            return StatusCode(500, "An error occurred while creating the clip");
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateClip(int id, [FromBody] CreateClipDto dto)
    {
        var clip = await _context.Clips
            .Include(c => c.Tags)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (clip == null)
        {
            return NotFound();
        }

        try
        {
            var storageType = _validationService.DetermineStorageType(dto.LocationString);
            string title;
            int duration;
            string locationString = dto.LocationString;

            if (storageType == StorageType.YouTube)
            {
                if (!_validationService.ValidateYouTubeUrl(dto.LocationString))
                {
                    return BadRequest("Invalid YouTube URL");
                }

                var (videoTitle, videoDuration, videoId) = await _youtubeService.GetVideoMetadataAsync(dto.LocationString);
                // Use provided title if available, otherwise use video title
                title = !string.IsNullOrWhiteSpace(dto.Title) ? dto.Title : videoTitle;
                duration = videoDuration;
                locationString = videoId;
            }
            else
            {
                if (!_validationService.ValidateLocalPath(dto.LocationString))
                {
                    return BadRequest("Local file path does not exist");
                }

                // Use provided title if available, otherwise extract filename as title
                title = !string.IsNullOrWhiteSpace(dto.Title) ? dto.Title : Path.GetFileNameWithoutExtension(dto.LocationString);
                duration = 0;
            }

            clip.Title = title;
            clip.Description = dto.Description ?? string.Empty;
            clip.StorageType = storageType;
            clip.LocationString = locationString;
            clip.Duration = duration;

            // Update tags
            clip.Tags.Clear();
            if (dto.TagIds != null && dto.TagIds.Any())
            {
                var tags = await _context.Tags
                    .Where(t => dto.TagIds.Contains(t.Id))
                    .ToListAsync();

                foreach (var tag in tags)
                {
                    clip.Tags.Add(tag);
                }
            }

            // Create and add new tags if provided
            if (dto.NewTags != null && dto.NewTags.Any())
            {
                var newTags = await GetOrCreateTagsAsync(dto.NewTags);
                foreach (var tag in newTags)
                {
                    clip.Tags.Add(tag);
                }
            }

            await _context.SaveChangesAsync();

            return Ok(MapToDto(clip));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating clip");
            return StatusCode(500, "An error occurred while updating the clip");
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteClip(int id)
    {
        var clip = await _context.Clips.FindAsync(id);
        if (clip == null)
        {
            return NotFound();
        }

        _context.Clips.Remove(clip);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private async Task<List<Tag>> GetOrCreateTagsAsync(List<NewTagDto> newTags)
    {
        var result = new List<Tag>();

        foreach (var newTagDto in newTags)
        {
            if (string.IsNullOrWhiteSpace(newTagDto.Category) || string.IsNullOrWhiteSpace(newTagDto.Value))
            {
                continue; // Skip invalid tags
            }

            if (!Enum.TryParse<TagCategory>(newTagDto.Category, ignoreCase: true, out var category))
            {
                _logger.LogWarning("Invalid tag category: {Category}", newTagDto.Category);
                continue; // Skip invalid categories
            }

            // Check if tag already exists
            var existingTag = await _context.Tags
                .FirstOrDefaultAsync(t => t.Category == category && t.Value == newTagDto.Value.Trim());

            if (existingTag != null)
            {
                result.Add(existingTag);
            }
            else
            {
                // Create new tag
                var tag = new Tag
                {
                    Category = category,
                    Value = newTagDto.Value.Trim()
                };
                _context.Tags.Add(tag);
                await _context.SaveChangesAsync();
                result.Add(tag);
            }
        }

        return result;
    }

    private bool IsUnclassified(Clip clip)
    {
        return clip.Tags.Count == 0 
            || string.IsNullOrWhiteSpace(clip.Description)
            || clip.Title == Path.GetFileNameWithoutExtension(clip.LocationString);
    }

    private ClipDto MapToDto(Clip clip)
    {
        return new ClipDto
        {
            Id = clip.Id,
            Title = clip.Title,
            Description = clip.Description,
            StorageType = clip.StorageType.ToString(),
            LocationString = clip.LocationString,
            Duration = clip.Duration,
            Tags = clip.Tags.Select(t => new TagDto
            {
                Id = t.Id,
                Category = t.Category.ToString(),
                Value = t.Value
            }).ToList(),
            IsUnclassified = IsUnclassified(clip)
        };
    }
}


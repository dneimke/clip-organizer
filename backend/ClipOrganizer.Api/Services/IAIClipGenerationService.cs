using ClipOrganizer.Api.DTOs;

namespace ClipOrganizer.Api.Services;

public interface IAIClipGenerationService
{
    Task<AIClipGenerationResult> GenerateClipMetadataAsync(string userNotes, List<AvailableTag> availableTags);
}

public class AIClipGenerationResult
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<int> SuggestedTagIds { get; set; } = new();
    public List<NewTagDto> SuggestedNewTags { get; set; } = new();
}

public class AvailableTag
{
    public int Id { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}


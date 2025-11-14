namespace ClipOrganizer.Api.DTOs;

public class GenerateMetadataDto
{
    public string Notes { get; set; } = string.Empty;
}

public class GenerateMetadataResponseDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<int> SuggestedTagIds { get; set; } = new();
    public List<NewTagDto> SuggestedNewTags { get; set; } = new();
}


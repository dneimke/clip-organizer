namespace ClipOrganizer.Api.DTOs;

public class ClipDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string StorageType { get; set; } = string.Empty;
    public string LocationString { get; set; } = string.Empty;
    public int Duration { get; set; }
    public string? ThumbnailPath { get; set; }
    public List<TagDto> Tags { get; set; } = new();
    public bool IsUnclassified { get; set; }
}


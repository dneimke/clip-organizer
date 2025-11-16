namespace ClipOrganizer.Api.DTOs;

public class UpdateClipDto
{
    public string LocationString { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Notes { get; set; }
    public List<int> TagIds { get; set; } = new();
    public List<NewTagDto>? NewTags { get; set; }
}



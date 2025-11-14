namespace ClipOrganizer.Api.DTOs;

public class BulkUpdateClipDto
{
    public int ClipId { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public List<int>? TagIds { get; set; }
    public List<NewTagDto>? NewTags { get; set; }
}


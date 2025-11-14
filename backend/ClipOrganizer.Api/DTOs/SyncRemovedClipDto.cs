namespace ClipOrganizer.Api.DTOs;

public class SyncRemovedClipDto
{
    public int ClipId { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
}


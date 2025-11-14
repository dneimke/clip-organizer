namespace ClipOrganizer.Api.DTOs;

public class BulkUploadItemDto
{
    public int ClipId { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
}


namespace ClipOrganizer.Api.DTOs;

public class ReconciliationItemDto
{
    public string FilePath { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // "new", "missing", "matched", "error"
    public string? Directory { get; set; }
    public long? FileSize { get; set; }
    public DateTime? LastModified { get; set; }
    
    // For existing clips
    public int? ClipId { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public List<TagDto>? Tags { get; set; }
    
    // For errors
    public string? ErrorMessage { get; set; }
}


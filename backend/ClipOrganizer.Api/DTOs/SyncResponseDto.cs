namespace ClipOrganizer.Api.DTOs;

public class SyncResponseDto
{
    public List<SyncAddedClipDto> AddedClips { get; set; } = new();
    public List<SyncRemovedClipDto> RemovedClips { get; set; } = new();
    public List<SyncErrorDto> Errors { get; set; } = new();
    public int TotalScanned { get; set; }
    public int TotalAdded { get; set; }
    public int TotalRemoved { get; set; }
}


namespace ClipOrganizer.Api.DTOs;

public class SyncPreviewResponseDto
{
    public List<ReconciliationItemDto> Items { get; set; } = new();
    public int TotalScanned { get; set; }
    public int NewFilesCount { get; set; }
    public int MissingFilesCount { get; set; }
    public int MatchedFilesCount { get; set; }
    public int ErrorCount { get; set; }
    public string RootFolderPath { get; set; } = string.Empty;
}


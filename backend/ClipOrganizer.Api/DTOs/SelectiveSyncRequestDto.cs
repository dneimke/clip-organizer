namespace ClipOrganizer.Api.DTOs;

public class SelectiveSyncRequestDto
{
    public string RootFolderPath { get; set; } = string.Empty;
    public List<string> FilesToAdd { get; set; } = new();
    public List<int> ClipIdsToRemove { get; set; } = new();
}


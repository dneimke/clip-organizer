namespace ClipOrganizer.Api.DTOs;

public class SyncErrorDto
{
    public string FilePath { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
}


namespace ClipOrganizer.Api.DTOs;

public class BulkUploadErrorDto
{
    public string FilePath { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
}


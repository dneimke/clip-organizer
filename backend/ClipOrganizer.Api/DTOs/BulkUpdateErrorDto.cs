namespace ClipOrganizer.Api.DTOs;

public class BulkUpdateErrorDto
{
    public int ClipId { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}


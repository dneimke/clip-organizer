namespace ClipOrganizer.Api.DTOs;

public class BulkUploadResponseDto
{
    public List<BulkUploadItemDto> Successes { get; set; } = new();
    public List<BulkUploadErrorDto> Failures { get; set; } = new();
}


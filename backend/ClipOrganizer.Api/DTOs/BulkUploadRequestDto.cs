namespace ClipOrganizer.Api.DTOs;

public class BulkUploadRequestDto
{
    public List<string> FilePaths { get; set; } = new();
}


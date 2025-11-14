namespace ClipOrganizer.Api.DTOs;

public class BulkUpdateResponseDto
{
    public int SuccessCount { get; set; }
    public List<BulkUpdateErrorDto> Failures { get; set; } = new();
}


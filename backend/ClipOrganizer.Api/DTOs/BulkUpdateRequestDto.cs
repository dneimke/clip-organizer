namespace ClipOrganizer.Api.DTOs;

public class BulkUpdateRequestDto
{
    public List<BulkUpdateClipDto> Updates { get; set; } = new();
}


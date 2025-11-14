namespace ClipOrganizer.Api.DTOs;

public class FilterClipsDto
{
    public string? SearchTerm { get; set; }
    public List<int>? TagIds { get; set; }
}


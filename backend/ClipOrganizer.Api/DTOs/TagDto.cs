namespace ClipOrganizer.Api.DTOs;

public class TagDto
{
    public int Id { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}


namespace ClipOrganizer.Api.DTOs;

public class CreateSessionPlanDto
{
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public List<int> ClipIds { get; set; } = new();
}


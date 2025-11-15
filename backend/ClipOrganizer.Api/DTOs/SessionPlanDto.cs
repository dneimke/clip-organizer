namespace ClipOrganizer.Api.DTOs;

public class SessionPlanDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public List<int> ClipIds { get; set; } = new();
}


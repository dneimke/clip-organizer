namespace ClipOrganizer.Api.DTOs;

public class GenerateSessionPlanDto
{
    public int DurationMinutes { get; set; }
    public List<string> FocusAreas { get; set; } = new();
}


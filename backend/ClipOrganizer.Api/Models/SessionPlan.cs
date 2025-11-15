namespace ClipOrganizer.Api.Models;

public class SessionPlan
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    
    public ICollection<Clip> Clips { get; set; } = new List<Clip>();
}


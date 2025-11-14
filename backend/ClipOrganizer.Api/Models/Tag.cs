namespace ClipOrganizer.Api.Models;

public class Tag
{
    public int Id { get; set; }
    public TagCategory Category { get; set; }
    public string Value { get; set; } = string.Empty;
    
    public ICollection<Clip> Clips { get; set; } = new List<Clip>();
}


namespace ClipOrganizer.Api.Models;

public class Clip
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public StorageType StorageType { get; set; }
    public string LocationString { get; set; } = string.Empty;
    public int Duration { get; set; } // Duration in seconds
    public string? ThumbnailPath { get; set; } // Path to thumbnail image file
    
    public ICollection<Tag> Tags { get; set; } = new List<Tag>();
}


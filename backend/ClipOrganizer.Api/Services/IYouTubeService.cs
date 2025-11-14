namespace ClipOrganizer.Api.Services;

public interface IYouTubeService
{
    Task<(string Title, int DurationSeconds, string VideoId)> GetVideoMetadataAsync(string url);
    string ExtractVideoId(string url);
    bool IsValidYouTubeUrl(string url);
}


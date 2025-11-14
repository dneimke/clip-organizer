using ClipOrganizer.Api.Models;

namespace ClipOrganizer.Api.Services;

public class ClipValidationService : IClipValidationService
{
    private readonly IYouTubeService _youtubeService;

    public ClipValidationService(IYouTubeService youtubeService)
    {
        _youtubeService = youtubeService;
    }

    public StorageType DetermineStorageType(string locationString)
    {
        if (string.IsNullOrWhiteSpace(locationString))
            throw new ArgumentException("Location string cannot be empty", nameof(locationString));

        // Check if it's a YouTube URL
        if (_youtubeService.IsValidYouTubeUrl(locationString))
        {
            return StorageType.YouTube;
        }

        // Assume it's a local path
        return StorageType.Local;
    }

    public bool ValidateLocalPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        try
        {
            // Check if it's an absolute path
            if (!Path.IsPathRooted(path))
                return false;

            // Check if the file exists
            return File.Exists(path);
        }
        catch
        {
            return false;
        }
    }

    public bool ValidateYouTubeUrl(string url)
    {
        return _youtubeService.IsValidYouTubeUrl(url);
    }
}


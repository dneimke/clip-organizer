using ClipOrganizer.Api.Models;

namespace ClipOrganizer.Api.Services;

public interface IClipValidationService
{
    StorageType DetermineStorageType(string locationString);
    bool ValidateLocalPath(string path);
    bool ValidateYouTubeUrl(string url);
}


namespace ClipOrganizer.Api.Services;

public interface IThumbnailService
{
    Task<string?> GenerateThumbnailAsync(string videoPath, int clipId);
    Task<string?> GenerateThumbnailAsync(string videoPath, int clipId, TimeSpan? timestamp);
    void DeleteThumbnail(string thumbnailPath);
    Task<string> GetThumbnailPathAsync(int clipId);
}


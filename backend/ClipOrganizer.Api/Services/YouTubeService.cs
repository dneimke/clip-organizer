using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using ClipOrganizer.Api.Models;
using GoogleYouTubeService = Google.Apis.YouTube.v3.YouTubeService;

namespace ClipOrganizer.Api.Services;

public class YouTubeService : IYouTubeService
{
    private readonly GoogleYouTubeService _googleYouTubeService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<YouTubeService> _logger;

    public YouTubeService(IConfiguration configuration, ILogger<YouTubeService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        
        var apiKey = _configuration["YouTube:ApiKey"];
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException("YouTube API key is not configured. Please set YouTube:ApiKey in appsettings.json");
        }

        _googleYouTubeService = new GoogleYouTubeService(new BaseClientService.Initializer()
        {
            ApiKey = apiKey,
            ApplicationName = "ClipOrganizer"
        });
    }

    public string ExtractVideoId(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return string.Empty;

        // Try to extract video ID from various YouTube URL formats
        var patterns = new[]
        {
            @"(?:youtube\.com\/watch\?v=|youtu\.be\/|youtube\.com\/embed\/)([a-zA-Z0-9_-]{11})",
            @"youtube\.com\/watch\?.*v=([a-zA-Z0-9_-]{11})"
        };

        foreach (var pattern in patterns)
        {
            var match = System.Text.RegularExpressions.Regex.Match(url, pattern);
            if (match.Success && match.Groups.Count > 1)
            {
                return match.Groups[1].Value;
            }
        }

        // If it's already just a video ID (11 characters)
        if (url.Length == 11 && System.Text.RegularExpressions.Regex.IsMatch(url, @"^[a-zA-Z0-9_-]{11}$"))
        {
            return url;
        }

        return string.Empty;
    }

    public bool IsValidYouTubeUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        var videoId = ExtractVideoId(url);
        return !string.IsNullOrEmpty(videoId);
    }

    public async Task<(string Title, int DurationSeconds, string VideoId)> GetVideoMetadataAsync(string url)
    {
        var videoId = ExtractVideoId(url);
        
        if (string.IsNullOrEmpty(videoId))
        {
            throw new ArgumentException("Invalid YouTube URL or video ID", nameof(url));
        }

        try
        {
            var videosRequest = _googleYouTubeService.Videos.List("snippet,contentDetails");
            videosRequest.Id = videoId;

            var response = await videosRequest.ExecuteAsync();

            if (response.Items == null || response.Items.Count == 0)
            {
                throw new InvalidOperationException($"Video with ID {videoId} not found");
            }

            var video = response.Items[0];
            var title = video.Snippet.Title;
            
            // Parse duration (ISO 8601 format: PT1H2M10S)
            var duration = ParseDuration(video.ContentDetails.Duration);
            
            return (title, duration, videoId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching YouTube video metadata for {VideoId}", videoId);
            throw;
        }
    }

    private int ParseDuration(string iso8601Duration)
    {
        // ISO 8601 duration format: PT1H2M10S
        var match = System.Text.RegularExpressions.Regex.Match(iso8601Duration, @"PT(?:(\d+)H)?(?:(\d+)M)?(?:(\d+)S)?");
        
        if (!match.Success)
            return 0;

        var hours = match.Groups[1].Success ? int.Parse(match.Groups[1].Value) : 0;
        var minutes = match.Groups[2].Success ? int.Parse(match.Groups[2].Value) : 0;
        var seconds = match.Groups[3].Success ? int.Parse(match.Groups[3].Value) : 0;

        return hours * 3600 + minutes * 60 + seconds;
    }

    public string GetThumbnailUrl(string videoId)
    {
        if (string.IsNullOrEmpty(videoId))
        {
            return string.Empty;
        }

        // Use maxresdefault for highest quality, fallback options:
        // maxresdefault.jpg - 1280x720 (may not exist for all videos)
        // hqdefault.jpg - 480x360 (high quality, usually available)
        // mqdefault.jpg - 320x180 (medium quality)
        // sddefault.jpg - 640x480 (standard definition)
        return $"https://img.youtube.com/vi/{videoId}/hqdefault.jpg";
    }
}


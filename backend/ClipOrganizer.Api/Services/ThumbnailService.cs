using FFMpegCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using System.Drawing;
using ClipOrganizer.Api.Helpers;

namespace ClipOrganizer.Api.Services;

public class ThumbnailService : IThumbnailService
{
    private readonly string _thumbnailsDirectory;
    private readonly ILogger<ThumbnailService> _logger;
    private readonly IConfiguration _configuration;
    private static bool _ffmpegConfigured = false;
    private static readonly object _configLock = new object();
    private const int ThumbnailWidth = 320;
    private const int ThumbnailHeight = 180;

    public ThumbnailService(IWebHostEnvironment env, IConfiguration configuration, ILogger<ThumbnailService> logger)
    {
        _thumbnailsDirectory = Path.Combine(env.ContentRootPath, "thumbnails");
        _configuration = configuration;
        _logger = logger;
        
        // Ensure thumbnails directory exists
        if (!Directory.Exists(_thumbnailsDirectory))
        {
            Directory.CreateDirectory(_thumbnailsDirectory);
        }

        // Configure FFmpeg path if specified
        ConfigureFFmpeg();
    }

    private void ConfigureFFmpeg()
    {
        // Only configure once (thread-safe)
        if (_ffmpegConfigured) return;

        lock (_configLock)
        {
            if (_ffmpegConfigured) return;

            var ffmpegPath = _configuration["FFmpeg:BinaryFolder"];
            if (!string.IsNullOrWhiteSpace(ffmpegPath))
            {
                try
                {
                    // Normalize the path
                    var normalizedPath = Path.GetFullPath(ffmpegPath);
                    
                    // Verify the directory exists
                    if (Directory.Exists(normalizedPath))
                    {
                        GlobalFFOptions.Configure(new FFOptions { BinaryFolder = normalizedPath });
                        _logger.LogInformation("FFmpeg configured to use custom path: {FFmpegPath}", normalizedPath);
                    }
                    else
                    {
                        _logger.LogWarning("FFmpeg binary folder not found: {FFmpegPath}. Will try to use FFmpeg from PATH.", normalizedPath);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to configure custom FFmpeg path: {FFmpegPath}. Will try to use FFmpeg from PATH.", ffmpegPath);
                }
            }
            else
            {
                _logger.LogDebug("No custom FFmpeg path configured. Will use FFmpeg from system PATH.");
            }

            _ffmpegConfigured = true;
        }
    }

    public async Task<string?> GenerateThumbnailAsync(string videoPath, int clipId)
    {
        try
        {
            if (!File.Exists(videoPath))
            {
                _logger.LogWarning("Video file not found: {VideoPath}", LogSanitizationHelper.SanitizePathForLogging(videoPath));
                return null;
            }

            // Get video duration and extract frame at 10% into video
            var videoInfo = await FFProbe.AnalyseAsync(videoPath);
            var timestamp = videoInfo.Duration * 0.1; // 10% into video
            
            return await GenerateThumbnailAsync(videoPath, clipId, timestamp);
        }
        catch (Exception ex)
        {
            // Provide more helpful error messages
            var errorMessage = GetErrorMessage(ex);
            _logger.LogError(ex, "Failed to generate thumbnail for clip {ClipId} from {VideoPath}. {ErrorMessage}", 
                clipId, LogSanitizationHelper.SanitizePathForLogging(videoPath), LogSanitizationHelper.SanitizeForLogging(errorMessage));
            return null;
        }
    }

    public async Task<string?> GenerateThumbnailAsync(string videoPath, int clipId, TimeSpan? timestamp)
    {
        try
        {
            if (!File.Exists(videoPath))
            {
                _logger.LogWarning("Video file not found: {VideoPath}", LogSanitizationHelper.SanitizePathForLogging(videoPath));
                return null;
            }

            var thumbnailPath = GetThumbnailPath(clipId);
            
            // Use FFmpeg to extract frame (extract at full size, resize with ImageSharp)
            // SnapshotAsync signature: (inputPath, outputPath, size, captureTime)
            await FFMpeg.SnapshotAsync(
                videoPath,
                thumbnailPath,
                size: null, // Extract at full size, resize with ImageSharp
                captureTime: timestamp ?? TimeSpan.FromSeconds(1)
            );

            // Resize/optimize using ImageSharp and convert to JPEG
            if (File.Exists(thumbnailPath))
            {
                using var image = await SixLabors.ImageSharp.Image.LoadAsync(thumbnailPath);
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new SixLabors.ImageSharp.Size(ThumbnailWidth, ThumbnailHeight),
                    Mode = ResizeMode.Max
                }));
                
                // Delete the temporary file (might be PNG from FFmpeg)
                File.Delete(thumbnailPath);
                
                // Save as JPEG
                var jpegEncoder = new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder
                {
                    Quality = 85
                };
                await image.SaveAsync(thumbnailPath, jpegEncoder);
            }

            return thumbnailPath;
        }
        catch (Exception ex)
        {
            // Provide more helpful error messages
            var errorMessage = GetErrorMessage(ex);
            _logger.LogError(ex, "Failed to generate thumbnail for clip {ClipId} from {VideoPath}. {ErrorMessage}", 
                clipId, LogSanitizationHelper.SanitizePathForLogging(videoPath), LogSanitizationHelper.SanitizeForLogging(errorMessage));
            return null;
        }
    }

    private string GetErrorMessage(Exception ex)
    {
        var message = ex.Message;
        var innerMessage = ex.InnerException?.Message ?? "";

        // Check for FFmpeg/ffprobe not found errors
        if (message.Contains("ffprobe.exe") || message.Contains("ffmpeg.exe") || 
            message.Contains("File not found") || innerMessage.Contains("ffprobe.exe") || 
            innerMessage.Contains("ffmpeg.exe"))
        {
            var configuredPath = _configuration["FFmpeg:BinaryFolder"];
            if (!string.IsNullOrWhiteSpace(configuredPath))
            {
                return $"FFmpeg/ffprobe not found at configured path '{configuredPath}'. " +
                       "Please verify the path in appsettings.json under 'FFmpeg:BinaryFolder' " +
                       "or ensure FFmpeg is installed and added to your system PATH.";
            }
            else
            {
                return "FFmpeg/ffprobe not found. Please ensure FFmpeg is installed and either: " +
                       "1) Added to your system PATH, or " +
                       "2) Configured in appsettings.json under 'FFmpeg:BinaryFolder' " +
                       "(e.g., \"FFmpeg\": { \"BinaryFolder\": \"C:\\\\ffmpeg\\\\bin\" })";
            }
        }

        return message;
    }

    public void DeleteThumbnail(string thumbnailPath)
    {
        try
        {
            if (!string.IsNullOrEmpty(thumbnailPath) && File.Exists(thumbnailPath))
            {
                File.Delete(thumbnailPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete thumbnail: {ThumbnailPath}", LogSanitizationHelper.SanitizePathForLogging(thumbnailPath));
        }
    }

    public string GetThumbnailPath(int clipId)
    {
        return Path.Combine(_thumbnailsDirectory, $"{clipId}.jpg");
    }
}


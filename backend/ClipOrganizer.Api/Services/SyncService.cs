using Microsoft.EntityFrameworkCore;
using ClipOrganizer.Api.Data;
using ClipOrganizer.Api.DTOs;
using ClipOrganizer.Api.Models;
using System.IO;

namespace ClipOrganizer.Api.Services;

public class SyncService : ISyncService
{
    private readonly ClipDbContext _context;
    private readonly ILogger<SyncService> _logger;
    private readonly string[] _validVideoExtensions = { ".mp4", ".webm", ".mov", ".avi", ".ogg" };

    public SyncService(ClipDbContext context, ILogger<SyncService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<SyncResponseDto> SyncAsync(string rootFolderPath)
    {
        var response = new SyncResponseDto();

        try
        {
            // Validate root folder path
            if (string.IsNullOrWhiteSpace(rootFolderPath))
            {
                response.Errors.Add(new SyncErrorDto
                {
                    FilePath = string.Empty,
                    ErrorMessage = "Root folder path is required"
                });
                return response;
            }

            if (!Path.IsPathRooted(rootFolderPath))
            {
                response.Errors.Add(new SyncErrorDto
                {
                    FilePath = rootFolderPath,
                    ErrorMessage = "Root folder path must be an absolute path"
                });
                return response;
            }

            if (!Directory.Exists(rootFolderPath))
            {
                response.Errors.Add(new SyncErrorDto
                {
                    FilePath = rootFolderPath,
                    ErrorMessage = "Root folder does not exist"
                });
                return response;
            }

            // Scan filesystem for video files recursively
            var filesystemFiles = ScanDirectory(rootFolderPath);
            response.TotalScanned = filesystemFiles.Count;

            // Get all Local clips from database
            var existingClips = await _context.Clips
                .Where(c => c.StorageType == StorageType.Local)
                .ToListAsync();

            // Create a case-insensitive dictionary for quick lookup
            var existingClipsByPath = existingClips.ToDictionary(
                c => c.LocationString.ToLowerInvariant(),
                c => c,
                StringComparer.OrdinalIgnoreCase
            );

            // Find files that need to be added (exist in filesystem but not in database)
            var filesToAdd = filesystemFiles
                .Where(f => !existingClipsByPath.ContainsKey(f.ToLowerInvariant()))
                .ToList();

            // Find clips that need to be removed (exist in database but not in filesystem)
            var clipsToRemove = existingClips
                .Where(c => !filesystemFiles.Any(f => 
                    string.Equals(f, c.LocationString, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            // Add new files to database
            foreach (var filePath in filesToAdd)
            {
                try
                {
                    if (!File.Exists(filePath))
                    {
                        response.Errors.Add(new SyncErrorDto
                        {
                            FilePath = filePath,
                            ErrorMessage = "File does not exist"
                        });
                        continue;
                    }

                    var title = Path.GetFileNameWithoutExtension(filePath);
                    var clip = new Clip
                    {
                        Title = title,
                        Description = string.Empty,
                        StorageType = StorageType.Local,
                        LocationString = filePath,
                        Duration = 0
                    };

                    _context.Clips.Add(clip);
                    await _context.SaveChangesAsync();

                    response.AddedClips.Add(new SyncAddedClipDto
                    {
                        ClipId = clip.Id,
                        FilePath = filePath,
                        Title = title
                    });
                    response.TotalAdded++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error adding clip during sync: {FilePath}", filePath);
                    response.Errors.Add(new SyncErrorDto
                    {
                        FilePath = filePath,
                        ErrorMessage = $"Error adding clip: {ex.Message}"
                    });
                }
            }

            // Remove clips whose files no longer exist
            foreach (var clip in clipsToRemove)
            {
                try
                {
                    _context.Clips.Remove(clip);
                    await _context.SaveChangesAsync();

                    response.RemovedClips.Add(new SyncRemovedClipDto
                    {
                        ClipId = clip.Id,
                        FilePath = clip.LocationString,
                        Title = clip.Title
                    });
                    response.TotalRemoved++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error removing clip during sync: {ClipId}", clip.Id);
                    response.Errors.Add(new SyncErrorDto
                    {
                        FilePath = clip.LocationString,
                        ErrorMessage = $"Error removing clip: {ex.Message}"
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during sync operation");
            response.Errors.Add(new SyncErrorDto
            {
                FilePath = rootFolderPath,
                ErrorMessage = $"Sync operation failed: {ex.Message}"
            });
        }

        return response;
    }

    private List<string> ScanDirectory(string rootPath)
    {
        var videoFiles = new List<string>();

        try
        {
            // Get all video files in current directory
            foreach (var extension in _validVideoExtensions)
            {
                try
                {
                    var files = Directory.GetFiles(rootPath, $"*{extension}", SearchOption.TopDirectoryOnly);
                    videoFiles.AddRange(files);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error scanning directory for {Extension} files: {Path}", extension, rootPath);
                }
            }

            // Recursively scan subdirectories
            var subdirectories = Directory.GetDirectories(rootPath);
            foreach (var subdirectory in subdirectories)
            {
                try
                {
                    videoFiles.AddRange(ScanDirectory(subdirectory));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error scanning subdirectory: {Path}", subdirectory);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error scanning directory: {Path}", rootPath);
        }

        return videoFiles;
    }
}


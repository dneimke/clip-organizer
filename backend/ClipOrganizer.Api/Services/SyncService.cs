using Microsoft.EntityFrameworkCore;
using ClipOrganizer.Api.Data;
using ClipOrganizer.Api.DTOs;
using ClipOrganizer.Api.Models;
using ClipOrganizer.Api.Helpers;
using System.IO;

namespace ClipOrganizer.Api.Services;

public class SyncService : ISyncService
{
    private readonly ClipDbContext _context;
    private readonly IThumbnailService _thumbnailService;
    private readonly ILogger<SyncService> _logger;
    private readonly string[] _validVideoExtensions = { ".mp4", ".webm", ".mov", ".avi", ".ogg" };

    public SyncService(ClipDbContext context, IThumbnailService thumbnailService, ILogger<SyncService> logger)
    {
        _context = context;
        _thumbnailService = thumbnailService;
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

                    // Generate thumbnail after clip is saved (so we have clip.Id)
                    try
                    {
                        var thumbnailPath = await _thumbnailService.GenerateThumbnailAsync(filePath, clip.Id);
                        if (thumbnailPath != null)
                        {
                            clip.ThumbnailPath = thumbnailPath;
                            await _context.SaveChangesAsync();
                        }
                    }
                    catch (Exception thumbEx)
                    {
                        // Don't fail the entire sync if thumbnail generation fails
                        _logger.LogWarning(thumbEx, "Failed to generate thumbnail for clip {ClipId}", clip.Id);
                    }

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
                    _logger.LogError(ex, "Error adding clip during sync: {FilePath}", LogSanitizationHelper.SanitizePathForLogging(filePath));
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
                    _logger.LogWarning(ex, "Error scanning directory for {Extension} files: {Path}", LogSanitizationHelper.SanitizeForLogging(extension), LogSanitizationHelper.SanitizePathForLogging(rootPath));
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
                    _logger.LogWarning(ex, "Error scanning subdirectory: {Path}", LogSanitizationHelper.SanitizePathForLogging(subdirectory));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error scanning directory: {Path}", LogSanitizationHelper.SanitizePathForLogging(rootPath));
        }

        return videoFiles;
    }

    public async Task<SyncPreviewResponseDto> PreviewSyncAsync(string rootFolderPath)
    {
        var response = new SyncPreviewResponseDto
        {
            RootFolderPath = rootFolderPath
        };

        try
        {
            // Validate root folder path
            if (string.IsNullOrWhiteSpace(rootFolderPath))
            {
                response.Items.Add(new ReconciliationItemDto
                {
                    FilePath = string.Empty,
                    Status = "error",
                    ErrorMessage = "Root folder path is required"
                });
                response.ErrorCount++;
                return response;
            }

            if (!Path.IsPathRooted(rootFolderPath))
            {
                response.Items.Add(new ReconciliationItemDto
                {
                    FilePath = rootFolderPath,
                    Status = "error",
                    ErrorMessage = "Root folder path must be an absolute path"
                });
                response.ErrorCount++;
                return response;
            }

            if (!Directory.Exists(rootFolderPath))
            {
                response.Items.Add(new ReconciliationItemDto
                {
                    FilePath = rootFolderPath,
                    Status = "error",
                    ErrorMessage = "Root folder does not exist"
                });
                response.ErrorCount++;
                return response;
            }

            // Scan filesystem for video files recursively
            var filesystemFiles = ScanDirectory(rootFolderPath);
            response.TotalScanned = filesystemFiles.Count;

            // Get all Local clips from database with tags
            var existingClips = await _context.Clips
                .Where(c => c.StorageType == StorageType.Local)
                .Include(c => c.Tags)
                .ToListAsync();

            // Create a case-insensitive dictionary for quick lookup
            var existingClipsByPath = existingClips.ToDictionary(
                c => c.LocationString.ToLowerInvariant(),
                c => c,
                StringComparer.OrdinalIgnoreCase
            );

            // Process filesystem files
            foreach (var filePath in filesystemFiles)
            {
                try
                {
                    var normalizedPath = filePath.ToLowerInvariant();
                    var fileInfo = new FileInfo(filePath);
                    var directory = Path.GetDirectoryName(filePath);

                    if (existingClipsByPath.ContainsKey(normalizedPath))
                    {
                        // File exists and matches database clip
                        var clip = existingClipsByPath[normalizedPath];
                        response.Items.Add(new ReconciliationItemDto
                        {
                            FilePath = filePath,
                            Status = "matched",
                            Directory = directory,
                            FileSize = fileInfo.Exists ? fileInfo.Length : null,
                            LastModified = fileInfo.Exists ? fileInfo.LastWriteTime : null,
                            ClipId = clip.Id,
                            Title = clip.Title,
                            Description = clip.Description,
                            Tags = clip.Tags.Select(t => new TagDto
                            {
                                Id = t.Id,
                                Category = t.Category.ToString(),
                                Value = t.Value
                            }).ToList()
                        });
                        response.MatchedFilesCount++;
                    }
                    else
                    {
                        // File exists but not in database (new file)
                        response.Items.Add(new ReconciliationItemDto
                        {
                            FilePath = filePath,
                            Status = "new",
                            Directory = directory,
                            FileSize = fileInfo.Exists ? fileInfo.Length : null,
                            LastModified = fileInfo.Exists ? fileInfo.LastWriteTime : null
                        });
                        response.NewFilesCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error processing file during preview: {FilePath}", LogSanitizationHelper.SanitizePathForLogging(filePath));
                    response.Items.Add(new ReconciliationItemDto
                    {
                        FilePath = filePath,
                        Status = "error",
                        ErrorMessage = $"Error processing file: {ex.Message}"
                    });
                    response.ErrorCount++;
                }
            }

            // Process database clips to find missing files
            foreach (var clip in existingClips)
            {
                try
                {
                    var fileExists = File.Exists(clip.LocationString);
                    if (!fileExists)
                    {
                        var directory = Path.GetDirectoryName(clip.LocationString);
                        response.Items.Add(new ReconciliationItemDto
                        {
                            FilePath = clip.LocationString,
                            Status = "missing",
                            Directory = directory,
                            ClipId = clip.Id,
                            Title = clip.Title,
                            Description = clip.Description,
                            Tags = clip.Tags.Select(t => new TagDto
                            {
                                Id = t.Id,
                                Category = t.Category.ToString(),
                                Value = t.Value
                            }).ToList()
                        });
                        response.MissingFilesCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error checking clip file during preview: {ClipId}", clip.Id);
                    response.Items.Add(new ReconciliationItemDto
                    {
                        FilePath = clip.LocationString,
                        Status = "error",
                        ClipId = clip.Id,
                        Title = clip.Title,
                        ErrorMessage = $"Error checking file: {ex.Message}"
                    });
                    response.ErrorCount++;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during preview sync operation");
            response.Items.Add(new ReconciliationItemDto
            {
                FilePath = rootFolderPath,
                Status = "error",
                ErrorMessage = $"Preview operation failed: {ex.Message}"
            });
            response.ErrorCount++;
        }

        return response;
    }

    public async Task<SyncResponseDto> SelectiveSyncAsync(SelectiveSyncRequestDto request)
    {
        var response = new SyncResponseDto();

        try
        {
            // Validate root folder path
            if (string.IsNullOrWhiteSpace(request.RootFolderPath))
            {
                response.Errors.Add(new SyncErrorDto
                {
                    FilePath = string.Empty,
                    ErrorMessage = "Root folder path is required"
                });
                return response;
            }

            if (!Path.IsPathRooted(request.RootFolderPath))
            {
                response.Errors.Add(new SyncErrorDto
                {
                    FilePath = request.RootFolderPath,
                    ErrorMessage = "Root folder path must be an absolute path"
                });
                return response;
            }

            if (!Directory.Exists(request.RootFolderPath))
            {
                response.Errors.Add(new SyncErrorDto
                {
                    FilePath = request.RootFolderPath,
                    ErrorMessage = "Root folder does not exist"
                });
                return response;
            }

            // Add selected files
            foreach (var filePath in request.FilesToAdd)
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

                    // Check if clip already exists
                    var existingClip = await _context.Clips
                        .FirstOrDefaultAsync(c => c.StorageType == StorageType.Local &&
                            string.Equals(c.LocationString, filePath, StringComparison.OrdinalIgnoreCase));

                    if (existingClip != null)
                    {
                        response.Errors.Add(new SyncErrorDto
                        {
                            FilePath = filePath,
                            ErrorMessage = "A clip with this file path already exists"
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
                    _logger.LogError(ex, "Error adding clip during selective sync: {FilePath}", LogSanitizationHelper.SanitizePathForLogging(filePath));
                    response.Errors.Add(new SyncErrorDto
                    {
                        FilePath = filePath,
                        ErrorMessage = $"Error adding clip: {ex.Message}"
                    });
                }
            }

            // Remove selected clips
            foreach (var clipId in request.ClipIdsToRemove)
            {
                try
                {
                    var clip = await _context.Clips.FindAsync(clipId);
                    if (clip == null)
                    {
                        response.Errors.Add(new SyncErrorDto
                        {
                            FilePath = string.Empty,
                            ErrorMessage = $"Clip with ID {clipId} not found"
                        });
                        continue;
                    }

                    var filePath = clip.LocationString;
                    var title = clip.Title;

                    _context.Clips.Remove(clip);
                    await _context.SaveChangesAsync();

                    response.RemovedClips.Add(new SyncRemovedClipDto
                    {
                        ClipId = clipId,
                        FilePath = filePath,
                        Title = title
                    });
                    response.TotalRemoved++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error removing clip during selective sync: {ClipId}", clipId);
                    response.Errors.Add(new SyncErrorDto
                    {
                        FilePath = string.Empty,
                        ErrorMessage = $"Error removing clip {clipId}: {ex.Message}"
                    });
                }
            }

            response.TotalScanned = request.FilesToAdd.Count + request.ClipIdsToRemove.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during selective sync operation");
            response.Errors.Add(new SyncErrorDto
            {
                FilePath = request.RootFolderPath,
                ErrorMessage = $"Selective sync operation failed: {ex.Message}"
            });
        }

        return response;
    }
}


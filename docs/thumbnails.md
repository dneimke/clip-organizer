# Thumbnail Generation Feature

This document explains how the thumbnail generation feature works in the Clip Organizer application.

## Overview

The application automatically generates thumbnails for video clips to provide visual previews in the UI. Thumbnails are generated differently depending on the clip's storage type:

- **Local Files**: Thumbnails are extracted from video files using FFmpeg and stored locally
- **YouTube Videos**: Thumbnails use YouTube's thumbnail API (no local processing required)

## How It Works

### Local File Thumbnails

For clips stored as local files, thumbnails are generated using the following process:

1. **Frame Extraction**: FFmpeg extracts a frame from the video at 10% of the video duration (to avoid black screens or intro sequences)
2. **Resizing**: The extracted frame is resized to 320x180 pixels using ImageSharp
3. **Storage**: The thumbnail is saved as a JPEG file in the `thumbnails/` directory with the filename `{clipId}.jpg`
4. **Database**: The thumbnail path is stored in the `Clips.ThumbnailPath` column

**Technical Details**:
- **Tool**: FFmpeg (via FFMpegCore .NET library)
- **Image Processing**: ImageSharp (SixLabors.ImageSharp)
- **Format**: JPEG
- **Dimensions**: 320x180 pixels (16:9 aspect ratio)
- **Storage Location**: `backend/ClipOrganizer.Api/thumbnails/`
- **Naming Convention**: `{clipId}.jpg` (e.g., `1.jpg`, `42.jpg`)

### YouTube Thumbnails

For YouTube clips, thumbnails are handled differently:

1. **URL Generation**: The application constructs a YouTube thumbnail URL using the video ID
2. **Storage**: The URL is stored in the `Clips.ThumbnailPath` column (not a local file path)
3. **Serving**: When requested, the application redirects to YouTube's thumbnail URL

**Technical Details**:
- **Format**: YouTube provides multiple thumbnail sizes (default, medium, high, standard, maxres)
- **URL Pattern**: `https://img.youtube.com/vi/{videoId}/{size}.jpg`
- **No Local Storage**: YouTube thumbnails are not downloaded or stored locally
- **No FFmpeg Required**: YouTube thumbnails don't require FFmpeg installation

## When Thumbnails Are Generated

Thumbnails are automatically generated in the following scenarios:

1. **Creating a New Clip**: When a clip is created via `POST /api/clips`
   - Local files: Thumbnail is generated immediately after clip creation
   - YouTube videos: Thumbnail URL is set during clip creation

2. **Updating a Clip**: When a clip's location is updated via `PUT /api/clips/{id}`
   - If the location changes, the old thumbnail is deleted and a new one is generated

3. **Bulk Upload**: When clips are added via bulk upload
   - Thumbnails are generated for each new local clip

4. **Filesystem Sync**: When syncing clips from the filesystem
   - Thumbnails are generated for newly discovered local clips

5. **Manual Regeneration**: Via `POST /api/clips/regenerate-thumbnails`
   - Can regenerate thumbnails for existing clips (see below)

## Regenerating Thumbnails

### API Endpoint

**Endpoint**: `POST /api/clips/regenerate-thumbnails`

**Query Parameters**:
- `regenerateAll` (optional, default: `false`): If `true`, regenerates thumbnails for all clips. If `false`, only processes clips without thumbnails.

### Usage Examples

**Generate thumbnails for clips missing them** (default):
```bash
# PowerShell
Invoke-RestMethod -Uri "http://localhost:5059/api/clips/regenerate-thumbnails" -Method POST

# curl
curl -X POST http://localhost:5059/api/clips/regenerate-thumbnails
```

**Regenerate all thumbnails**:
```bash
# PowerShell
Invoke-RestMethod -Uri "http://localhost:5059/api/clips/regenerate-thumbnails?regenerateAll=true" -Method POST

# curl
curl -X POST "http://localhost:5059/api/clips/regenerate-thumbnails?regenerateAll=true"
```

**Using browser developer console**:
```javascript
fetch('http://localhost:5059/api/clips/regenerate-thumbnails', { method: 'POST' })
  .then(r => r.json())
  .then(console.log);
```

### Response Format

The endpoint returns a JSON object with the following structure:

```json
{
  "totalProcessed": 25,
  "succeeded": 23,
  "failed": 1,
  "skipped": 1,
  "message": "Thumbnail regeneration completed. Processed: 25, Succeeded: 23, Failed: 1, Skipped: 1"
}
```

**Fields**:
- `totalProcessed`: Total number of clips processed
- `succeeded`: Number of thumbnails successfully generated
- `failed`: Number of clips that failed to generate thumbnails (e.g., missing files, FFmpeg errors)
- `skipped`: Number of clips skipped (e.g., file not found, invalid storage type)
- `message`: Human-readable summary message

## Serving Thumbnails

### API Endpoint

**Endpoint**: `GET /api/clips/{id}/thumbnail`

**Behavior**:
- For local clips: Returns the JPEG file from the filesystem
- For YouTube clips: Redirects (HTTP 302) to YouTube's thumbnail URL

**Content-Type**: `image/jpeg` (for local thumbnails)

### Frontend Usage

Thumbnails are displayed in:
- **Clip Cards**: Shows thumbnail image in the clip list/grid
- **Clip Detail Page**: Shows thumbnail above the video player

The frontend handles both local and YouTube thumbnails automatically:
- Local thumbnails: Loaded from `/api/clips/{id}/thumbnail`
- YouTube thumbnails: Loaded directly from YouTube's CDN (via redirect)

## Error Handling

### Graceful Degradation

Thumbnail generation failures are handled gracefully:

- **Clip Creation**: If thumbnail generation fails, the clip is still created successfully (just without a thumbnail)
- **Errors Logged**: All thumbnail generation errors are logged as warnings
- **No Blocking**: Thumbnail failures don't prevent clip operations from completing

### Common Failure Scenarios

1. **FFmpeg Not Installed**: Local thumbnail generation fails, but clips are still created
2. **FFmpeg Not in PATH**: Same as above - graceful failure
3. **Video File Missing**: Clip is skipped during regeneration
4. **Invalid Video Format**: FFmpeg may fail to process certain formats
5. **Disk Space**: Insufficient disk space prevents thumbnail file creation
6. **Permissions**: File system permissions may prevent thumbnail creation

### Configuration

### FFmpeg Path Configuration

The application can find FFmpeg in two ways:

1. **System PATH** (Default): If FFmpeg is installed and added to your system PATH, the application will use it automatically.

2. **Custom Path** (Alternative): You can configure a custom FFmpeg path in `appsettings.json`:

```json
{
  "FFmpeg": {
    "BinaryFolder": "C:\\ffmpeg\\bin"
  }
}
```

**Notes**:
- Replace `C:\\ffmpeg\\bin` with the actual path to your FFmpeg `bin` directory
- Use double backslashes (`\\`) in JSON paths on Windows
- The path should point to the directory containing `ffmpeg.exe` and `ffprobe.exe`
- If a custom path is configured, it takes precedence over PATH
- The application validates the path exists and logs a warning if not found

## Troubleshooting

If thumbnails aren't generating:

1. **Verify FFmpeg Installation**:
   ```bash
   ffmpeg -version
   ```

2. **Check FFmpeg Configuration**:
   - **If using PATH**: Ensure FFmpeg's `bin` directory is in your system PATH
   - **If using custom path**: Verify the path in `appsettings.json` is correct and points to the `bin` directory
   - Restart your terminal/IDE after adding FFmpeg to PATH
   - Restart the backend application after changing `appsettings.json`

3. **Check Backend Logs**:
   - Look for warnings about thumbnail generation failures
   - Check for specific FFmpeg error messages
   - Look for "FFmpeg configured to use custom path" message on startup (if using custom path)
   - Error messages now include helpful guidance on how to fix FFmpeg configuration issues

4. **Verify File Permissions**:
   - Ensure the application has write access to the `thumbnails/` directory
   - Check that video files are readable

5. **Test Thumbnail Generation**:
   - Try regenerating thumbnails for a single clip
   - Check the API response for specific error messages

6. **Common Error: "File not found: ffprobe.exe"**:
   - This means FFmpeg is not found. Solutions:
     - Add FFmpeg to your system PATH (see [Install FFmpeg](../README.md#22-install-ffmpeg))
     - Or configure a custom path in `appsettings.json` under `FFmpeg:BinaryFolder`
     - Restart the backend application after making changes

## Database Schema

### Clips Table

The `Clips` table includes a `ThumbnailPath` column:

- **Type**: `TEXT` (nullable)
- **Purpose**: Stores the path to the thumbnail file (local) or URL (YouTube)
- **Local Clips**: Contains a file path like `C:\path\to\thumbnails\42.jpg`
- **YouTube Clips**: Contains a URL like `https://img.youtube.com/vi/VIDEO_ID/default.jpg`

### Migration

The `ThumbnailPath` column is automatically added to existing databases on application startup. No manual migration is required.

## File System Structure

```
backend/ClipOrganizer.Api/
├── thumbnails/          # Generated thumbnail images (created automatically)
│   ├── 1.jpg
│   ├── 2.jpg
│   └── ...
└── clips.db             # SQLite database
```

**Notes**:
- The `thumbnails/` directory is created automatically when the first thumbnail is generated
- Thumbnails are named `{clipId}.jpg` for easy identification
- Old thumbnails are automatically deleted when clips are updated or deleted

## Performance Considerations

### Generation Time

- **Local Files**: Thumbnail generation takes 1-5 seconds per clip (depending on video size and FFmpeg performance)
- **YouTube Clips**: Thumbnail URL generation is instantaneous (no processing)

### Batch Operations

When regenerating thumbnails for many clips:
- The operation processes clips sequentially (one at a time)
- Large batches may take several minutes
- Consider running during off-peak hours for large libraries

### Storage

- **Thumbnail Size**: Approximately 5-20 KB per thumbnail (JPEG, 320x180)
- **Storage Impact**: Minimal - 1000 thumbnails ≈ 5-20 MB

## Future Enhancements

Potential improvements to the thumbnail feature:

1. **Custom Timestamp**: Allow users to specify which frame to use for thumbnails
2. **Multiple Thumbnails**: Generate multiple thumbnails per clip (e.g., start, middle, end)
3. **Thumbnail Preview**: Show thumbnail preview when hovering over clips
4. **Batch Processing**: Parallel thumbnail generation for faster batch operations
5. **Thumbnail Quality Settings**: Allow users to configure thumbnail size/quality
6. **Thumbnail Cache**: Cache thumbnails to reduce regeneration overhead

## Related Documentation

- [README.md](../README.md) - Main project documentation
- [YouTube Integration](youtube-integration.md) - YouTube-specific features


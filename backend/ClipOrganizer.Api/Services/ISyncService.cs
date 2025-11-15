using ClipOrganizer.Api.DTOs;

namespace ClipOrganizer.Api.Services;

public interface ISyncService
{
    Task<SyncResponseDto> SyncAsync(string rootFolderPath);
    Task<SyncPreviewResponseDto> PreviewSyncAsync(string rootFolderPath);
    Task<SyncResponseDto> SelectiveSyncAsync(SelectiveSyncRequestDto request);
}


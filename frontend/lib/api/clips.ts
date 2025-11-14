import { Clip, CreateClipDto, GenerateMetadataDto, GenerateMetadataResponseDto, BulkUploadRequest, BulkUploadResponse, BulkUpdateRequest, BulkUpdateResponse } from '@/types';

const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5059';

export async function getClips(
  searchTerm?: string, 
  tagIds?: number[],
  sortBy?: string,
  sortOrder?: string,
  unclassifiedOnly?: boolean
): Promise<Clip[]> {
  const params = new URLSearchParams();
  if (searchTerm) params.append('searchTerm', searchTerm);
  if (tagIds && tagIds.length > 0) {
    tagIds.forEach(id => params.append('tagIds', id.toString()));
  }
  if (sortBy) params.append('sortBy', sortBy);
  if (sortOrder) params.append('sortOrder', sortOrder);
  if (unclassifiedOnly) params.append('unclassifiedOnly', 'true');

  const response = await fetch(`${API_BASE_URL}/api/clips?${params.toString()}`);
  if (!response.ok) {
    throw new Error('Failed to fetch clips');
  }
  return response.json();
}

export async function getClip(id: number): Promise<Clip> {
  const response = await fetch(`${API_BASE_URL}/api/clips/${id}`);
  if (!response.ok) {
    throw new Error('Failed to fetch clip');
  }
  return response.json();
}

export async function createClip(dto: CreateClipDto): Promise<Clip> {
  const response = await fetch(`${API_BASE_URL}/api/clips`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(dto),
  });
  if (!response.ok) {
    const error = await response.text();
    throw new Error(error || 'Failed to create clip');
  }
  return response.json();
}

export async function updateClip(id: number, dto: CreateClipDto): Promise<Clip> {
  const response = await fetch(`${API_BASE_URL}/api/clips/${id}`, {
    method: 'PUT',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(dto),
  });
  if (!response.ok) {
    const error = await response.text();
    throw new Error(error || 'Failed to update clip');
  }
  return response.json();
}

export async function deleteClip(id: number): Promise<void> {
  const response = await fetch(`${API_BASE_URL}/api/clips/${id}`, {
    method: 'DELETE',
  });
  if (!response.ok) {
    throw new Error('Failed to delete clip');
  }
}

export async function generateClipMetadata(dto: GenerateMetadataDto): Promise<GenerateMetadataResponseDto> {
  const response = await fetch(`${API_BASE_URL}/api/clips/generate-metadata`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(dto),
  });
  if (!response.ok) {
    const error = await response.text();
    throw new Error(error || 'Failed to generate metadata');
  }
  return response.json();
}

export async function bulkUploadClips(filePaths: string[]): Promise<BulkUploadResponse> {
  const request: BulkUploadRequest = { filePaths };
  const response = await fetch(`${API_BASE_URL}/api/clips/bulk-upload`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(request),
  });
  if (!response.ok) {
    const error = await response.text();
    throw new Error(error || 'Failed to upload clips');
  }
  return response.json();
}

export async function getUnclassifiedClips(): Promise<Clip[]> {
  const response = await fetch(`${API_BASE_URL}/api/clips/unclassified`);
  if (!response.ok) {
    const errorText = await response.text();
    console.error('Failed to fetch unclassified clips:', response.status, errorText);
    throw new Error(`Failed to fetch unclassified clips: ${response.status} ${errorText || response.statusText}`);
  }
  return response.json();
}

export async function bulkUpdateClips(updates: BulkUpdateRequest['updates']): Promise<BulkUpdateResponse> {
  const request: BulkUpdateRequest = { updates };
  const response = await fetch(`${API_BASE_URL}/api/clips/bulk-update`, {
    method: 'PUT',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(request),
  });
  if (!response.ok) {
    const error = await response.text();
    throw new Error(error || 'Failed to update clips');
  }
  return response.json();
}


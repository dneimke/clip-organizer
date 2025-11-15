export enum StorageType {
  Local = "Local",
  YouTube = "YouTube"
}

export enum TagCategory {
  SkillTactic = "SkillTactic",
  FieldArea = "FieldArea",
  PlayerRole = "PlayerRole",
  OutcomeQuality = "OutcomeQuality"
}

export interface Tag {
  id: number;
  category: string;
  value: string;
}

export interface Clip {
  id: number;
  title: string;
  description: string;
  storageType: string;
  locationString: string;
  duration: number;
  tags: Tag[];
  isUnclassified: boolean;
}

export interface CreateClipDto {
  locationString: string;
  title?: string;
  description?: string;
  notes?: string;
  tagIds: number[];
  newTags?: NewTag[];
}

export interface GenerateMetadataDto {
  notes: string;
}

export interface GenerateMetadataResponseDto {
  title: string;
  description: string;
  suggestedTagIds: number[];
  suggestedNewTags?: NewTag[];
}

export interface CreateTagDto {
  category: string;
  value: string;
}

export interface NewTag {
  category: string;
  value: string;
}

export interface BulkUploadRequest {
  filePaths: string[];
}

export interface BulkUploadItem {
  clipId: number;
  filePath: string;
  title: string;
}

export interface BulkUploadError {
  filePath: string;
  errorMessage: string;
}

export interface BulkUploadResponse {
  successes: BulkUploadItem[];
  failures: BulkUploadError[];
}


export interface SyncRequest {
  rootFolderPath: string;
}

export interface SyncAddedClip {
  clipId: number;
  filePath: string;
  title: string;
}

export interface SyncRemovedClip {
  clipId: number;
  filePath: string;
  title: string;
}

export interface SyncError {
  filePath: string;
  errorMessage: string;
}

export interface SyncResponse {
  addedClips: SyncAddedClip[];
  removedClips: SyncRemovedClip[];
  errors: SyncError[];
  totalScanned: number;
  totalAdded: number;
  totalRemoved: number;
}

export interface Setting {
  key: string;
  value: string;
}

export interface RootFolderSetting {
  rootFolderPath: string;
}

export interface ReconciliationItem {
  filePath: string;
  status: 'new' | 'missing' | 'matched' | 'error';
  directory?: string;
  fileSize?: number;
  lastModified?: string;
  clipId?: number;
  title?: string;
  description?: string;
  tags?: Tag[];
  errorMessage?: string;
}

export interface SyncPreviewResponse {
  items: ReconciliationItem[];
  totalScanned: number;
  newFilesCount: number;
  missingFilesCount: number;
  matchedFilesCount: number;
  errorCount: number;
  rootFolderPath: string;
}

export interface SelectiveSyncRequest {
  rootFolderPath: string;
  filesToAdd: string[];
  clipIdsToRemove: number[];
}

export interface SessionPlan {
  id: number;
  title: string;
  summary: string;
  createdDate: string;
  clipIds: number[];
}

export interface GenerateSessionPlanRequest {
  durationMinutes: number;
  focusAreas: string[];
}

export interface CreateSessionPlanRequest {
  title: string;
  summary: string;
  clipIds: number[];
}


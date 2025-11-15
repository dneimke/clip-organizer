'use client';

import { useState, useEffect, useMemo } from 'react';
import { useRouter } from 'next/navigation';
import { getSyncPreview, selectiveSync } from '@/lib/api/clips';
import { getRootFolder } from '@/lib/api/settings';
import { SyncPreviewResponse, ReconciliationItem, SyncResponse } from '@/types';
import ReconciliationItemComponent from '@/components/ReconciliationItem';
import ReconciliationSummary from '@/components/ReconciliationSummary';
import StatusFilter from '@/components/StatusFilter';

type StatusFilterType = 'all' | 'new' | 'missing' | 'matched' | 'error';

export default function ReconcilePage() {
  const router = useRouter();
  const [rootFolderPath, setRootFolderPath] = useState<string>('');
  const [configuredRootFolder, setConfiguredRootFolder] = useState<string>('');
  const [isLoading, setIsLoading] = useState(true);
  const [isPreviewing, setIsPreviewing] = useState(false);
  const [isApplying, setIsApplying] = useState(false);
  const [preview, setPreview] = useState<SyncPreviewResponse | null>(null);
  const [result, setResult] = useState<SyncResponse | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [isUsingDefault, setIsUsingDefault] = useState(true);
  const [selectedStatus, setSelectedStatus] = useState<StatusFilterType>('all');
  const [selectedItems, setSelectedItems] = useState<Set<string>>(new Set());

  useEffect(() => {
    loadConfiguredRootFolder();
  }, []);

  const loadConfiguredRootFolder = async () => {
    try {
      setIsLoading(true);
      const setting = await getRootFolder();
      const configuredPath = setting.rootFolderPath || '';
      setConfiguredRootFolder(configuredPath);
      setRootFolderPath(configuredPath);
      setIsUsingDefault(true);
    } catch (err: any) {
      console.error('Failed to load configured root folder:', err);
    } finally {
      setIsLoading(false);
    }
  };

  const handleUseDefault = () => {
    setRootFolderPath(configuredRootFolder);
    setIsUsingDefault(true);
    setError(null);
  };

  const handlePathChange = (value: string) => {
    setRootFolderPath(value);
    setIsUsingDefault(value === configuredRootFolder);
    setError(null);
  };

  const handlePreview = async () => {
    const pathToUse = rootFolderPath.trim() || configuredRootFolder.trim();
    
    if (!pathToUse) {
      setError('Please enter a root folder path or configure one in Settings');
      return;
    }

    setIsPreviewing(true);
    setError(null);
    setPreview(null);
    setResult(null);
    setSelectedItems(new Set());

    try {
      const response = await getSyncPreview(pathToUse === configuredRootFolder ? '' : pathToUse);
      setPreview(response);
    } catch (err: any) {
      setError(err.message || 'Failed to preview sync');
    } finally {
      setIsPreviewing(false);
    }
  };

  const handleToggleItem = (filePath: string) => {
    const newSelected = new Set(selectedItems);
    if (newSelected.has(filePath)) {
      newSelected.delete(filePath);
    } else {
      newSelected.add(filePath);
    }
    setSelectedItems(newSelected);
  };

  const handleSelectAll = () => {
    if (!preview) return;
    const selectableItems = preview.items.filter(
      item => item.status === 'new' || item.status === 'missing'
    );
    const allSelected = selectableItems.every(item => selectedItems.has(item.filePath));
    
    if (allSelected) {
      // Deselect all
      setSelectedItems(new Set());
    } else {
      // Select all selectable items
      const newSelected = new Set(selectedItems);
      selectableItems.forEach(item => newSelected.add(item.filePath));
      setSelectedItems(newSelected);
    }
  };

  const handleSelectByStatus = (status: 'new' | 'missing') => {
    if (!preview) return;
    const itemsToSelect = preview.items.filter(item => item.status === status);
    const allSelected = itemsToSelect.every(item => selectedItems.has(item.filePath));
    
    const newSelected = new Set(selectedItems);
    if (allSelected) {
      // Deselect all of this status
      itemsToSelect.forEach(item => newSelected.delete(item.filePath));
    } else {
      // Select all of this status
      itemsToSelect.forEach(item => newSelected.add(item.filePath));
    }
    setSelectedItems(newSelected);
  };

  const handleApply = async () => {
    if (!preview) return;

    const filesToAdd = preview.items
      .filter(item => item.status === 'new' && selectedItems.has(item.filePath))
      .map(item => item.filePath);

    const clipIdsToRemove = preview.items
      .filter(item => item.status === 'missing' && selectedItems.has(item.filePath) && item.clipId)
      .map(item => item.clipId!)
      .filter(id => id !== undefined);

    if (filesToAdd.length === 0 && clipIdsToRemove.length === 0) {
      setError('Please select at least one item to sync');
      return;
    }

    setIsApplying(true);
    setError(null);

    try {
      const pathToUse = rootFolderPath.trim() || configuredRootFolder.trim();
      const response = await selectiveSync({
        rootFolderPath: pathToUse,
        filesToAdd,
        clipIdsToRemove,
      });
      setResult(response);
      // Refresh preview after applying changes
      await handlePreview();
    } catch (err: any) {
      setError(err.message || 'Failed to apply changes');
    } finally {
      setIsApplying(false);
    }
  };

  const filteredItems = useMemo(() => {
    if (!preview) return [];
    if (selectedStatus === 'all') return preview.items;
    return preview.items.filter(item => item.status === selectedStatus);
  }, [preview, selectedStatus]);

  const statusCounts = useMemo(() => {
    if (!preview) {
      return { all: 0, new: 0, missing: 0, matched: 0, error: 0 };
    }
    return {
      all: preview.items.length,
      new: preview.newFilesCount,
      missing: preview.missingFilesCount,
      matched: preview.matchedFilesCount,
      error: preview.errorCount,
    };
  }, [preview]);

  const selectedCount = useMemo(() => {
    return selectedItems.size;
  }, [selectedItems]);

  return (
    <div className="min-h-screen bg-[#0a0a0a] text-white">
      <div className="container mx-auto px-6 py-8">
        <div className="max-w-6xl mx-auto">
          {/* Header */}
          <div className="mb-8">
            <h1 className="text-3xl font-bold mb-2">Reconcile Filesystem</h1>
            <p className="text-gray-400">
              Preview changes before syncing. Select which files to add or remove from your video library.
            </p>
          </div>

          {/* Form */}
          <div className="bg-[#1a1a1a] border border-[#303030] rounded-lg p-6 mb-6">
            {isLoading ? (
              <div className="text-center py-8">
                <p className="text-[#007BFF]">Loading...</p>
              </div>
            ) : (
              <>
                <div className="mb-4">
                  <div className="flex justify-between items-center mb-2">
                    <label htmlFor="rootFolderPath" className="block text-sm font-medium text-white">
                      Root Folder Path
                    </label>
                    {configuredRootFolder && (
                      <button
                        onClick={handleUseDefault}
                        disabled={isUsingDefault || isPreviewing || isApplying}
                        className="text-xs text-blue-400 hover:text-blue-300 disabled:opacity-50 disabled:cursor-not-allowed"
                      >
                        Use Default
                      </button>
                    )}
                  </div>
                  <input
                    id="rootFolderPath"
                    type="text"
                    value={rootFolderPath}
                    onChange={(e) => handlePathChange(e.target.value)}
                    placeholder="C:\\Videos"
                    className="w-full px-4 py-2 border border-[#303030] rounded-lg bg-[#2a2a2a] text-white text-sm font-mono placeholder:text-gray-500 focus:outline-none focus:border-blue-500"
                    disabled={isPreviewing || isApplying}
                  />
                  <div className="flex items-center gap-2 mt-1">
                    <p className="text-xs text-gray-500">
                      {configuredRootFolder 
                        ? (isUsingDefault 
                          ? `Using configured root folder: ${configuredRootFolder}` 
                          : 'Enter a custom folder path or click "Use Default"')
                        : 'Enter the absolute path to the root folder to scan (e.g., C:\\Videos) or configure one in Settings'}
                    </p>
                  </div>
                </div>

                {/* Error message */}
                {error && (
                  <div className="mb-4 bg-red-900/20 border border-red-700 text-red-300 px-4 py-3 rounded text-sm">
                    {error}
                  </div>
                )}

                {/* Action buttons */}
                <div className="flex justify-end gap-3">
                  <button
                    onClick={() => router.back()}
                    disabled={isPreviewing || isApplying}
                    className="px-4 py-2 bg-[#3a3a3a] text-white rounded-lg hover:bg-[#4a4a3a] disabled:opacity-50 disabled:cursor-not-allowed transition-colors font-medium"
                  >
                    Cancel
                  </button>
                  <button
                    onClick={handlePreview}
                    disabled={isPreviewing || isApplying || (!rootFolderPath.trim() && !configuredRootFolder.trim())}
                    className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors font-medium"
                  >
                    {isPreviewing ? 'Previewing...' : 'Preview Changes'}
                  </button>
                </div>
              </>
            )}
          </div>

          {/* Preview Results */}
          {preview && (
            <div className="space-y-6">
              {/* Summary */}
              <ReconciliationSummary preview={preview} />

              {/* Selection Controls */}
              {(preview.newFilesCount > 0 || preview.missingFilesCount > 0) && (
                <div className="bg-[#1a1a1a] border border-[#303030] rounded-lg p-6">
                  <div className="flex items-center justify-between mb-4">
                    <h2 className="text-xl font-semibold">Selection Controls</h2>
                    <div className="text-sm text-gray-400">
                      {selectedCount} item{selectedCount !== 1 ? 's' : ''} selected
                    </div>
                  </div>
                  <div className="flex flex-wrap gap-2">
                    <button
                      onClick={handleSelectAll}
                      className="px-4 py-2 bg-[#2a2a2a] text-white rounded-lg hover:bg-[#3a3a3a] transition-colors text-sm font-medium"
                    >
                      {preview.items.filter(item => (item.status === 'new' || item.status === 'missing') && selectedItems.has(item.filePath)).length === preview.newFilesCount + preview.missingFilesCount
                        ? 'Deselect All'
                        : 'Select All'}
                    </button>
                    {preview.newFilesCount > 0 && (
                      <button
                        onClick={() => handleSelectByStatus('new')}
                        className="px-4 py-2 bg-yellow-500/20 text-yellow-400 rounded-lg hover:bg-yellow-500/30 transition-colors text-sm font-medium border border-yellow-500/50"
                      >
                        {preview.items.filter(item => item.status === 'new' && selectedItems.has(item.filePath)).length === preview.newFilesCount
                          ? 'Deselect New'
                          : 'Select All New'}
                      </button>
                    )}
                    {preview.missingFilesCount > 0 && (
                      <button
                        onClick={() => handleSelectByStatus('missing')}
                        className="px-4 py-2 bg-red-500/20 text-red-400 rounded-lg hover:bg-red-500/30 transition-colors text-sm font-medium border border-red-500/50"
                      >
                        {preview.items.filter(item => item.status === 'missing' && selectedItems.has(item.filePath)).length === preview.missingFilesCount
                          ? 'Deselect Missing'
                          : 'Select All Missing'}
                      </button>
                    )}
                  </div>
                </div>
              )}

              {/* Status Filter */}
              <StatusFilter
                selectedStatus={selectedStatus}
                onStatusChange={setSelectedStatus}
                counts={statusCounts}
              />

              {/* Items List */}
              <div className="space-y-3">
                {filteredItems.length === 0 ? (
                  <div className="text-center py-12 bg-[#1a1a1a] border border-[#303030] rounded-lg">
                    <p className="text-gray-400">No items found for this filter.</p>
                  </div>
                ) : (
                  filteredItems.map((item) => (
                    <ReconciliationItemComponent
                      key={item.filePath}
                      item={item}
                      isSelected={selectedItems.has(item.filePath)}
                      onToggle={() => handleToggleItem(item.filePath)}
                      selectable={item.status === 'new' || item.status === 'missing'}
                    />
                  ))
                )}
              </div>

              {/* Apply Button */}
              {selectedCount > 0 && (
                <div className="bg-[#1a1a1a] border border-[#303030] rounded-lg p-6">
                  <div className="flex items-center justify-between">
                    <div>
                      <h3 className="text-lg font-semibold mb-1">Ready to Apply Changes</h3>
                      <p className="text-sm text-gray-400">
                        {preview.items.filter(item => item.status === 'new' && selectedItems.has(item.filePath)).length} file(s) will be added,{' '}
                        {preview.items.filter(item => item.status === 'missing' && selectedItems.has(item.filePath)).length} clip(s) will be removed
                      </p>
                    </div>
                    <button
                      onClick={handleApply}
                      disabled={isApplying}
                      className="px-6 py-3 bg-green-600 text-white rounded-lg hover:bg-green-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors font-medium"
                    >
                      {isApplying ? 'Applying...' : 'Apply Selected Changes'}
                    </button>
                  </div>
                </div>
              )}

              {/* Results */}
              {result && (
                <div className="bg-[#1a1a1a] border border-[#303030] rounded-lg p-6">
                  <h2 className="text-xl font-semibold mb-4">Sync Results</h2>
                  <div className="grid grid-cols-3 gap-4 mb-4">
                    <div className="bg-[#2a2a2a] rounded-lg p-4">
                      <div className="text-2xl font-bold text-green-400">{result.totalAdded}</div>
                      <div className="text-sm text-gray-400 mt-1">Clips Added</div>
                    </div>
                    <div className="bg-[#2a2a2a] rounded-lg p-4">
                      <div className="text-2xl font-bold text-red-400">{result.totalRemoved}</div>
                      <div className="text-sm text-gray-400 mt-1">Clips Removed</div>
                    </div>
                    <div className="bg-[#2a2a2a] rounded-lg p-4">
                      <div className="text-2xl font-bold text-yellow-400">{result.errors.length}</div>
                      <div className="text-sm text-gray-400 mt-1">Errors</div>
                    </div>
                  </div>
                  {result.errors.length > 0 && (
                    <div className="space-y-2">
                      {result.errors.map((err, index) => (
                        <div
                          key={index}
                          className="bg-red-900/20 border border-red-700 rounded-lg p-3 text-sm"
                        >
                          <div className="font-medium text-red-300">{err.errorMessage}</div>
                          {err.filePath && (
                            <div className="text-gray-400 text-xs font-mono mt-1">{err.filePath}</div>
                          )}
                        </div>
                      ))}
                    </div>
                  )}
                </div>
              )}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}


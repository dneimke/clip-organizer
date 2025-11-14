'use client';

import { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { syncClips } from '@/lib/api/clips';
import { getRootFolder } from '@/lib/api/settings';
import { SyncResponse } from '@/types';

export default function SyncPage() {
  const router = useRouter();
  const [rootFolderPath, setRootFolderPath] = useState<string>('');
  const [configuredRootFolder, setConfiguredRootFolder] = useState<string>('');
  const [isLoading, setIsLoading] = useState(true);
  const [isSyncing, setIsSyncing] = useState(false);
  const [result, setResult] = useState<SyncResponse | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [isUsingDefault, setIsUsingDefault] = useState(true);

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

  const handleSync = async () => {
    const pathToUse = rootFolderPath.trim() || configuredRootFolder.trim();
    
    if (!pathToUse) {
      setError('Please enter a root folder path or configure one in Settings');
      return;
    }

    setIsSyncing(true);
    setError(null);
    setResult(null);

    try {
      // If using configured folder, send empty request to use default
      // Otherwise, send the custom path
      const response = await syncClips(pathToUse === configuredRootFolder ? '' : pathToUse);
      setResult(response);
    } catch (err: any) {
      setError(err.message || 'Failed to sync clips');
    } finally {
      setIsSyncing(false);
    }
  };

  const handleCancel = () => {
    router.back();
  };

  return (
    <div className="min-h-screen bg-[#0a0a0a] text-white">
      <div className="container mx-auto px-6 py-8">
        <div className="max-w-4xl mx-auto">
          {/* Header */}
          <div className="mb-8">
            <h1 className="text-3xl font-bold mb-2">Sync Filesystem</h1>
            <p className="text-gray-400">
              Scan a root folder and synchronize your video library with the local filesystem.
              New videos will be added, and clips whose files no longer exist will be removed.
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
                        disabled={isUsingDefault || isSyncing}
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
                    disabled={isSyncing}
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
                  {configuredRootFolder && !isUsingDefault && (
                    <p className="text-xs text-yellow-400 mt-1">
                      ⚠ Using custom folder path (different from configured default)
                    </p>
                  )}
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
                    onClick={handleCancel}
                    disabled={isSyncing || isLoading}
                    className="px-4 py-2 bg-[#3a3a3a] text-white rounded-lg hover:bg-[#4a4a4a] disabled:opacity-50 disabled:cursor-not-allowed transition-colors font-medium"
                  >
                    Cancel
                  </button>
                  <button
                    onClick={handleSync}
                    disabled={isSyncing || isLoading || (!rootFolderPath.trim() && !configuredRootFolder.trim())}
                    className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors font-medium"
                  >
                    {isSyncing ? 'Syncing...' : 'Start Sync'}
                  </button>
                </div>
              </>
            )}
          </div>

          {/* Results */}
          {result && (
            <div className="space-y-6">
              {/* Summary */}
              <div className="bg-[#1a1a1a] border border-[#303030] rounded-lg p-6">
                <h2 className="text-xl font-semibold mb-4">Sync Summary</h2>
                <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
                  <div className="bg-[#2a2a2a] rounded-lg p-4">
                    <div className="text-2xl font-bold text-blue-400">{result.totalScanned}</div>
                    <div className="text-sm text-gray-400 mt-1">Files Scanned</div>
                  </div>
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
              </div>

              {/* Added Clips */}
              {result.addedClips.length > 0 && (
                <div className="bg-[#1a1a1a] border border-[#303030] rounded-lg p-6">
                  <h2 className="text-xl font-semibold mb-4 text-green-400">
                    Added Clips ({result.addedClips.length})
                  </h2>
                  <div className="space-y-2 max-h-64 overflow-y-auto">
                    {result.addedClips.map((clip) => (
                      <div
                        key={clip.clipId}
                        className="bg-[#2a2a2a] rounded-lg p-3 text-sm"
                      >
                        <div className="font-medium text-white">{clip.title}</div>
                        <div className="text-gray-400 text-xs font-mono mt-1">{clip.filePath}</div>
                      </div>
                    ))}
                  </div>
                </div>
              )}

              {/* Removed Clips */}
              {result.removedClips.length > 0 && (
                <div className="bg-[#1a1a1a] border border-[#303030] rounded-lg p-6">
                  <h2 className="text-xl font-semibold mb-4 text-red-400">
                    Removed Clips ({result.removedClips.length})
                  </h2>
                  <div className="space-y-2 max-h-64 overflow-y-auto">
                    {result.removedClips.map((clip) => (
                      <div
                        key={clip.clipId}
                        className="bg-[#2a2a2a] rounded-lg p-3 text-sm"
                      >
                        <div className="font-medium text-white">{clip.title}</div>
                        <div className="text-gray-400 text-xs font-mono mt-1">{clip.filePath}</div>
                      </div>
                    ))}
                  </div>
                </div>
              )}

              {/* Errors */}
              {result.errors.length > 0 && (
                <div className="bg-[#1a1a1a] border border-[#303030] rounded-lg p-6">
                  <h2 className="text-xl font-semibold mb-4 text-yellow-400">
                    Errors ({result.errors.length})
                  </h2>
                  <div className="space-y-2 max-h-64 overflow-y-auto">
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
                </div>
              )}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}


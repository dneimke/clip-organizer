'use client';

import { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { getRootFolder, setRootFolder } from '@/lib/api/settings';

export default function SettingsPage() {
  const router = useRouter();
  const [rootFolderPath, setRootFolderPath] = useState<string>('');
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);

  useEffect(() => {
    loadRootFolder();
  }, []);

  const loadRootFolder = async () => {
    try {
      setIsLoading(true);
      setError(null);
      const setting = await getRootFolder();
      setRootFolderPath(setting.rootFolderPath || '');
    } catch (err: any) {
      setError(err.message || 'Failed to load root folder setting');
    } finally {
      setIsLoading(false);
    }
  };

  const handleSave = async () => {
    if (!rootFolderPath.trim()) {
      setError('Root folder path cannot be empty');
      return;
    }

    setIsSaving(true);
    setError(null);
    setSuccess(false);

    try {
      await setRootFolder(rootFolderPath.trim());
      setSuccess(true);
      setTimeout(() => setSuccess(false), 3000);
    } catch (err: any) {
      setError(err.message || 'Failed to save root folder setting');
    } finally {
      setIsSaving(false);
    }
  };

  const handleCancel = () => {
    router.back();
  };

  return (
    <div className="min-h-screen bg-[#0a0a0a] text-white">
      <div className="container mx-auto px-6 py-8">
        <div className="max-w-2xl mx-auto">
          {/* Header */}
          <div className="mb-8">
            <h1 className="text-3xl font-bold mb-2">Settings</h1>
            <p className="text-gray-400">
              Configure application settings including the root folder for video library synchronization.
            </p>
          </div>

          {/* Settings Form */}
          <div className="bg-[#1a1a1a] border border-[#303030] rounded-lg p-6">
            <h2 className="text-xl font-semibold mb-4">Video Library Root Folder</h2>
            <p className="text-sm text-gray-400 mb-4">
              Set the root folder path that will be used for automatic synchronization.
              This folder will be scanned recursively for video files.
            </p>

            {isLoading ? (
              <div className="text-center py-8">
                <p className="text-[#007BFF]">Loading settings...</p>
              </div>
            ) : (
              <>
                <div className="mb-4">
                  <label htmlFor="rootFolderPath" className="block text-sm font-medium text-white mb-2">
                    Root Folder Path
                  </label>
                  <input
                    id="rootFolderPath"
                    type="text"
                    value={rootFolderPath}
                    onChange={(e) => {
                      setRootFolderPath(e.target.value);
                      setError(null);
                      setSuccess(false);
                    }}
                    placeholder="C:\\Videos"
                    className="w-full px-4 py-2 border border-[#303030] rounded-lg bg-[#2a2a2a] text-white text-sm font-mono placeholder:text-gray-500 focus:outline-none focus:border-blue-500"
                    disabled={isSaving}
                  />
                  <p className="text-xs text-gray-500 mt-1">
                    Enter the absolute path to your video library root folder (e.g., C:\Videos)
                  </p>
                </div>

                {/* Error message */}
                {error && (
                  <div className="mb-4 bg-red-900/20 border border-red-700 text-red-300 px-4 py-3 rounded text-sm">
                    {error}
                  </div>
                )}

                {/* Success message */}
                {success && (
                  <div className="mb-4 bg-green-900/20 border border-green-700 text-green-300 px-4 py-3 rounded text-sm">
                    Root folder setting saved successfully!
                  </div>
                )}

                {/* Action buttons */}
                <div className="flex justify-end gap-3">
                  <button
                    onClick={handleCancel}
                    disabled={isSaving}
                    className="px-4 py-2 bg-[#3a3a3a] text-white rounded-lg hover:bg-[#4a4a4a] disabled:opacity-50 disabled:cursor-not-allowed transition-colors font-medium"
                  >
                    Cancel
                  </button>
                  <button
                    onClick={handleSave}
                    disabled={isSaving || !rootFolderPath.trim()}
                    className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors font-medium"
                  >
                    {isSaving ? 'Saving...' : 'Save'}
                  </button>
                </div>
              </>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}


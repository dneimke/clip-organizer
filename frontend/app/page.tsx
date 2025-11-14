'use client';

import { useState, useEffect, useCallback, useMemo } from 'react';
import { useRouter } from 'next/navigation';
import { Clip, Tag } from '@/types';
import { getClips, getUnclassifiedClips, syncClips } from '@/lib/api/clips';
import { getRootFolder } from '@/lib/api/settings';
import { getTags } from '@/lib/api/tags';
import SearchBar from '@/components/SearchBar';
import FilterSidebar from '@/components/FilterSidebar';
import ClipList from '@/components/ClipList';
import SortDropdown, { SortBy, SortOrder } from '@/components/SortDropdown';
import Toast from '@/components/Toast';

export default function Home() {
  const router = useRouter();
  const [clips, setClips] = useState<Clip[]>([]);
  const [tagsByCategory, setTagsByCategory] = useState<Record<string, Tag[]>>({});
  const [searchTerm, setSearchTerm] = useState('');
  const [selectedTagIds, setSelectedTagIds] = useState<number[]>([]);
  const [sortBy, setSortBy] = useState<SortBy>('dateAdded');
  const [sortOrder, setSortOrder] = useState<SortOrder>('desc');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [unclassifiedCount, setUnclassifiedCount] = useState(0);
  const [isQuickSyncing, setIsQuickSyncing] = useState(false);
  const [toast, setToast] = useState<{ message: string; type: 'success' | 'error' | 'info' } | null>(null);

  const loadTags = useCallback(async () => {
    try {
      const tags = await getTags();
      setTagsByCategory(tags);
    } catch (err) {
      console.error('Failed to load tags:', err);
    }
  }, []);

  const loadUnclassifiedCount = useCallback(async () => {
    try {
      const unclassified = await getUnclassifiedClips();
      setUnclassifiedCount(unclassified.length);
    } catch (err) {
      console.error('Failed to load unclassified count:', err);
    }
  }, []);

  const loadClips = useCallback(async () => {
    try {
      setLoading(true);
      const fetchedClips = await getClips(
        searchTerm || undefined,
        selectedTagIds.length > 0 ? selectedTagIds : undefined,
        sortBy,
        sortOrder
      );
      setClips(fetchedClips);
      setError(null);
    } catch (err: any) {
      setError(err.message || 'Failed to load clips');
      console.error('Error loading clips:', err);
    } finally {
      setLoading(false);
    }
  }, [searchTerm, selectedTagIds, sortBy, sortOrder]);

  useEffect(() => {
    loadTags();
    loadUnclassifiedCount();
  }, [loadTags, loadUnclassifiedCount]);

  useEffect(() => {
    loadClips();
  }, [loadClips]);

  useEffect(() => {
    // Refresh unclassified count after clips load
    loadUnclassifiedCount();
  }, [clips, loadUnclassifiedCount]);

  const handleTagToggle = (tagId: number) => {
    setSelectedTagIds((prev) =>
      prev.includes(tagId)
        ? prev.filter((id) => id !== tagId)
        : [...prev, tagId]
    );
  };

  const handleQuickSync = async () => {
    setIsQuickSyncing(true);
    setToast(null);

    try {
      // Check if root folder is configured
      const rootFolderSetting = await getRootFolder();
      if (!rootFolderSetting.rootFolderPath) {
        setToast({
          message: 'Root folder not configured. Please configure it in Settings first.',
          type: 'error',
        });
        setIsQuickSyncing(false);
        return;
      }

      // Perform sync with configured root folder
      const result = await syncClips();
      
      // Show success message with summary
      const summary = `Sync complete: ${result.totalScanned} scanned, ${result.totalAdded} added, ${result.totalRemoved} removed`;
      setToast({
        message: summary,
        type: 'success',
      });

      // Reload clips to show updated list
      await loadClips();
      await loadUnclassifiedCount();
    } catch (err: any) {
      setToast({
        message: err.message || 'Failed to sync clips',
        type: 'error',
      });
    } finally {
      setIsQuickSyncing(false);
    }
  };

  return (
    <div className="min-h-screen bg-[#121212]">
      {/* Header */}
      <header className="bg-[#121212] border-b border-[#303030]">
        <div className="container mx-auto px-6 py-6">
          <div className="flex justify-between items-center">
            <h1 className="text-3xl font-bold text-white">
              Clip AI
            </h1>
            <div className="flex items-center gap-3">
              {unclassifiedCount > 0 && (
                <button
                  onClick={() => router.push('/clips/bulk-classify')}
                  className="px-4 py-2 bg-yellow-600 text-white rounded-lg hover:bg-yellow-700 transition-colors font-medium flex items-center gap-2 relative"
                >
                  <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M7 7h.01M7 3h5c.512 0 1.024.195 1.414.586l7 7a2 2 0 010 2.828l-7 7a2 2 0 01-2.828 0l-7-7A1.994 1.994 0 013 12V7a4 4 0 014-4z" />
                  </svg>
                  Unclassified ({unclassifiedCount})
                  {unclassifiedCount > 0 && (
                    <span className="absolute -top-2 -right-2 bg-red-500 text-white text-xs font-bold rounded-full w-5 h-5 flex items-center justify-center">
                      {unclassifiedCount}
                    </span>
                  )}
                </button>
              )}
              <button
                onClick={handleQuickSync}
                disabled={isQuickSyncing}
                className="px-4 py-2 bg-purple-600 text-white rounded-lg hover:bg-purple-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors font-medium flex items-center gap-2"
                title="Quick sync with configured root folder"
              >
                <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
                </svg>
                {isQuickSyncing ? 'Syncing...' : 'Quick Sync'}
              </button>
              <button
                onClick={() => router.push('/clips/bulk-upload')}
                className="px-4 py-2 bg-green-600 text-white rounded-lg hover:bg-green-700 transition-colors font-medium flex items-center gap-2"
              >
                <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M7 16a4 4 0 01-.88-7.903A5 5 0 1115.9 6L16 6a5 5 0 011 9.9M15 13l-3-3m0 0l-3 3m3-3v12" />
                </svg>
                Bulk Upload
              </button>
              <button
                onClick={() => router.push('/settings')}
                className="px-4 py-2 bg-[#202020] text-white rounded-lg hover:bg-[#303030] transition-colors font-medium flex items-center gap-2 border border-[#303030]"
                title="Configure application settings"
              >
                <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z" />
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                </svg>
                Settings
              </button>
              <button
                onClick={() => router.push('/clips/new')}
                className="px-4 py-2 bg-[#007BFF] text-white rounded-lg hover:bg-[#0056b3] transition-colors font-medium flex items-center gap-2"
              >
                <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
                </svg>
                Add New Clip
              </button>
            </div>
          </div>
        </div>
      </header>

      {/* Main Content */}
      <div className="container mx-auto px-6 py-8">
        <div className="mb-6 flex flex-col sm:flex-row gap-4 items-start sm:items-center justify-between">
          <div className="flex-1 w-full sm:w-auto">
            <SearchBar
              value={searchTerm}
              onChange={setSearchTerm}
              placeholder="Search clips by title..."
            />
          </div>
          <SortDropdown
            sortBy={sortBy}
            sortOrder={sortOrder}
            onSortByChange={setSortBy}
            onSortOrderChange={setSortOrder}
          />
        </div>

        <div className="flex gap-6">
          <FilterSidebar
            tagsByCategory={tagsByCategory}
            selectedTagIds={selectedTagIds}
            onTagToggle={handleTagToggle}
          />

          <div className="flex-1">
            {loading ? (
              <div className="text-center py-12">
                <p className="text-[#007BFF]">Loading clips...</p>
              </div>
            ) : error ? (
              <div className="bg-red-900/20 border border-red-700 text-red-300 px-4 py-3 rounded">
                {error}
              </div>
            ) : (
              <ClipList clips={clips} />
            )}
          </div>
        </div>
      </div>

      {/* Toast Notification */}
      {toast && (
        <Toast
          message={toast.message}
          type={toast.type}
          onClose={() => setToast(null)}
        />
      )}
    </div>
  );
}

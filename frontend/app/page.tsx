'use client';

import { useState, useEffect, useCallback, useMemo } from 'react';
import { useRouter } from 'next/navigation';
import { Clip, Tag } from '@/types';
import { getClips, getUnclassifiedClips } from '@/lib/api/clips';
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


  return (
    <div className="min-h-screen bg-[#121212]">
      {/* Main Content */}
      <div className="container mx-auto px-4 sm:px-6 py-6 sm:py-8">
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

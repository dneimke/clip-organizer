'use client';

import { useState, useEffect, useCallback } from 'react';
import { Clip, Tag } from '@/types';
import { getClips, parseQuery } from '@/lib/api/clips';
import { getTags } from '@/lib/api/tags';
import AIQueryInput from '@/components/AIQueryInput';
import ClipList, { ViewMode } from '@/components/ClipList';
import ViewToggle from '@/components/ViewToggle';
import Toast from '@/components/Toast';
import { QueryParseResult } from '@/types';

export default function Home() {
  const [clips, setClips] = useState<Clip[]>([]);
  const [parsedFilters, setParsedFilters] = useState<QueryParseResult | null>(null);
  const [tagsByCategory, setTagsByCategory] = useState<Record<string, Tag[]>>({});
  const [loading, setLoading] = useState(false);
  const [parsing, setParsing] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [toast, setToast] = useState<{ message: string; type: 'success' | 'error' | 'info' } | null>(null);
  const [viewMode, setViewMode] = useState<ViewMode>('card');
  const [hasSearched, setHasSearched] = useState(false);
  const [diagnosticsExpanded, setDiagnosticsExpanded] = useState(false);
  const [queryClearSignal, setQueryClearSignal] = useState(0);

  // Load view mode from localStorage after hydration
  useEffect(() => {
    const saved = localStorage.getItem('clipViewMode');
    if (saved === 'card' || saved === 'list') {
      setViewMode(saved);
    }
  }, []);

  // Load tags for displaying tag names
  useEffect(() => {
    const loadTags = async () => {
      try {
        const tags = await getTags();
        setTagsByCategory(tags);
      } catch (err) {
        console.error('Failed to load tags:', err);
      }
    };
    loadTags();
  }, []);

  const handleQuerySubmit = useCallback(async (userQuery: string) => {
    setError(null);
    setParsing(true);
    setHasSearched(true);

    try {
      const parseResult = await parseQuery(userQuery);
      setParsedFilters(parseResult);

      setLoading(true);
      const fetchedClips = await getClips(
        parseResult.searchTerm || undefined,
        parseResult.tagIds.length > 0 ? parseResult.tagIds : undefined,
        parseResult.subfolders.length > 0 ? parseResult.subfolders : undefined,
        parseResult.sortBy || undefined,
        parseResult.sortOrder || undefined,
        parseResult.unclassifiedOnly,
        parseResult.favoriteOnly
      );
      setClips(fetchedClips);
      setError(null);
    } catch (err: unknown) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to parse query or load clips';
      setError(errorMessage);
      setToast({
        message: errorMessage,
        type: 'error',
      });
      setParsedFilters(null);
      setClips([]);
    } finally {
      setParsing(false);
      setLoading(false);
    }
  }, []);

  const handleClear = () => {
    setParsedFilters(null);
    setClips([]);
    setError(null);
    setHasSearched(false);
    setQueryClearSignal(prev => prev + 1);
  };

  const handleViewModeChange = (mode: ViewMode) => {
    setViewMode(mode);
    if (typeof window !== 'undefined') {
      localStorage.setItem('clipViewMode', mode);
    }
  };

  return (
    <div className="min-h-screen bg-[#121212]">
      {/* Main Content */}
      <div className="container mx-auto px-4 sm:px-6 py-6 sm:py-8">
        {/* AI Query Input */}
        <div className="mb-6">
          <AIQueryInput
            onQuerySubmit={handleQuerySubmit}
            isLoading={parsing}
            clearSignal={queryClearSignal}
          />
        </div>

        {/* Diagnostics Panel - Collapsible */}
        {parsedFilters && (parsedFilters.interpretedQuery || parsedFilters.tagIds.length > 0 || parsedFilters.subfolders.length > 0 || parsedFilters.searchTerm || parsedFilters.unclassifiedOnly || parsedFilters.favoriteOnly) && (
          <div className="mb-6 bg-[#202020] border border-[#303030] rounded-lg overflow-hidden">
            <button
              onClick={() => setDiagnosticsExpanded(!diagnosticsExpanded)}
              className="w-full px-4 py-3 flex items-center justify-between hover:bg-[#303030] transition-colors"
            >
              <div className="flex items-center gap-2">
                <span className="text-sm font-semibold text-white">Diagnostics</span>
                <span className="text-xs text-gray-400">(AI interpretation & filters)</span>
              </div>
              <div className="flex items-center gap-2">
                {parsedFilters.tagIds.length > 0 && (
                  <span className="text-xs bg-[#007BFF]/20 text-[#007BFF] px-2 py-0.5 rounded-full">
                    {parsedFilters.tagIds.length} tag{parsedFilters.tagIds.length !== 1 ? 's' : ''}
                  </span>
                )}
                {parsedFilters.searchTerm && (
                  <span className="text-xs bg-[#007BFF]/20 text-[#007BFF] px-2 py-0.5 rounded-full">
                    Search
                  </span>
                )}
                {parsedFilters.favoriteOnly && (
                  <span className="text-xs bg-[#ef4444]/20 text-[#ef4444] px-2 py-0.5 rounded-full">
                    Favourites only
                  </span>
                )}
                <svg
                  className={`w-4 h-4 text-gray-400 transition-transform ${diagnosticsExpanded ? 'rotate-180' : ''}`}
                  fill="none"
                  stroke="currentColor"
                  viewBox="0 0 24 24"
                >
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
                </svg>
              </div>
            </button>
            
            {diagnosticsExpanded && (
              <div className="px-4 pb-4 space-y-4 border-t border-[#303030] pt-4">
                {/* AI Interpretation */}
                {parsedFilters.interpretedQuery && (
                  <div>
                    <p className="text-xs text-gray-400 mb-1">AI Interpretation:</p>
                    <p className="text-sm text-white">{parsedFilters.interpretedQuery}</p>
                  </div>
                )}

                {/* Applied Filters */}
                {(parsedFilters.tagIds.length > 0 || parsedFilters.subfolders.length > 0 || parsedFilters.searchTerm || parsedFilters.unclassifiedOnly || parsedFilters.favoriteOnly) && (
                  <div>
                    <p className="text-xs text-gray-400 mb-2">Applied Filters:</p>
                    <div className="flex flex-wrap gap-2">
                      {parsedFilters.searchTerm && (
                        <span className="px-3 py-1 bg-[#007BFF]/20 text-[#007BFF] rounded-full text-sm">
                          Search: &quot;{parsedFilters.searchTerm}&quot;
                        </span>
                      )}
                      {parsedFilters.tagIds.length > 0 && (() => {
                        // Get all tags from all categories
                        const allTags = Object.values(tagsByCategory).flat();
                        const selectedTags = allTags.filter(tag => parsedFilters.tagIds.includes(tag.id));
                        return selectedTags.map(tag => (
                          <span key={tag.id} className="px-3 py-1 bg-[#007BFF]/20 text-[#007BFF] rounded-full text-sm">
                            {tag.value}
                          </span>
                        ));
                      })()}
                      {parsedFilters.subfolders.length > 0 && parsedFilters.subfolders.map((subfolder, index) => (
                        <span key={index} className="px-3 py-1 bg-[#007BFF]/20 text-[#007BFF] rounded-full text-sm">
                          {subfolder}
                        </span>
                      ))}
                      {parsedFilters.favoriteOnly && (
                        <span className="px-3 py-1 bg-[#ef4444]/20 text-[#ef4444] rounded-full text-sm">
                          Favourites only
                        </span>
                      )}
                      {parsedFilters.unclassifiedOnly && (
                        <span className="px-3 py-1 bg-yellow-600/20 text-yellow-400 rounded-full text-sm">
                          Unclassified only
                        </span>
                      )}
                      {parsedFilters.sortBy && (
                        <span className="px-3 py-1 bg-[#007BFF]/20 text-[#007BFF] rounded-full text-sm">
                          Sort: {parsedFilters.sortBy} ({parsedFilters.sortOrder || 'desc'})
                        </span>
                      )}
                    </div>
                  </div>
                )}

                {/* Clear Button */}
                <div className="pt-2">
                  <button
                    onClick={handleClear}
                    className="text-sm text-[#007BFF] hover:text-[#0099FF] transition-colors"
                  >
                    Clear all filters
                  </button>
                </div>
              </div>
            )}
          </div>
        )}

        {/* Results */}
        <div className="mb-4 flex items-center justify-between">
          <div>
            {hasSearched && !loading && !parsing && (
              <p className="text-gray-400 text-sm">
                Found {clips.length} clip{clips.length !== 1 ? 's' : ''}
              </p>
            )}
          </div>
          {hasSearched && !loading && !parsing && !error && clips.length > 0 && (
            <div className="flex items-center gap-4">
              <ViewToggle viewMode={viewMode} onViewModeChange={handleViewModeChange} />
            </div>
          )}
        </div>

        {parsing ? (
          <div className="text-center py-12">
            <p className="text-[#007BFF]">Parsing your query...</p>
          </div>
        ) : loading ? (
          <div className="text-center py-12">
            <p className="text-[#007BFF]">Loading clips...</p>
          </div>
        ) : error ? (
          <div className="bg-red-900/20 border border-red-700 text-red-300 px-4 py-3 rounded">
            {error}
          </div>
        ) : hasSearched && clips.length === 0 ? (
          <div className="text-center py-12">
            <p className="text-gray-400 mb-4">No clips found matching your query.</p>
            <button
              onClick={handleClear}
              className="px-4 py-2 bg-[#007BFF] text-white rounded-lg hover:bg-[#0056b3] transition-colors"
            >
              Try a different query
            </button>
          </div>
        ) : hasSearched ? (
          <ClipList clips={clips} viewMode={viewMode} />
        ) : null}
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

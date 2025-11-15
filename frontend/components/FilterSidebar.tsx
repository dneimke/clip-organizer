'use client';

import { useState, useMemo } from 'react';
import { Tag } from '@/types';

interface FilterSidebarProps {
  tagsByCategory: Record<string, Tag[]>;
  selectedTagIds: number[];
  onTagToggle: (tagId: number) => void;
  subfolders: string[];
  selectedSubfolders: string[];
  onSubfolderToggle: (subfolder: string) => void;
  onClearFilters?: () => void;
  unclassifiedCount: number;
  showUnclassifiedFilter: boolean;
  unclassifiedFilterActive: boolean;
  onUnclassifiedFilterToggle: () => void;
}

export default function FilterSidebar({ 
  tagsByCategory, 
  selectedTagIds, 
  onTagToggle,
  subfolders,
  selectedSubfolders,
  onSubfolderToggle,
  onClearFilters,
  unclassifiedCount,
  showUnclassifiedFilter,
  unclassifiedFilterActive,
  onUnclassifiedFilterToggle
}: FilterSidebarProps) {
  const [tagsExpanded, setTagsExpanded] = useState(true);
  const [subfoldersExpanded, setSubfoldersExpanded] = useState(true);
  const [unclassifiedExpanded, setUnclassifiedExpanded] = useState(true);
  const [subfolderSearch, setSubfolderSearch] = useState('');

  const hasActiveFilters = selectedTagIds.length > 0 || selectedSubfolders.length > 0 || unclassifiedFilterActive;

  const filteredSubfolders = useMemo(() => {
    if (!subfolderSearch.trim()) return subfolders;
    const searchLower = subfolderSearch.toLowerCase();
    return subfolders.filter(s => s.toLowerCase().includes(searchLower));
  }, [subfolders, subfolderSearch]);

  const totalTags = Object.values(tagsByCategory).reduce((sum, tags) => sum + tags.length, 0);
  const selectedTagsCount = selectedTagIds.length;
  const selectedSubfoldersCount = selectedSubfolders.length;

  return (
    <div className="w-64 bg-[#202020] border border-[#303030] rounded-lg overflow-hidden flex flex-col max-h-[calc(100vh-8rem)]">
      {/* Header */}
      <div className="p-4 border-b border-[#303030] flex items-center justify-between">
        <h2 className="text-lg font-semibold text-white">Filters</h2>
        {hasActiveFilters && onClearFilters && (
          <button
            onClick={onClearFilters}
            className="text-xs text-[#007BFF] hover:text-[#0099FF] transition-colors"
          >
            Clear All
          </button>
        )}
      </div>

      {/* Scrollable Content */}
      <div className="flex-1 overflow-y-auto p-4 space-y-4">
        {/* Tags Section */}
        <div className="border-b border-[#303030] pb-4 last:border-0">
          <button
            onClick={() => setTagsExpanded(!tagsExpanded)}
            className="w-full flex items-center justify-between mb-3 text-left"
          >
            <div className="flex items-center gap-2">
              <h3 className="text-sm font-semibold text-white">Tags</h3>
              {selectedTagsCount > 0 && (
                <span className="text-xs bg-[#007BFF] text-white px-2 py-0.5 rounded-full">
                  {selectedTagsCount}
                </span>
              )}
            </div>
            <svg
              className={`w-4 h-4 text-gray-400 transition-transform ${tagsExpanded ? 'rotate-180' : ''}`}
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
            >
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
            </svg>
          </button>

          {tagsExpanded && (
            <div className="space-y-3">
              {Object.entries(tagsByCategory).map(([category, tags]) => (
                <div key={category}>
                  <h4 className="text-xs font-medium text-gray-400 mb-2 uppercase tracking-wide">
                    {category.replace(/([A-Z])/g, ' $1').trim()}
                  </h4>
                  <div className="space-y-1">
                    {tags.map((tag) => (
                      <label
                        key={tag.id}
                        className="flex items-center space-x-2 cursor-pointer hover:bg-[#303030] px-2 py-1.5 rounded transition-colors group"
                      >
                        <input
                          type="checkbox"
                          checked={selectedTagIds.includes(tag.id)}
                          onChange={() => onTagToggle(tag.id)}
                          className="w-4 h-4 text-[#007BFF] border-gray-600 rounded focus:ring-[#007BFF] focus:ring-2 bg-[#303030] checked:bg-[#007BFF]"
                        />
                        <span className="text-sm text-gray-300 group-hover:text-white transition-colors">
                          {tag.value}
                        </span>
                      </label>
                    ))}
                  </div>
                </div>
              ))}
              {totalTags === 0 && (
                <p className="text-xs text-gray-400 italic">No tags available</p>
              )}
            </div>
          )}
        </div>

        {/* Subfolders Section */}
        <div className="border-b border-[#303030] pb-4 last:border-0">
          <button
            onClick={() => setSubfoldersExpanded(!subfoldersExpanded)}
            className="w-full flex items-center justify-between mb-3 text-left"
          >
            <div className="flex items-center gap-2">
              <h3 className="text-sm font-semibold text-white">Subfolders</h3>
              {selectedSubfoldersCount > 0 && (
                <span className="text-xs bg-[#007BFF] text-white px-2 py-0.5 rounded-full">
                  {selectedSubfoldersCount}
                </span>
              )}
            </div>
            <svg
              className={`w-4 h-4 text-gray-400 transition-transform ${subfoldersExpanded ? 'rotate-180' : ''}`}
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
            >
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
            </svg>
          </button>

          {subfoldersExpanded && (
            <div className="space-y-2">
              {subfolders.length > 5 && (
                <input
                  type="text"
                  value={subfolderSearch}
                  onChange={(e) => setSubfolderSearch(e.target.value)}
                  placeholder="Search subfolders..."
                  className="w-full px-3 py-1.5 text-sm bg-[#303030] border border-[#404040] rounded text-white placeholder:text-gray-500 focus:outline-none focus:ring-2 focus:ring-[#007BFF] focus:border-transparent"
                />
              )}

              {filteredSubfolders.length === 0 ? (
                <p className="text-xs text-gray-400 italic">
                  {subfolderSearch ? 'No subfolders match your search' : 'No subfolders available'}
                </p>
              ) : (
                <div className="space-y-1 max-h-64 overflow-y-auto">
                  {filteredSubfolders.map((subfolder) => (
                    <label
                      key={subfolder}
                      className="flex items-center space-x-2 cursor-pointer hover:bg-[#303030] px-2 py-1.5 rounded transition-colors group"
                    >
                      <input
                        type="checkbox"
                        checked={selectedSubfolders.includes(subfolder)}
                        onChange={() => onSubfolderToggle(subfolder)}
                        className="w-4 h-4 text-[#007BFF] border-gray-600 rounded focus:ring-[#007BFF] focus:ring-2 bg-[#303030] checked:bg-[#007BFF]"
                      />
                      <span className="text-sm text-gray-300 group-hover:text-white transition-colors truncate">
                        {subfolder}
                      </span>
                    </label>
                  ))}
                </div>
              )}
            </div>
          )}
        </div>

        {/* Unclassified Section */}
        {showUnclassifiedFilter && (
          <div className="border-b border-[#303030] pb-4 last:border-0">
            <button
              onClick={() => setUnclassifiedExpanded(!unclassifiedExpanded)}
              className="w-full flex items-center justify-between mb-3 text-left"
            >
              <div className="flex items-center gap-2">
                <h3 className="text-sm font-semibold text-white">Unclassified</h3>
                {unclassifiedFilterActive && (
                  <span className="text-xs bg-yellow-600 text-white px-2 py-0.5 rounded-full">
                    1
                  </span>
                )}
              </div>
              <svg
                className={`w-4 h-4 text-gray-400 transition-transform ${unclassifiedExpanded ? 'rotate-180' : ''}`}
                fill="none"
                stroke="currentColor"
                viewBox="0 0 24 24"
              >
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
              </svg>
            </button>

            {unclassifiedExpanded && (
              <div className="space-y-2">
                <label className="flex items-center space-x-2 cursor-pointer hover:bg-[#303030] px-2 py-1.5 rounded transition-colors group">
                  <input
                    type="checkbox"
                    checked={unclassifiedFilterActive}
                    onChange={onUnclassifiedFilterToggle}
                    className="w-4 h-4 text-yellow-600 border-gray-600 rounded focus:ring-yellow-600 focus:ring-2 bg-[#303030] checked:bg-yellow-600"
                  />
                  <span className="text-sm text-gray-300 group-hover:text-white transition-colors">
                    Show unclassified only ({unclassifiedCount})
                  </span>
                </label>
              </div>
            )}
          </div>
        )}
      </div>
    </div>
  );
}


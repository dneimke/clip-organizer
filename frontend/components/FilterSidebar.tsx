'use client';

import { Tag } from '@/types';

interface FilterSidebarProps {
  tagsByCategory: Record<string, Tag[]>;
  selectedTagIds: number[];
  onTagToggle: (tagId: number) => void;
  subfolders: string[];
  selectedSubfolders: string[];
  onSubfolderToggle: (subfolder: string) => void;
}

export default function FilterSidebar({ 
  tagsByCategory, 
  selectedTagIds, 
  onTagToggle,
  subfolders,
  selectedSubfolders,
  onSubfolderToggle
}: FilterSidebarProps) {
  return (
    <div className="w-64 bg-[#202020] border border-[#303030] p-4 rounded-lg h-fit">
      <h2 className="text-lg font-semibold mb-4 text-white">Filter by Tags</h2>
      <div className="space-y-4 mb-6">
        {Object.entries(tagsByCategory).map(([category, tags]) => (
          <div key={category}>
            <h3 className="text-sm font-medium text-gray-300 mb-2 capitalize">
              {category.replace(/([A-Z])/g, ' $1').trim()}
            </h3>
            <div className="space-y-2">
              {tags.map((tag) => (
                <label
                  key={tag.id}
                  className="flex items-center space-x-2 cursor-pointer hover:bg-[#303030] p-2 rounded transition-colors"
                >
                  <input
                    type="checkbox"
                    checked={selectedTagIds.includes(tag.id)}
                    onChange={() => onTagToggle(tag.id)}
                    className="w-4 h-4 text-[#007BFF] border-gray-600 rounded focus:ring-[#007BFF] focus:ring-2 bg-[#303030] checked:bg-[#007BFF]"
                  />
                  <span className="text-sm text-gray-300">{tag.value}</span>
                </label>
              ))}
            </div>
          </div>
        ))}
      </div>

      <h2 className="text-lg font-semibold mb-4 text-white">Filter by Subfolder</h2>
      <div className="space-y-2">
        {subfolders.length === 0 ? (
          <p className="text-sm text-gray-400">No subfolders available</p>
        ) : (
          subfolders.map((subfolder) => (
            <label
              key={subfolder}
              className="flex items-center space-x-2 cursor-pointer hover:bg-[#303030] p-2 rounded transition-colors"
            >
              <input
                type="checkbox"
                checked={selectedSubfolders.includes(subfolder)}
                onChange={() => onSubfolderToggle(subfolder)}
                className="w-4 h-4 text-[#007BFF] border-gray-600 rounded focus:ring-[#007BFF] focus:ring-2 bg-[#303030] checked:bg-[#007BFF]"
              />
              <span className="text-sm text-gray-300">{subfolder}</span>
            </label>
          ))
        )}
      </div>
    </div>
  );
}


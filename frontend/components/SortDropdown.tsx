'use client';

export type SortBy = 'title' | 'dateAdded';
export type SortOrder = 'asc' | 'desc';

interface SortDropdownProps {
  sortBy: SortBy;
  sortOrder: SortOrder;
  onSortByChange: (sortBy: SortBy) => void;
  onSortOrderChange: (sortOrder: SortOrder) => void;
}

export default function SortDropdown({ 
  sortBy, 
  sortOrder, 
  onSortByChange, 
  onSortOrderChange 
}: SortDropdownProps) {
  return (
    <div className="flex items-center gap-3">
      <div className="flex items-center gap-2">
        <label className="text-sm text-gray-300 font-medium">Sort by:</label>
        <select
          value={sortBy}
          onChange={(e) => onSortByChange(e.target.value as SortBy)}
          className="px-3 py-2 border border-[#303030] rounded-lg focus:outline-none focus:ring-2 focus:ring-[#007BFF] focus:border-[#007BFF] bg-[#202020] text-white text-sm"
        >
          <option value="dateAdded">Date Added</option>
          <option value="title">Title</option>
        </select>
      </div>
      
      <div className="flex items-center gap-2">
        <label className="text-sm text-gray-300 font-medium">Order:</label>
        <select
          value={sortOrder}
          onChange={(e) => onSortOrderChange(e.target.value as SortOrder)}
          className="px-3 py-2 border border-[#303030] rounded-lg focus:outline-none focus:ring-2 focus:ring-[#007BFF] focus:border-[#007BFF] bg-[#202020] text-white text-sm"
        >
          <option value="desc">Descending</option>
          <option value="asc">Ascending</option>
        </select>
      </div>
    </div>
  );
}







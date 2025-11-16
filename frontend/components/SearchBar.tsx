'use client';

interface SearchBarProps {
  value: string;
  onChange: (value: string) => void;
  placeholder?: string;
  favoriteOnly?: boolean;
  onFavoriteOnlyChange?: (value: boolean) => void;
}

export default function SearchBar({ value, onChange, placeholder = "Search clips...", favoriteOnly, onFavoriteOnlyChange }: SearchBarProps) {
  return (
    <div className="w-full flex items-center gap-3">
      <div className="relative flex-1">
        <input
          type="text"
          value={value}
          onChange={(e) => onChange(e.target.value)}
          placeholder={placeholder}
          className="w-full px-4 py-2 pl-10 border border-[#303030] rounded-lg focus:outline-none focus:ring-2 focus:ring-[#007BFF] focus:border-[#007BFF] bg-[#202020] text-white placeholder:text-gray-500"
        />
        <svg
          className="absolute left-3 top-2.5 h-5 w-5 text-gray-500"
          fill="none"
          stroke="currentColor"
          viewBox="0 0 24 24"
        >
          <path
            strokeLinecap="round"
            strokeLinejoin="round"
            strokeWidth={2}
            d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"
          />
        </svg>
      </div>
      {typeof favoriteOnly !== 'undefined' && onFavoriteOnlyChange && (
        <label className="flex items-center gap-2 text-sm text-gray-300 select-none">
          <input
            type="checkbox"
            className="h-4 w-4"
            checked={favoriteOnly}
            onChange={(e) => onFavoriteOnlyChange(e.target.checked)}
          />
          Favourites only
        </label>
      )}
    </div>
  );
}


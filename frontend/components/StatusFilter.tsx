'use client';

type StatusFilter = 'all' | 'new' | 'missing' | 'matched' | 'error';

interface StatusFilterProps {
  selectedStatus: StatusFilter;
  onStatusChange: (status: StatusFilter) => void;
  counts: {
    all: number;
    new: number;
    missing: number;
    matched: number;
    error: number;
  };
}

export default function StatusFilter({ selectedStatus, onStatusChange, counts }: StatusFilterProps) {
  const filters: { value: StatusFilter; label: string; color: string }[] = [
    { value: 'all', label: 'All', color: 'text-gray-400' },
    { value: 'matched', label: 'Matched', color: 'text-green-400' },
    { value: 'new', label: 'New Files', color: 'text-yellow-400' },
    { value: 'missing', label: 'Missing', color: 'text-red-400' },
    { value: 'error', label: 'Errors', color: 'text-gray-400' },
  ];

  return (
    <div className="flex flex-wrap gap-2">
      {filters.map((filter) => (
        <button
          key={filter.value}
          onClick={() => onStatusChange(filter.value)}
          className={`px-4 py-2 rounded-lg font-medium transition-colors ${
            selectedStatus === filter.value
              ? `${filter.color.replace('text-', 'bg-').replace('-400', '-500/20')} ${filter.color} border-2 border-current`
              : 'bg-[#2a2a2a] text-gray-400 hover:bg-[#3a3a3a] border-2 border-transparent'
          }`}
        >
          {filter.label} ({counts[filter.value]})
        </button>
      ))}
    </div>
  );
}


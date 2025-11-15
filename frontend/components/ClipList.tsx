'use client';

import { Clip } from '@/types';
import ClipCard from './ClipCard';
import ClipListItem from './ClipListItem';

export type ViewMode = 'card' | 'list';

interface ClipListProps {
  clips: Clip[];
  viewMode?: ViewMode;
}

export default function ClipList({ clips, viewMode = 'card' }: ClipListProps) {
  if (clips.length === 0) {
    return (
      <div className="text-center py-12">
        <p className="text-[#007BFF]">No clips found. Create your first clip!</p>
      </div>
    );
  }

  if (viewMode === 'list') {
    return (
      <div className="space-y-2">
        {clips.map((clip) => (
          <ClipListItem key={clip.id} clip={clip} />
        ))}
      </div>
    );
  }

  return (
    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
      {clips.map((clip) => (
        <ClipCard key={clip.id} clip={clip} />
      ))}
    </div>
  );
}


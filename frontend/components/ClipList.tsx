'use client';

import { Clip } from '@/types';
import ClipCard from './ClipCard';

interface ClipListProps {
  clips: Clip[];
}

export default function ClipList({ clips }: ClipListProps) {
  if (clips.length === 0) {
    return (
      <div className="text-center py-12">
        <p className="text-[#007BFF]">No clips found. Create your first clip!</p>
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


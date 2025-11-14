'use client';

import Link from 'next/link';
import { Clip } from '@/types';

interface ClipCardProps {
  clip: Clip;
}

export default function ClipCard({ clip }: ClipCardProps) {
  const formatDuration = (seconds: number) => {
    if (seconds === 0) return 'N/A';
    const mins = Math.floor(seconds / 60);
    const secs = seconds % 60;
    return `${mins}:${secs.toString().padStart(2, '0')}`;
  };

  const isUnclassified = clip.isUnclassified ?? false;

  return (
    <Link href={`/clips/${clip.id}`}>
      <div className={`bg-[#202020] rounded-lg overflow-hidden hover:bg-[#252525] transition-all cursor-pointer flex flex-col h-full ${isUnclassified ? 'border-2 border-yellow-500/50' : ''}`}>
        {/* Video Placeholder */}
        <div className="relative bg-[#303030] w-full aspect-video flex items-center justify-center">
          {isUnclassified && (
            <div className="absolute top-2 right-2 bg-yellow-500 text-yellow-900 text-xs font-bold px-2 py-1 rounded">
              Unclassified
            </div>
          )}
          <svg
            className="w-16 h-16 text-white/70"
            fill="currentColor"
            viewBox="0 0 24 24"
          >
            <path d="M8 5v14l11-7z" />
          </svg>
        </div>

        {/* Content */}
        <div className="p-4 flex flex-col flex-1">
          {/* Title */}
          <h3 className="text-lg font-semibold text-white mb-2 line-clamp-2">
            {clip.title}
          </h3>

          {/* Description */}
          <p className="text-sm text-gray-400 mb-3 line-clamp-2">
            {clip.storageType === 'YouTube' 
              ? `YouTube video • ${formatDuration(clip.duration)}`
              : `Local file • ${formatDuration(clip.duration)}`
            }
          </p>

          {/* Tags */}
          {clip.tags.length > 0 && (
            <div className="flex flex-wrap gap-2 mt-auto">
              {clip.tags.slice(0, 3).map((tag) => (
                <span
                  key={tag.id}
                  className="px-2 py-1 text-xs bg-[#007BFF] text-white rounded-full font-medium"
                >
                  {tag.value}
                </span>
              ))}
              {clip.tags.length > 3 && (
                <span className="px-2 py-1 text-xs text-gray-400 font-medium">
                  +{clip.tags.length - 3}
                </span>
              )}
            </div>
          )}
        </div>
      </div>
    </Link>
  );
}


'use client';

import Link from 'next/link';
import Image from 'next/image';
import { Clip } from '@/types';

interface ClipListItemProps {
  clip: Clip;
}

export default function ClipListItem({ clip }: ClipListItemProps) {
  const formatDuration = (seconds: number) => {
    if (seconds === 0) return 'N/A';
    const mins = Math.floor(seconds / 60);
    const secs = seconds % 60;
    return `${mins}:${secs.toString().padStart(2, '0')}`;
  };

  const isUnclassified = clip.isUnclassified ?? false;

  return (
    <Link href={`/clips/${clip.id}`}>
      <div className={`bg-[#202020] rounded-lg p-4 hover:bg-[#252525] transition-all cursor-pointer border-b border-[#303030] last:border-b-0 ${isUnclassified ? 'border-l-4 border-l-yellow-500' : ''}`}>
        <div className="flex items-start gap-4">
          {/* Thumbnail */}
          <div className="relative bg-[#303030] w-32 h-20 flex-shrink-0 rounded overflow-hidden flex items-center justify-center">
            {isUnclassified && (
              <div className="absolute top-1 right-1 z-10 bg-yellow-500 text-yellow-900 text-xs font-bold px-1.5 py-0.5 rounded">
                Unclassified
              </div>
            )}
            {clip.thumbnailPath ? (
              clip.storageType === 'YouTube' ? (
                <Image
                  src={clip.thumbnailPath}
                  alt={clip.title}
                  fill
                  className="object-cover"
                  unoptimized
                  onError={(e) => {
                    // Fallback to placeholder if image fails to load
                    const target = e.currentTarget;
                    target.style.display = 'none';
                    const parent = target.parentElement;
                    const placeholder = parent?.querySelector('.placeholder-icon') as HTMLElement;
                    if (placeholder) {
                      placeholder.classList.remove('hidden');
                    }
                  }}
                />
              ) : (
                <Image
                  src={`${process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5059'}/api/clips/${clip.id}/thumbnail`}
                  alt={clip.title}
                  fill
                  className="object-cover"
                  onError={(e) => {
                    // Fallback to placeholder if image fails to load
                    const target = e.currentTarget;
                    target.style.display = 'none';
                    const parent = target.parentElement;
                    const placeholder = parent?.querySelector('.placeholder-icon') as HTMLElement;
                    if (placeholder) {
                      placeholder.classList.remove('hidden');
                    }
                  }}
                />
              )
            ) : null}
            <div className={`absolute inset-0 flex items-center justify-center ${clip.thumbnailPath ? 'hidden' : ''} placeholder-icon`}>
              <svg
                className="w-8 h-8 text-white/70"
                fill="currentColor"
                viewBox="0 0 24 24"
              >
                <path d="M8 5v14l11-7z" />
              </svg>
            </div>
          </div>

          {/* Content */}
          <div className="flex-1 min-w-0">
            <div className="flex items-start justify-between gap-4">
              <div className="flex-1 min-w-0">
                {/* Title */}
                <h3 className="text-lg font-semibold text-white mb-1 line-clamp-1">
                  {clip.title}
                </h3>

                {/* Description */}
                {clip.description && (
                  <p className="text-sm text-gray-400 mb-2 line-clamp-2">
                    {clip.description}
                  </p>
                )}

                {/* Metadata */}
                <div className="flex flex-wrap items-center gap-4 text-sm text-gray-400 mb-2">
                  <span>
                    {clip.storageType === 'YouTube' ? 'YouTube video' : 'Local file'}
                  </span>
                  <span>•</span>
                  <span>Duration: {formatDuration(clip.duration)}</span>
                  {clip.locationString && (
                    <>
                      <span>•</span>
                      <span className="truncate max-w-xs" title={clip.locationString}>
                        {clip.locationString}
                      </span>
                    </>
                  )}
                </div>

                {/* Tags */}
                {clip.tags.length > 0 && (
                  <div className="flex flex-wrap gap-2">
                    {clip.tags.map((tag) => (
                      <span
                        key={tag.id}
                        className="px-2 py-1 text-xs bg-[#007BFF] text-white rounded-full font-medium"
                      >
                        {tag.value}
                      </span>
                    ))}
                  </div>
                )}
              </div>
            </div>
          </div>
        </div>
      </div>
    </Link>
  );
}


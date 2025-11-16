'use client';

import { useState, useEffect, useCallback } from 'react';
import { useParams, useRouter } from 'next/navigation';
import Link from 'next/link';
import { Clip } from '@/types';
import { getClip, deleteClip, setFavorite } from '@/lib/api/clips';
import YouTubePlayer from '@/components/YouTubePlayer';
import LocalClipViewer from '@/components/LocalClipViewer';
import ClipForm from '@/components/ClipForm';

export default function ClipDetailPage() {
  const params = useParams();
  const router = useRouter();
  const clipId = parseInt(params.id as string);
  const [clip, setClip] = useState<Clip | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [isEditing, setIsEditing] = useState(false);
  const [isDeleting, setIsDeleting] = useState(false);
  const [favUpdating, setFavUpdating] = useState(false);

  const loadClip = useCallback(async () => {
    try {
      setLoading(true);
      const fetchedClip = await getClip(clipId);
      setClip(fetchedClip);
      setError(null);
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Failed to load clip');
      console.error('Error loading clip:', err);
    } finally {
      setLoading(false);
    }
  }, [clipId]);

  useEffect(() => {
    loadClip();
  }, [loadClip]);

  const handleToggleFavorite = async () => {
    if (!clip || favUpdating) return;
    const next = !clip.isFavorite;
    setClip({ ...clip, isFavorite: next });
    setFavUpdating(true);
    try {
      await setFavorite(clip.id, next);
    } catch {
      // revert on failure
      setClip({ ...clip, isFavorite: !next });
    } finally {
      setFavUpdating(false);
    }
  };

  const handleDelete = async () => {
    if (!confirm('Are you sure you want to delete this clip?')) {
      return;
    }

    try {
      setIsDeleting(true);
      await deleteClip(clipId);
      router.push('/');
      router.refresh();
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Failed to delete clip');
      setIsDeleting(false);
    }
  };


  if (loading) {
    return (
      <div className="min-h-screen bg-[#121212] flex items-center justify-center">
        <p className="text-[#007BFF]">Loading clip...</p>
      </div>
    );
  }

  if (error || !clip) {
    return (
      <div className="min-h-screen bg-[#121212]">
        <div className="container mx-auto px-4 py-8">
          {/* Breadcrumb Navigation */}
          <nav className="mb-6" aria-label="Breadcrumb">
            <ol className="flex items-center gap-2 text-sm" itemScope itemType="https://schema.org/BreadcrumbList">
              <li itemProp="itemListElement" itemScope itemType="https://schema.org/ListItem">
                <Link 
                  href="/" 
                  className="text-gray-400 hover:text-white transition-colors"
                  itemProp="item"
                >
                  <span itemProp="name">Home</span>
                </Link>
                <meta itemProp="position" content="1" />
              </li>
              <li className="text-gray-600" aria-hidden="true">/</li>
              <li itemProp="itemListElement" itemScope itemType="https://schema.org/ListItem">
                <Link 
                  href="/" 
                  className="text-gray-400 hover:text-white transition-colors"
                  itemProp="item"
                >
                  <span itemProp="name">Clips</span>
                </Link>
                <meta itemProp="position" content="2" />
              </li>
            </ol>
          </nav>
          <div className="bg-red-900/20 border border-red-700 text-red-300 px-4 py-3 rounded">
            {error || 'Clip not found'}
          </div>
        </div>
      </div>
    );
  }

  if (isEditing) {
    return (
      <div className="min-h-screen bg-[#121212]">
        <div className="container mx-auto px-4 py-8">
          {/* Breadcrumb Navigation */}
          <nav className="mb-6" aria-label="Breadcrumb">
            <ol className="flex items-center gap-2 text-sm" itemScope itemType="https://schema.org/BreadcrumbList">
              <li itemProp="itemListElement" itemScope itemType="https://schema.org/ListItem">
                <Link 
                  href="/" 
                  className="text-gray-400 hover:text-white transition-colors"
                  itemProp="item"
                >
                  <span itemProp="name">Home</span>
                </Link>
                <meta itemProp="position" content="1" />
              </li>
              <li className="text-gray-600" aria-hidden="true">/</li>
              <li itemProp="itemListElement" itemScope itemType="https://schema.org/ListItem">
                <Link 
                  href="/" 
                  className="text-gray-400 hover:text-white transition-colors"
                  itemProp="item"
                >
                  <span itemProp="name">Clips</span>
                </Link>
                <meta itemProp="position" content="2" />
              </li>
              <li className="text-gray-600" aria-hidden="true">/</li>
              <li itemProp="itemListElement" itemScope itemType="https://schema.org/ListItem">
                <Link 
                  href={`/clips/${clip.id}`}
                  className="text-gray-400 hover:text-white transition-colors"
                  itemProp="item"
                  onClick={(e) => {
                    e.preventDefault();
                    setIsEditing(false);
                    loadClip();
                  }}
                >
                  <span itemProp="name">{clip.title}</span>
                </Link>
                <meta itemProp="position" content="3" />
              </li>
              <li className="text-gray-600" aria-hidden="true">/</li>
              <li className="text-white font-medium" itemProp="itemListElement" itemScope itemType="https://schema.org/ListItem">
                <span itemProp="name">Edit</span>
                <meta itemProp="position" content="4" />
              </li>
            </ol>
          </nav>
          <h1 className="text-3xl font-bold text-white mb-6">Edit Clip</h1>
          <div className="bg-[#1e1e1e] rounded-lg shadow-lg border border-[#303030] p-6">
            <ClipForm
              clipId={clip.id}
              initialLocationString={clip.storageType === 'YouTube' 
                ? `https://www.youtube.com/watch?v=${clip.locationString}`
                : clip.locationString}
              initialTitle={clip.title}
              initialDescription={clip.description}
              initialTagIds={clip.tags.map((t) => t.id)}
              onCancel={() => {
                setIsEditing(false);
                loadClip();
              }}
            />
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-[#121212]">
      <div className="container mx-auto px-4 py-8">
        {/* Breadcrumb Navigation */}
        <nav className="mb-6" aria-label="Breadcrumb">
          <ol className="flex items-center gap-2 text-sm" itemScope itemType="https://schema.org/BreadcrumbList">
            <li itemProp="itemListElement" itemScope itemType="https://schema.org/ListItem">
              <Link 
                href="/" 
                className="text-gray-400 hover:text-white transition-colors"
                itemProp="item"
              >
                <span itemProp="name">Home</span>
              </Link>
              <meta itemProp="position" content="1" />
            </li>
            <li className="text-gray-600" aria-hidden="true">/</li>
            <li itemProp="itemListElement" itemScope itemType="https://schema.org/ListItem">
              <Link 
                href="/" 
                className="text-gray-400 hover:text-white transition-colors"
                itemProp="item"
              >
                <span itemProp="name">Clips</span>
              </Link>
              <meta itemProp="position" content="2" />
            </li>
            <li className="text-gray-600" aria-hidden="true">/</li>
            <li className="text-white font-medium truncate max-w-xs sm:max-w-md md:max-w-lg" itemProp="itemListElement" itemScope itemType="https://schema.org/ListItem">
              <span itemProp="name">{clip.title}</span>
              <meta itemProp="position" content="3" />
            </li>
          </ol>
        </nav>

        {/* Clip Content */}
        <div className="bg-[#1e1e1e] rounded-lg shadow-lg border border-[#303030] p-6">
          <div className="flex flex-col sm:flex-row sm:justify-between sm:items-start gap-4 mb-6">
            <div className="flex-1 min-w-0">
              <h1 className="text-3xl font-bold text-white mb-2 break-words">{clip.title}</h1>
              <div className="flex items-center gap-4 text-sm text-gray-400 flex-wrap">
                <span>Duration: {clip.duration === 0 ? 'N/A' : `${Math.floor(clip.duration / 60)}:${(clip.duration % 60).toString().padStart(2, '0')}`}</span>
                <span className={`px-2 py-1 rounded font-medium ${
                  clip.storageType === 'YouTube'
                    ? 'bg-red-900/50 text-red-300 border border-red-700'
                    : 'bg-blue-900/50 text-blue-300 border border-blue-700'
                }`}>
                  {clip.storageType}
                </span>
                <button
                  onClick={handleToggleFavorite}
                  disabled={favUpdating}
                  title={clip.isFavorite ? 'Remove from favourites' : 'Add to favourites'}
                  className="inline-flex items-center gap-1 px-2 py-1 rounded bg-black/30 hover:bg-black/50 disabled:opacity-50 text-white border border-white/10"
                >
                  {clip.isFavorite ? (
                    <>
                      <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="#ef4444" className="w-4 h-4">
                        <path d="M11.645 20.91l-.007-.003-.022-.012a15.247 15.247 0 01-.383-.218 25.18 25.18 0 01-4.244-3.17C4.688 15.188 3 12.97 3 10.5 3 8.015 4.994 6 7.5 6A5.5 5.5 0 0112 8.243 5.5 5.5 0 0116.5 6C19.006 6 21 8.015 21 10.5c0 2.47-1.688 4.688-3.989 6.997a25.175 25.175 0 01-4.244 3.17 15.247 15.247 0 01-.383.218l-.022.012-.007.003-.003.001a.75.75 0 01-.676 0l-.003-.001z" />
                      </svg>
                      <span className="text-sm">Favourited</span>
                    </>
                  ) : (
                    <>
                      <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="#ffffff" strokeWidth="1.8" className="w-4 h-4">
                        <path d="M12 21s-7-4.5-9-8.5S5 3 8 6c2 2 4 2 4 2s2 0 4-2c3-3 7 0 5 6S12 21 12 21z" />
                      </svg>
                      <span className="text-sm">Add to favourites</span>
                    </>
                  )}
                </button>
              </div>
              {clip.description && clip.description.trim().length > 0 && (
                <p className="mt-3 text-gray-300 leading-relaxed whitespace-pre-line">
                  {clip.description}
                </p>
              )}
            </div>
            <div className="flex gap-2 flex-shrink-0">
              <button
                onClick={() => setIsEditing(true)}
                className="px-4 py-2 bg-[#007BFF] text-white rounded-lg hover:bg-[#0056b3] transition-colors font-medium"
              >
                Edit
              </button>
              <button
                onClick={handleDelete}
                disabled={isDeleting}
                className="px-4 py-2 bg-red-600 text-white rounded-lg hover:bg-red-700 disabled:opacity-50 transition-colors font-medium"
              >
                {isDeleting ? 'Deleting...' : 'Delete'}
              </button>
            </div>
          </div>

          <div className="mb-6">
            {clip.storageType === 'YouTube' ? (
              <YouTubePlayer videoId={clip.locationString} />
            ) : (
              <LocalClipViewer filePath={clip.locationString} clipId={clip.id} />
            )}
          </div>

          {clip.tags.length > 0 && (
            <div className="mb-6">
              <h2 className="text-lg font-semibold mb-2 text-white">Tags</h2>
              <div className="flex flex-wrap gap-2">
                {clip.tags.map((tag) => (
                  <span
                    key={tag.id}
                    className="px-3 py-1 bg-[#007BFF]/20 text-[#007BFF] rounded-full text-sm border border-[#007BFF]/30"
                  >
                    <span className="capitalize font-medium">
                      {tag.category.replace(/([A-Z])/g, ' $1').trim()}:
                    </span>{' '}
                    {tag.value}
                  </span>
                ))}
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}


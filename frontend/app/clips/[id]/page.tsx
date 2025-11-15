'use client';

import { useState, useEffect } from 'react';
import { useParams, useRouter } from 'next/navigation';
import Link from 'next/link';
import { Clip } from '@/types';
import { getClip, deleteClip } from '@/lib/api/clips';
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

  useEffect(() => {
    loadClip();
  }, [clipId]);

  const loadClip = async () => {
    try {
      setLoading(true);
      const fetchedClip = await getClip(clipId);
      setClip(fetchedClip);
      setError(null);
    } catch (err: any) {
      setError(err.message || 'Failed to load clip');
      console.error('Error loading clip:', err);
    } finally {
      setLoading(false);
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
    } catch (err: any) {
      setError(err.message || 'Failed to delete clip');
      setIsDeleting(false);
    }
  };

  const formatDuration = (seconds: number) => {
    if (seconds === 0) return 'N/A';
    const mins = Math.floor(seconds / 60);
    const secs = seconds % 60;
    return `${mins}:${secs.toString().padStart(2, '0')}`;
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
                <span>Duration: {formatDuration(clip.duration)}</span>
                <span className={`px-2 py-1 rounded font-medium ${
                  clip.storageType === 'YouTube'
                    ? 'bg-red-900/50 text-red-300 border border-red-700'
                    : 'bg-blue-900/50 text-blue-300 border border-blue-700'
                }`}>
                  {clip.storageType}
                </span>
              </div>
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


'use client';

import { useState, useEffect } from 'react';
import { useParams, useRouter } from 'next/navigation';
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
      <div className="min-h-screen bg-slate-50 flex items-center justify-center">
        <p className="text-blue-600">Loading clip...</p>
      </div>
    );
  }

  if (error || !clip) {
    return (
      <div className="min-h-screen bg-slate-50">
        <div className="container mx-auto px-4 py-8">
          <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded">
            {error || 'Clip not found'}
          </div>
        </div>
      </div>
    );
  }

  if (isEditing) {
    return (
      <div className="min-h-screen bg-slate-50">
        <div className="container mx-auto px-4 py-8">
          <h1 className="text-3xl font-bold text-slate-800 mb-6">Edit Clip</h1>
          <div className="bg-white rounded-lg shadow-md border border-blue-100 p-6">
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
    <div className="min-h-screen bg-slate-50">
      <div className="container mx-auto px-4 py-8">
        <div className="bg-white rounded-lg shadow-md border border-blue-100 p-6">
          <div className="flex justify-between items-start mb-6">
            <div>
              <h1 className="text-3xl font-bold text-slate-800 mb-2">{clip.title}</h1>
              <div className="flex items-center gap-4 text-sm text-slate-600">
                <span>Duration: {formatDuration(clip.duration)}</span>
                <span className={`px-2 py-1 rounded font-medium ${
                  clip.storageType === 'YouTube'
                    ? 'bg-red-100 text-red-800'
                    : 'bg-blue-100 text-blue-700'
                }`}>
                  {clip.storageType}
                </span>
              </div>
            </div>
            <div className="flex gap-2">
              <button
                onClick={() => setIsEditing(true)}
                className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors font-medium shadow-sm hover:shadow-md"
              >
                Edit
              </button>
              <button
                onClick={handleDelete}
                disabled={isDeleting}
                className="px-4 py-2 bg-red-600 text-white rounded-lg hover:bg-red-700 disabled:opacity-50 transition-colors font-medium shadow-sm hover:shadow-md"
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
              <h2 className="text-lg font-semibold mb-2 text-slate-800">Tags</h2>
              <div className="flex flex-wrap gap-2">
                {clip.tags.map((tag) => (
                  <span
                    key={tag.id}
                    className="px-3 py-1 bg-blue-50 text-blue-700 rounded-full text-sm border border-blue-200"
                  >
                    <span className="text-blue-600 capitalize font-medium">
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


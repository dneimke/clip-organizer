'use client';

import { useState, useEffect, useCallback } from 'react';
import { useParams } from 'next/navigation';
import Link from 'next/link';
import { SessionPlan, Clip } from '@/types';
import { getSessionPlan, removeClipFromCollection, updateCollection } from '@/lib/api/session-plans';
import { getClips } from '@/lib/api/clips';
import ClipCard from '@/components/ClipCard';
import Toast from '@/components/Toast';

export default function PlanDetailPage() {
  const params = useParams();
  const planId = parseInt(params.id as string);
  const [plan, setPlan] = useState<SessionPlan | null>(null);
  const [clips, setClips] = useState<Clip[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [toast, setToast] = useState<{ message: string; type: 'success' | 'error' | 'info' } | null>(null);
  const [editingTitle, setEditingTitle] = useState(false);
  const [newTitle, setNewTitle] = useState('');
  const [renaming, setRenaming] = useState(false);

  const loadPlan = useCallback(async () => {
    try {
      setLoading(true);
      const fetchedPlan = await getSessionPlan(planId);
      setPlan(fetchedPlan);

      // Load clip details
      if (fetchedPlan.clipIds.length > 0) {
        const allClips = await getClips();
        const planClips = allClips.filter((c) => fetchedPlan.clipIds.includes(c.id));
        setClips(planClips);
      }
      setError(null);
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Failed to load collection');
      console.error('Error loading plan:', err);
    } finally {
      setLoading(false);
    }
  }, [planId]);

  useEffect(() => {
    loadPlan();
  }, [loadPlan]);

  useEffect(() => {
    if (plan) {
      setNewTitle(plan.title);
    }
  }, [plan]);

  const formatDate = (dateString: string) => {
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'long',
      day: 'numeric',
    });
  };

  const totalDuration = clips.reduce((sum, clip) => sum + clip.duration, 0);
  const totalDurationMinutes = Math.ceil(totalDuration / 60);

  if (loading) {
    return (
      <div className="min-h-screen bg-[#121212] flex items-center justify-center">
        <p className="text-[#007BFF]">Loading collection...</p>
      </div>
    );
  }

  if (error || !plan) {
    return (
      <div className="min-h-screen bg-[#121212]">
        <div className="container mx-auto px-4 sm:px-6 py-6 sm:py-8">
          <div className="mb-6">
            <Link
              href="/plans"
              className="text-[#007BFF] hover:text-[#0056b3] transition-colors inline-flex items-center gap-2"
            >
              <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 19l-7-7 7-7" />
              </svg>
              Back to Collections
            </Link>
          </div>
          <div className="p-4 bg-red-900/20 border border-red-700 rounded-lg text-red-300">
            {error || 'Collection not found'}
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-[#121212]">
      <div className="container mx-auto px-4 sm:px-6 py-6 sm:py-8">
        {/* Back Link */}
        <div className="mb-6">
          <Link
            href="/plans"
            className="text-[#007BFF] hover:text-[#0056b3] transition-colors inline-flex items-center gap-2"
          >
            <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 19l-7-7 7-7" />
            </svg>
            Back to Collections
          </Link>
        </div>

        {/* Plan Header */}
        <div className="mb-8">
          <div className="flex items-center gap-3 mb-3">
            {editingTitle ? (
              <div className="flex items-center gap-2">
                <input
                  value={newTitle}
                  onChange={(e) => setNewTitle(e.target.value)}
                  className="px-3 py-1.5 bg-[#202020] border border-[#303030] rounded text-white focus:outline-none focus:ring-2 focus:ring-[#007BFF]"
                />
                <button
                  onClick={async () => {
                    if (!plan) return;
                    if (!newTitle.trim()) {
                      setToast({ message: 'Title cannot be empty', type: 'error' });
                      return;
                    }
                    setRenaming(true);
                    try {
                      const updated = await updateCollection(plan.id, { title: newTitle.trim() });
                      setPlan(updated);
                      setEditingTitle(false);
                      setToast({ message: 'Collection renamed', type: 'success' });
                    } catch (err: unknown) {
                      setToast({
                        message: err instanceof Error ? err.message : 'Failed to rename collection',
                        type: 'error',
                      });
                    } finally {
                      setRenaming(false);
                    }
                  }}
                  disabled={renaming}
                  className="px-3 py-1.5 bg-[#007BFF] text-white rounded hover:bg-[#0056b3] disabled:opacity-50 text-sm"
                >
                  {renaming ? 'Saving...' : 'Save'}
                </button>
                <button
                  onClick={() => {
                    setEditingTitle(false);
                    setNewTitle(plan.title);
                  }}
                  disabled={renaming}
                  className="px-3 py-1.5 bg-[#303030] text-white rounded hover:bg-[#404040] text-sm"
                >
                  Cancel
                </button>
              </div>
            ) : (
              <>
                <h1 className="text-3xl font-bold text-white">{plan.title}</h1>
                <button
                  onClick={() => setEditingTitle(true)}
                  className="p-2 text-gray-300 hover:text-white transition-colors"
                  title="Rename collection"
                  aria-label="rename collection"
                >
                  <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5M18.5 2.5a2.121 2.121 0 113 3L12 15l-4 1 1-4 9.5-9.5z" />
                  </svg>
                </button>
              </>
            )}
          </div>
          <p className="text-gray-300 text-lg mb-4">{plan.summary}</p>
          <div className="flex flex-wrap gap-4 text-sm text-gray-400">
            <span>Created: {formatDate(plan.createdDate)}</span>
            <span>•</span>
            <span>{clips.length} clip{clips.length !== 1 ? 's' : ''}</span>
            <span>•</span>
            <span>Total Duration: {totalDurationMinutes} minutes</span>
          </div>
        </div>

        {/* Clips Grid */}
        {clips.length === 0 ? (
          <div className="text-center py-12">
            <p className="text-gray-400 text-lg">No clips in this collection</p>
          </div>
        ) : (
          <div>
            <h2 className="text-2xl font-semibold text-white mb-4">Clips</h2>
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
              {clips.map((clip) => (
                <div key={clip.id} className="relative group">
                  <ClipCard clip={clip} />
                  <button
                    onClick={async (e) => {
                      e.preventDefault();
                      e.stopPropagation();
                      try {
                        await removeClipFromCollection(planId, clip.id);
                        setClips((prev) => prev.filter((c) => c.id !== clip.id));
                        setToast({ message: 'Removed from collection', type: 'success' });
                      } catch (err: unknown) {
                        setToast({
                          message: err instanceof Error ? err.message : 'Failed to remove from collection',
                          type: 'error',
                        });
                      }
                    }}
                    title="Remove from collection"
                    aria-label="remove from collection"
                    className="hidden group-hover:flex absolute top-2 right-2 z-20 items-center gap-1 px-2 py-1 rounded bg-red-600/90 hover:bg-red-600 text-white text-xs"
                  >
                    <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                    </svg>
                    Remove
                  </button>
                </div>
              ))}
            </div>
          </div>
        )}
      </div>

      {/* Toast */}
      {toast && (
        <Toast
          message={toast.message}
          type={toast.type}
          onClose={() => setToast(null)}
        />
      )}
    </div>
  );
}


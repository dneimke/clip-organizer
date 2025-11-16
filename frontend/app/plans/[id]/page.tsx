'use client';

import { useState, useEffect, useCallback } from 'react';
import { useParams } from 'next/navigation';
import Link from 'next/link';
import { SessionPlan, Clip } from '@/types';
import { getSessionPlan } from '@/lib/api/session-plans';
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
      setError(err instanceof Error ? err.message : 'Failed to load session plan');
      console.error('Error loading plan:', err);
    } finally {
      setLoading(false);
    }
  }, [planId]);

  useEffect(() => {
    loadPlan();
  }, [loadPlan]);

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
        <p className="text-[#007BFF]">Loading session plan...</p>
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
              Back to Plans
            </Link>
          </div>
          <div className="p-4 bg-red-900/20 border border-red-700 rounded-lg text-red-300">
            {error || 'Session plan not found'}
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
            Back to Plans
          </Link>
        </div>

        {/* Plan Header */}
        <div className="mb-8">
          <h1 className="text-3xl font-bold text-white mb-3">{plan.title}</h1>
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
            <p className="text-gray-400 text-lg">No clips in this plan</p>
          </div>
        ) : (
          <div>
            <h2 className="text-2xl font-semibold text-white mb-4">Clips</h2>
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
              {clips.map((clip) => (
                <ClipCard key={clip.id} clip={clip} />
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


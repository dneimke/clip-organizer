'use client';

import { useState, useEffect } from 'react';
import Link from 'next/link';
import { SessionPlan } from '@/types';
import { getSessionPlans, deleteSessionPlan } from '@/lib/api/session-plans';
import Toast from '@/components/Toast';

export default function PlansPage() {
  const [plans, setPlans] = useState<SessionPlan[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [toast, setToast] = useState<{ message: string; type: 'success' | 'error' | 'info' } | null>(null);
  const [deletingId, setDeletingId] = useState<number | null>(null);

  useEffect(() => {
    loadPlans();
  }, []);

  const loadPlans = async () => {
    try {
      setLoading(true);
      const fetchedPlans = await getSessionPlans();
      setPlans(fetchedPlans);
      setError(null);
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Failed to load session plans');
      console.error('Error loading plans:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleDelete = async (id: number) => {
    if (!confirm('Are you sure you want to delete this session plan?')) {
      return;
    }

    try {
      setDeletingId(id);
      await deleteSessionPlan(id);
      setPlans((prev) => prev.filter((p) => p.id !== id));
      setToast({
        message: 'Session plan deleted successfully',
        type: 'success',
      });
    } catch (err: unknown) {
      setToast({
        message: err instanceof Error ? err.message : 'Failed to delete session plan',
        type: 'error',
      });
    } finally {
      setDeletingId(null);
    }
  };

  const formatDate = (dateString: string) => {
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
    });
  };

  if (loading) {
    return (
      <div className="min-h-screen bg-[#121212] flex items-center justify-center">
        <p className="text-[#007BFF]">Loading session plans...</p>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-[#121212]">
      <div className="container mx-auto px-4 sm:px-6 py-6 sm:py-8">
        {/* Header */}
        <div className="mb-6 flex items-center justify-between">
          <h1 className="text-3xl font-bold text-white">Session Plans</h1>
        </div>

        {error && (
          <div className="mb-6 p-4 bg-red-900/20 border border-red-700 rounded-lg text-red-300">
            {error}
          </div>
        )}

        {/* Plans List */}
        {plans.length === 0 ? (
          <div className="text-center py-12">
            <p className="text-gray-400 text-lg mb-4">No session plans yet</p>
            <p className="text-gray-500 text-sm">
              Create your first session plan using the &quot;Plan a Session&quot; button in the header
            </p>
          </div>
        ) : (
          <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
            {plans.map((plan) => (
              <div
                key={plan.id}
                className="bg-[#202020] border border-[#303030] rounded-lg p-6 hover:bg-[#252525] transition-colors flex flex-col"
              >
                {/* Header */}
                <div className="flex items-start justify-between mb-3">
                  <Link href={`/plans/${plan.id}`} className="flex-1">
                    <h3 className="text-xl font-semibold text-white mb-2 hover:text-[#007BFF] transition-colors">
                      {plan.title}
                    </h3>
                  </Link>
                  <button
                    onClick={() => handleDelete(plan.id)}
                    disabled={deletingId === plan.id}
                    className="ml-2 p-1 text-gray-400 hover:text-red-400 transition-colors disabled:opacity-50"
                    aria-label="Delete plan"
                  >
                    <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                    </svg>
                  </button>
                </div>

                {/* Summary */}
                <p className="text-gray-300 text-sm mb-4 line-clamp-3 flex-1">{plan.summary}</p>

                {/* Footer */}
                <div className="flex items-center justify-between pt-4 border-t border-[#303030]">
                  <div className="text-xs text-gray-400">
                    {formatDate(plan.createdDate)}
                  </div>
                  <div className="text-xs text-gray-400">
                    {plan.clipIds.length} clip{plan.clipIds.length !== 1 ? 's' : ''}
                  </div>
                </div>

                {/* View Link */}
                <Link
                  href={`/plans/${plan.id}`}
                  className="mt-4 px-4 py-2 bg-[#007BFF] text-white rounded-lg hover:bg-[#0056b3] transition-colors text-center font-medium"
                >
                  View Plan
                </Link>
              </div>
            ))}
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


'use client';

import { useState, useEffect, useRef } from 'react';
import { SessionPlan, Tag, Clip } from '@/types';
import { generateSessionPlan, createSessionPlan } from '@/lib/api/session-plans';
import { getTags } from '@/lib/api/tags';
import { getClips } from '@/lib/api/clips';
import Toast from './Toast';
import YouTubePlayer from './YouTubePlayer';

interface SessionPlanModalProps {
  isOpen: boolean;
  onClose: () => void;
  onPlanSaved?: () => void;
}

export default function SessionPlanModal({ isOpen, onClose, onPlanSaved }: SessionPlanModalProps) {
  const [durationMinutes, setDurationMinutes] = useState(60);
  const [selectedFocusAreas, setSelectedFocusAreas] = useState<string[]>([]);
  const [tagsByCategory, setTagsByCategory] = useState<Record<string, Tag[]>>({});
  const [isGenerating, setIsGenerating] = useState(false);
  const [generatedPlan, setGeneratedPlan] = useState<SessionPlan | null>(null);
  const [planClips, setPlanClips] = useState<Clip[]>([]);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [toast, setToast] = useState<{ message: string; type: 'success' | 'error' | 'info' } | null>(null);
  const [previewingClipId, setPreviewingClipId] = useState<number | null>(null);
  const modalRef = useRef<HTMLDivElement>(null);
  const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5059';

  useEffect(() => {
    if (isOpen) {
      loadTags();
      // Reset state when modal opens
      setGeneratedPlan(null);
      setPlanClips([]);
      setSelectedFocusAreas([]);
      setDurationMinutes(60);
      setError(null);
      setPreviewingClipId(null);
    }
  }, [isOpen]);

  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (modalRef.current && !modalRef.current.contains(event.target as Node)) {
        onClose();
      }
    };

    if (isOpen) {
      document.addEventListener('mousedown', handleClickOutside);
      document.body.style.overflow = 'hidden';
    }

    return () => {
      document.removeEventListener('mousedown', handleClickOutside);
      document.body.style.overflow = 'unset';
    };
  }, [isOpen, onClose]);

  const loadTags = async () => {
    try {
      const tags = await getTags();
      setTagsByCategory(tags);
    } catch (err) {
      console.error('Failed to load tags:', err);
    }
  };

  const handleFocusAreaToggle = (tagValue: string) => {
    setSelectedFocusAreas((prev) =>
      prev.includes(tagValue)
        ? prev.filter((v) => v !== tagValue)
        : [...prev, tagValue]
    );
  };

  const handleGenerate = async () => {
    if (durationMinutes <= 0) {
      setError('Duration must be greater than 0');
      return;
    }

    setIsGenerating(true);
    setError(null);
    setGeneratedPlan(null);
    setPlanClips([]);

    try {
      const plan = await generateSessionPlan(durationMinutes, selectedFocusAreas);
      setGeneratedPlan(plan);

      // Load full clip details
      if (plan.clipIds.length > 0) {
        const clips = await getClips();
        const matchingClips = clips.filter((c) => plan.clipIds.includes(c.id));
        setPlanClips(matchingClips);
      }
    } catch (err: any) {
      setError(err.message || 'Failed to generate session plan');
      setToast({
        message: err.message || 'Failed to generate session plan',
        type: 'error',
      });
    } finally {
      setIsGenerating(false);
    }
  };

  const handleRemoveClip = (clipId: number) => {
    if (!generatedPlan) return;

    const updatedClipIds = generatedPlan.clipIds.filter((id) => id !== clipId);
    setGeneratedPlan({
      ...generatedPlan,
      clipIds: updatedClipIds,
    });
    setPlanClips((prev) => prev.filter((c) => c.id !== clipId));
  };

  const handleSave = async () => {
    if (!generatedPlan || generatedPlan.clipIds.length === 0) {
      setError('Cannot save plan with no clips');
      return;
    }

    setIsSaving(true);
    setError(null);

    try {
      await createSessionPlan(generatedPlan.title, generatedPlan.summary, generatedPlan.clipIds);
      setToast({
        message: 'Session plan saved successfully!',
        type: 'success',
      });
      onPlanSaved?.();
      setTimeout(() => {
        onClose();
      }, 1000);
    } catch (err: any) {
      setError(err.message || 'Failed to save session plan');
      setToast({
        message: err.message || 'Failed to save session plan',
        type: 'error',
      });
    } finally {
      setIsSaving(false);
    }
  };

  const formatDuration = (seconds: number) => {
    if (seconds === 0) return 'N/A';
    const mins = Math.floor(seconds / 60);
    const secs = seconds % 60;
    return `${mins}:${secs.toString().padStart(2, '0')}`;
  };

  const totalDuration = planClips.reduce((sum, clip) => sum + clip.duration, 0);
  const totalDurationMinutes = Math.ceil(totalDuration / 60);

  const extractYouTubeVideoId = (url: string): string | null => {
    const patterns = [
      /(?:youtube\.com\/watch\?v=|youtu\.be\/|youtube\.com\/embed\/)([^&\n?#]+)/,
      /youtube\.com\/watch\?.*v=([^&\n?#]+)/
    ];
    
    for (const pattern of patterns) {
      const match = url.match(pattern);
      if (match && match[1]) {
        return match[1];
      }
    }
    return null;
  };

  const togglePreview = (clipId: number) => {
    setPreviewingClipId(previewingClipId === clipId ? null : clipId);
  };

  if (!isOpen) return null;

  return (
    <>
      <div className="fixed inset-0 bg-black/70 z-50 flex items-center justify-center p-4">
        <div
          ref={modalRef}
          className="bg-[#121212] border border-[#303030] rounded-lg shadow-xl w-full max-w-4xl max-h-[90vh] overflow-y-auto"
        >
          <div className="p-6">
            {/* Header */}
            <div className="flex items-center justify-between mb-6">
              <h2 className="text-2xl font-bold text-white">Plan a Session</h2>
              <button
                onClick={onClose}
                className="text-gray-400 hover:text-white transition-colors"
                aria-label="Close"
              >
                <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                </svg>
              </button>
            </div>

            {!generatedPlan ? (
              /* Form */
              <div className="space-y-6">
                {/* Duration Input */}
                <div>
                  <label htmlFor="duration" className="block text-sm font-medium text-gray-300 mb-2">
                    Session Duration (minutes)
                  </label>
                  <input
                    id="duration"
                    type="number"
                    min="1"
                    value={durationMinutes}
                    onChange={(e) => setDurationMinutes(parseInt(e.target.value) || 0)}
                    className="w-full px-4 py-2 bg-[#202020] border border-[#303030] rounded-lg text-white focus:outline-none focus:ring-2 focus:ring-[#007BFF]"
                  />
                </div>

                {/* Focus Areas */}
                <div>
                  <label className="block text-sm font-medium text-gray-300 mb-3">
                    Focus Areas (select tags to focus on)
                  </label>
                  <div className="max-h-64 overflow-y-auto border border-[#303030] rounded-lg p-4 bg-[#202020]">
                    {Object.entries(tagsByCategory).map(([category, tags]) => (
                      <div key={category} className="mb-4 last:mb-0">
                        <h3 className="text-sm font-medium text-gray-300 mb-2 capitalize">
                          {category.replace(/([A-Z])/g, ' $1').trim()}
                        </h3>
                        <div className="flex flex-wrap gap-2">
                          {tags.map((tag) => (
                            <button
                              key={tag.id}
                              onClick={() => handleFocusAreaToggle(tag.value)}
                              className={`px-3 py-1.5 rounded-full text-sm font-medium transition-colors ${
                                selectedFocusAreas.includes(tag.value)
                                  ? 'bg-[#007BFF] text-white'
                                  : 'bg-[#303030] text-gray-300 hover:bg-[#404040]'
                              }`}
                            >
                              {tag.value}
                            </button>
                          ))}
                        </div>
                      </div>
                    ))}
                  </div>
                </div>

                {error && (
                  <div className="p-3 bg-red-900/20 border border-red-700 rounded-lg text-red-300 text-sm">
                    {error}
                  </div>
                )}

                {/* Generate Button */}
                <div className="flex justify-end gap-3">
                  <button
                    onClick={onClose}
                    className="px-4 py-2 bg-[#303030] text-white rounded-lg hover:bg-[#404040] transition-colors"
                  >
                    Cancel
                  </button>
                  <button
                    onClick={handleGenerate}
                    disabled={isGenerating || durationMinutes <= 0}
                    className="px-4 py-2 bg-[#007BFF] text-white rounded-lg hover:bg-[#0056b3] disabled:opacity-50 disabled:cursor-not-allowed transition-colors font-medium flex items-center gap-2"
                  >
                    {isGenerating ? (
                      <>
                        <svg className="animate-spin h-5 w-5" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                          <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                          <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                        </svg>
                        Generating...
                      </>
                    ) : (
                      'Generate Plan'
                    )}
                  </button>
                </div>
              </div>
            ) : (
              /* Results */
              <div className="space-y-6">
                {/* Plan Title and Summary */}
                <div>
                  <h3 className="text-xl font-bold text-white mb-2">{generatedPlan.title}</h3>
                  <p className="text-gray-300">{generatedPlan.summary}</p>
                  <div className="mt-3 text-sm text-gray-400">
                    Total Duration: {totalDurationMinutes} minutes ({planClips.length} clips)
                  </div>
                </div>

                {/* Clips List */}
                <div>
                  <h4 className="text-lg font-semibold text-white mb-3">Selected Clips</h4>
                  <div className="space-y-2 max-h-96 overflow-y-auto">
                    {planClips.map((clip) => {
                      const isPreviewing = previewingClipId === clip.id;
                      const youtubeVideoId = clip.storageType === 'YouTube' 
                        ? extractYouTubeVideoId(clip.locationString)
                        : null;
                      
                      return (
                        <div
                          key={clip.id}
                          className="bg-[#202020] border border-[#303030] rounded-lg overflow-hidden"
                        >
                          <div className="flex items-center justify-between p-3 hover:bg-[#252525] transition-colors">
                            <div className="flex-1">
                              <div className="font-medium text-white">{clip.title}</div>
                              <div className="text-sm text-gray-400">
                                {formatDuration(clip.duration)} • {clip.tags.length} tags
                              </div>
                            </div>
                            <div className="flex items-center gap-2 ml-4">
                              <button
                                onClick={() => togglePreview(clip.id)}
                                className="p-2 text-[#007BFF] hover:text-[#0056b3] transition-colors"
                                aria-label={isPreviewing ? "Hide preview" : "Preview clip"}
                                title={isPreviewing ? "Hide preview" : "Preview clip"}
                              >
                                {isPreviewing ? (
                                  <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 15l7-7 7 7" />
                                  </svg>
                                ) : (
                                  <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M14.752 11.168l-3.197-2.132A1 1 0 0010 9.87v4.263a1 1 0 001.555.832l3.197-2.132a1 1 0 000-1.664z" />
                                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                                  </svg>
                                )}
                              </button>
                              <button
                                onClick={() => handleRemoveClip(clip.id)}
                                className="p-2 text-red-400 hover:text-red-300 transition-colors"
                                aria-label="Remove clip"
                                title="Remove clip"
                              >
                                <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                                </svg>
                              </button>
                            </div>
                          </div>
                          
                          {/* Preview Section */}
                          {isPreviewing && (
                            <div className="border-t border-[#303030] p-4 bg-[#1a1a1a]">
                              {clip.storageType === 'YouTube' && youtubeVideoId ? (
                                <YouTubePlayer videoId={youtubeVideoId} />
                              ) : clip.storageType === 'Local' ? (
                                <div className="w-full aspect-video bg-black rounded-lg overflow-hidden">
                                  <video
                                    src={`${API_BASE_URL}/api/clips/${clip.id}/video`}
                                    controls
                                    className="w-full h-full"
                                    onError={(e) => {
                                      console.error('Video playback error:', e);
                                    }}
                                  >
                                    Your browser does not support the video tag.
                                  </video>
                                </div>
                              ) : (
                                <div className="text-gray-400 text-sm">
                                  Preview not available for this clip type.
                                </div>
                              )}
                            </div>
                          )}
                        </div>
                      );
                    })}
                  </div>
                </div>

                {error && (
                  <div className="p-3 bg-red-900/20 border border-red-700 rounded-lg text-red-300 text-sm">
                    {error}
                  </div>
                )}

                {/* Actions */}
                <div className="flex justify-end gap-3">
                  <button
                    onClick={() => {
                      setGeneratedPlan(null);
                      setPlanClips([]);
                    }}
                    className="px-4 py-2 bg-[#303030] text-white rounded-lg hover:bg-[#404040] transition-colors"
                  >
                    Start Over
                  </button>
                  <button
                    onClick={onClose}
                    className="px-4 py-2 bg-[#303030] text-white rounded-lg hover:bg-[#404040] transition-colors"
                  >
                    Cancel
                  </button>
                  <button
                    onClick={handleSave}
                    disabled={isSaving || planClips.length === 0}
                    className="px-4 py-2 bg-[#007BFF] text-white rounded-lg hover:bg-[#0056b3] disabled:opacity-50 disabled:cursor-not-allowed transition-colors font-medium"
                  >
                    {isSaving ? 'Saving...' : 'Save Plan'}
                  </button>
                </div>
              </div>
            )}
          </div>
        </div>
      </div>

      {/* Toast */}
      {toast && (
        <Toast
          message={toast.message}
          type={toast.type}
          onClose={() => setToast(null)}
        />
      )}
    </>
  );
}


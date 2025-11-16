'use client';

import { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { UpdateClipDto, Tag, NewTag } from '@/types';
import { updateClip, generateClipMetadata } from '@/lib/api/clips';
import { getTags, getTagCategories } from '@/lib/api/tags';

interface ClipFormProps {
  clipId?: number;
  initialLocationString?: string;
  initialTitle?: string;
  initialDescription?: string;
  initialTagIds?: number[];
  onCancel?: () => void;
}

export default function ClipForm({ clipId, initialLocationString = '', initialTitle = '', initialDescription = '', initialTagIds = [], onCancel }: ClipFormProps) {
  const router = useRouter();
  const [notes, setNotes] = useState('');
  const [title, setTitle] = useState(initialTitle);
  const [description, setDescription] = useState(initialDescription);
  const [locationString, setLocationString] = useState(initialLocationString);
  const [selectedTagIds, setSelectedTagIds] = useState<number[]>(initialTagIds);
  const [newTags, setNewTags] = useState<NewTag[]>([]);
  const [aiSuggestedNewTags, setAiSuggestedNewTags] = useState<NewTag[]>([]);
  const [tagsByCategory, setTagsByCategory] = useState<Record<string, Tag[]>>({});
  const [categories, setCategories] = useState<string[]>([]);
  const [loading, setLoading] = useState(false);
  const [isGenerating, setIsGenerating] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [isLoadingTags, setIsLoadingTags] = useState(true);
  const [showNewTagForm, setShowNewTagForm] = useState(false);
  const [newTagCategory, setNewTagCategory] = useState<string>('');
  const [newTagValue, setNewTagValue] = useState<string>('');

  useEffect(() => {
    loadTags();
  }, []);

  const loadTags = async () => {
    try {
      setIsLoadingTags(true);
      const [tags, cats] = await Promise.all([getTags(), getTagCategories()]);
      setTagsByCategory(tags);
      setCategories(cats);
    } catch {
      setError('Failed to load tags');
    } finally {
      setIsLoadingTags(false);
    }
  };

  const handleTagToggle = (tagId: number) => {
    setSelectedTagIds((prev) =>
      prev.includes(tagId)
        ? prev.filter((id) => id !== tagId)
        : [...prev, tagId]
    );
  };

  const handleGenerateWithAI = async () => {
    if (!notes.trim()) {
      setError('Please enter notes about the clip first');
      return;
    }

    setIsGenerating(true);
    setError(null);

    try {
      const result = await generateClipMetadata({ notes });
      setTitle(result.title);
      setDescription(result.description);
      setSelectedTagIds(result.suggestedTagIds);
      setAiSuggestedNewTags(result.suggestedNewTags || []);
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Failed to generate metadata with AI');
    } finally {
      setIsGenerating(false);
    }
  };

  const handleAcceptAiSuggestedTag = (tag: NewTag, index: number) => {
    setNewTags((prev) => [...prev, tag]);
    setAiSuggestedNewTags((prev) => prev.filter((_, i) => i !== index));
  };

  const handleRejectAiSuggestedTag = (index: number) => {
    setAiSuggestedNewTags((prev) => prev.filter((_, i) => i !== index));
  };

  const handleAddManualTag = () => {
    if (newTagCategory && newTagValue.trim()) {
      setNewTags((prev) => [...prev, { category: newTagCategory, value: newTagValue.trim() }]);
      setNewTagCategory('');
      setNewTagValue('');
      setShowNewTagForm(false);
    }
  };

  const handleRemoveNewTag = (index: number) => {
    setNewTags((prev) => prev.filter((_, i) => i !== index));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError(null);

    try {
      if (!clipId) {
        throw new Error('Clip ID is required for editing');
      }

      const dto: UpdateClipDto = {
        locationString,
        title: title || undefined,
        description: description || undefined,
        notes: notes || undefined,
        tagIds: selectedTagIds,
        newTags: newTags.length > 0 ? newTags : undefined,
      };

      await updateClip(clipId, dto);

      // Reload tags to show newly created tags
      await loadTags();
      router.push('/');
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Failed to save clip');
    } finally {
      setLoading(false);
    }
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      <div>
        <label htmlFor="notes" className="block text-sm font-medium text-white mb-2">
          Your Notes about the Clip
        </label>
        <textarea
          id="notes"
          value={notes}
          onChange={(e) => setNotes(e.target.value)}
          rows={4}
          className="w-full px-4 py-2 border border-[#303030] rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500 bg-[#2a2a2a] text-white placeholder:text-slate-400"
          placeholder="e.g., A drill focusing on quick passes in the attacking third, with an emphasis on off-ball movement..."
        />
      </div>

      <button
        type="button"
        onClick={handleGenerateWithAI}
        disabled={isGenerating || !notes.trim()}
        className="w-full px-6 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors font-medium shadow-sm hover:shadow-md"
      >
        {isGenerating ? 'Generating with AI...' : 'Generate with AI'}
      </button>

      <div>
        <label htmlFor="title" className="block text-sm font-medium text-white mb-2">
          Title
        </label>
        <input
          type="text"
          id="title"
          value={title}
          onChange={(e) => setTitle(e.target.value)}
          className="w-full px-4 py-2 border border-[#303030] rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500 bg-[#2a2a2a] text-white placeholder:text-slate-400"
          placeholder="AI will generate a title..."
        />
      </div>

      <div>
        <label htmlFor="description" className="block text-sm font-medium text-white mb-2">
          Description
        </label>
        <textarea
          id="description"
          value={description}
          onChange={(e) => setDescription(e.target.value)}
          rows={4}
          className="w-full px-4 py-2 border border-[#303030] rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500 bg-[#2a2a2a] text-white placeholder:text-slate-400"
          placeholder="AI will generate a description..."
        />
      </div>

      <div>
        <label htmlFor="locationString" className="block text-sm font-medium text-white mb-2">
          Location (File Path or YouTube URL)
        </label>
        <input
          type="text"
          id="locationString"
          value={locationString}
          onChange={(e) => setLocationString(e.target.value)}
          className="w-full px-4 py-2 border border-[#303030] rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500 bg-[#2a2a2a] text-white placeholder:text-slate-400"
          placeholder="C:\\Videos\\clip.mp4 or https://www.youtube.com/watch?v=..."
        />
      </div>

      <div>
        <label className="block text-sm font-medium text-white mb-2">
          Tags
        </label>
        {isLoadingTags ? (
          <p className="text-blue-600">Loading tags...</p>
        ) : Object.keys(tagsByCategory).length === 0 ? (
          <div className="border border-[#303030] rounded-lg p-4 bg-[#2a2a2a]">
            <p className="text-sm text-slate-400">
              No tags available. You can still create the clip without tags.
            </p>
          </div>
        ) : (
          <div className="space-y-4 max-h-96 overflow-y-auto border border-[#303030] rounded-lg p-4 bg-[#2a2a2a]">
            {Object.entries(tagsByCategory).map(([category, tags]) => (
              <div key={category}>
                <h3 className="text-sm font-medium text-white mb-2 capitalize">
                  {category.replace(/([A-Z])/g, ' $1').trim()}
                </h3>
                <div className="grid grid-cols-2 gap-2">
                  {tags.map((tag) => (
                    <label
                      key={tag.id}
                      className="flex items-center space-x-2 cursor-pointer hover:bg-[#3a3a3a] p-2 rounded transition-colors"
                    >
                      <input
                        type="checkbox"
                        checked={selectedTagIds.includes(tag.id)}
                        onChange={() => handleTagToggle(tag.id)}
                        className="w-4 h-4 text-blue-600 border-blue-300 rounded focus:ring-blue-500 focus:ring-2"
                      />
                      <span className="text-sm text-white">{tag.value}</span>
                    </label>
                  ))}
                </div>
              </div>
            ))}
          </div>
        )}

        {/* AI Suggested New Tags */}
        {aiSuggestedNewTags.length > 0 && (
          <div className="mt-4 border border-blue-500 rounded-lg p-4 bg-blue-900/20">
            <h3 className="text-sm font-medium text-blue-300 mb-2">AI Suggested New Tags</h3>
            <div className="flex flex-wrap gap-2">
              {aiSuggestedNewTags.map((tag, index) => (
                <div key={index} className="flex items-center gap-2 bg-blue-800/50 px-3 py-1 rounded">
                  <span className="text-sm text-blue-200">
                    {tag.category.replace(/([A-Z])/g, ' $1').trim()}: {tag.value}
                  </span>
                  <button
                    type="button"
                    onClick={() => handleAcceptAiSuggestedTag(tag, index)}
                    className="text-xs bg-blue-600 hover:bg-blue-700 text-white px-2 py-1 rounded"
                  >
                    Accept
                  </button>
                  <button
                    type="button"
                    onClick={() => handleRejectAiSuggestedTag(index)}
                    className="text-xs bg-red-600 hover:bg-red-700 text-white px-2 py-1 rounded"
                  >
                    Reject
                  </button>
                </div>
              ))}
            </div>
          </div>
        )}

        {/* New Tags to Create */}
        {newTags.length > 0 && (
          <div className="mt-4 border border-green-500 rounded-lg p-4 bg-green-900/20">
            <h3 className="text-sm font-medium text-green-300 mb-2">New Tags to Create</h3>
            <div className="flex flex-wrap gap-2">
              {newTags.map((tag, index) => (
                <div key={index} className="flex items-center gap-2 bg-green-800/50 px-3 py-1 rounded">
                  <span className="text-sm text-green-200">
                    {tag.category.replace(/([A-Z])/g, ' $1').trim()}: {tag.value}
                  </span>
                  <button
                    type="button"
                    onClick={() => handleRemoveNewTag(index)}
                    className="text-xs bg-red-600 hover:bg-red-700 text-white px-2 py-1 rounded"
                  >
                    Remove
                  </button>
                </div>
              ))}
            </div>
          </div>
        )}

        {/* Manual Tag Creation Form */}
        <div className="mt-4">
          {!showNewTagForm ? (
            <button
              type="button"
              onClick={() => setShowNewTagForm(true)}
              className="px-4 py-2 bg-green-600 text-white rounded-lg hover:bg-green-700 transition-colors text-sm"
            >
              + Add New Tag
            </button>
          ) : (
            <div className="border border-[#303030] rounded-lg p-4 bg-[#2a2a2a]">
              <div className="grid grid-cols-2 gap-4 mb-3">
                <div>
                  <label className="block text-sm font-medium text-white mb-1">Category</label>
                  <select
                    value={newTagCategory}
                    onChange={(e) => setNewTagCategory(e.target.value)}
                    className="w-full px-3 py-2 border border-[#303030] rounded bg-[#1a1a1a] text-white text-sm"
                  >
                    <option value="">Select category</option>
                    {categories.map((cat) => (
                      <option key={cat} value={cat}>
                        {cat.replace(/([A-Z])/g, ' $1').trim()}
                      </option>
                    ))}
                  </select>
                </div>
                <div>
                  <label className="block text-sm font-medium text-white mb-1">Value</label>
                  <input
                    type="text"
                    value={newTagValue}
                    onChange={(e) => setNewTagValue(e.target.value)}
                    onKeyDown={(e) => {
                      if (e.key === 'Enter') {
                        e.preventDefault();
                        handleAddManualTag();
                      }
                    }}
                    placeholder="Tag name"
                    className="w-full px-3 py-2 border border-[#303030] rounded bg-[#1a1a1a] text-white text-sm"
                  />
                </div>
              </div>
              <div className="flex gap-2">
                <button
                  type="button"
                  onClick={handleAddManualTag}
                  disabled={!newTagCategory || !newTagValue.trim()}
                  className="px-4 py-2 bg-green-600 text-white rounded hover:bg-green-700 disabled:opacity-50 disabled:cursor-not-allowed text-sm"
                >
                  Add Tag
                </button>
                <button
                  type="button"
                  onClick={() => {
                    setShowNewTagForm(false);
                    setNewTagCategory('');
                    setNewTagValue('');
                  }}
                  className="px-4 py-2 bg-slate-600 text-white rounded hover:bg-slate-700 text-sm"
                >
                  Cancel
                </button>
              </div>
            </div>
          )}
        </div>
      </div>

      {error && (
        <div className="bg-red-900/20 border border-red-700 text-red-300 px-4 py-3 rounded">
          {error}
        </div>
      )}

      <div className="flex space-x-4 justify-end">
        <button
          type="button"
          onClick={() => onCancel ? onCancel() : router.back()}
          className="px-6 py-2 bg-slate-200 text-slate-700 rounded-lg hover:bg-slate-300 transition-colors"
        >
          Cancel
        </button>
        <button
          type="submit"
          disabled={loading}
          className="px-6 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors font-medium shadow-sm hover:shadow-md"
        >
          {loading ? 'Saving...' : 'Update'} Clip
        </button>
      </div>
    </form>
  );
}


'use client';

import { useState, useEffect, useMemo } from 'react';
import { useRouter } from 'next/navigation';
import { Clip, Tag, BulkUpdateClip, NewTag } from '@/types';
import { getUnclassifiedClips, bulkUpdateClips } from '@/lib/api/clips';
import { getTags, getTagCategories } from '@/lib/api/tags';

interface ClipEditState {
  title: string;
  description: string;
  tagIds: number[];
  newTags: NewTag[];
}

export default function BulkClassifyPage() {
  const router = useRouter();
  const [clips, setClips] = useState<Clip[]>([]);
  const [tagsByCategory, setTagsByCategory] = useState<Record<string, Tag[]>>({});
  const [categories, setCategories] = useState<string[]>([]);
  const [selectedClipIds, setSelectedClipIds] = useState<Set<number>>(new Set());
  const [clipEdits, setClipEdits] = useState<Record<number, ClipEditState>>({});
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [searchTerm, setSearchTerm] = useState('');
  const [showNewTagForm, setShowNewTagForm] = useState(false);
  const [newTagCategory, setNewTagCategory] = useState<string>('');
  const [newTagValue, setNewTagValue] = useState<string>('');

  useEffect(() => {
    loadData();
  }, []);

  const loadData = async () => {
    try {
      setLoading(true);
      const [clipsData, tagsData, cats] = await Promise.all([
        getUnclassifiedClips(),
        getTags(),
        getTagCategories()
      ]);
      setClips(clipsData);
      setTagsByCategory(tagsData);
      setCategories(cats);
      
      // Initialize edit states
      const edits: Record<number, ClipEditState> = {};
      clipsData.forEach(clip => {
        edits[clip.id] = {
          title: clip.title,
          description: clip.description,
          tagIds: clip.tags.map(t => t.id),
          newTags: []
        };
      });
      setClipEdits(edits);
    } catch (err: any) {
      setError(err.message || 'Failed to load data');
    } finally {
      setLoading(false);
    }
  };

  // Compute filtered clips
  const filteredClips = useMemo(() => {
    return clips.filter(clip => {
      if (!searchTerm) return true;
      const term = searchTerm.toLowerCase();
      return clip.title.toLowerCase().includes(term) ||
             clip.description.toLowerCase().includes(term) ||
             clip.locationString.toLowerCase().includes(term);
    });
  }, [clips, searchTerm]);

  const handleSelectClip = (clipId: number) => {
    setSelectedClipIds(prev => {
      const newSet = new Set(prev);
      if (newSet.has(clipId)) {
        newSet.delete(clipId);
      } else {
        newSet.add(clipId);
      }
      return newSet;
    });
  };

  const handleSelectAll = () => {
    if (selectedClipIds.size === filteredClips.length) {
      setSelectedClipIds(new Set());
    } else {
      setSelectedClipIds(new Set(filteredClips.map(c => c.id)));
    }
  };

  const handleTitleChange = (clipId: number, title: string) => {
    setClipEdits(prev => ({
      ...prev,
      [clipId]: { ...prev[clipId], title }
    }));
  };

  const handleDescriptionChange = (clipId: number, description: string) => {
    setClipEdits(prev => ({
      ...prev,
      [clipId]: { ...prev[clipId], description }
    }));
  };

  const handleTagToggle = (clipId: number, tagId: number) => {
    setClipEdits(prev => {
      const currentTagIds = prev[clipId]?.tagIds || [];
      const newTagIds = currentTagIds.includes(tagId)
        ? currentTagIds.filter(id => id !== tagId)
        : [...currentTagIds, tagId];
      return {
        ...prev,
        [clipId]: { ...prev[clipId], tagIds: newTagIds }
      };
    });
  };

  const handleApplyTagsToSelected = (tagIds: number[]) => {
    setClipEdits(prev => {
      const newEdits = { ...prev };
      selectedClipIds.forEach(clipId => {
        if (newEdits[clipId]) {
          newEdits[clipId] = {
            ...newEdits[clipId],
            tagIds: [...new Set([...newEdits[clipId].tagIds, ...tagIds])]
          };
        }
      });
      return newEdits;
    });
  };

  const handleAddNewTagToSelected = (newTag: NewTag) => {
    setClipEdits(prev => {
      const newEdits = { ...prev };
      selectedClipIds.forEach(clipId => {
        if (newEdits[clipId]) {
          const existingNewTags = newEdits[clipId].newTags || [];
          // Check if tag already exists
          const tagExists = existingNewTags.some(
            t => t.category === newTag.category && t.value === newTag.value
          );
          if (!tagExists) {
            newEdits[clipId] = {
              ...newEdits[clipId],
              newTags: [...existingNewTags, newTag]
            };
          }
        }
      });
      return newEdits;
    });
  };

  const handleCreateNewTag = () => {
    if (newTagCategory && newTagValue.trim()) {
      const newTag: NewTag = { category: newTagCategory, value: newTagValue.trim() };
      if (selectedClipIds.size > 0) {
        handleAddNewTagToSelected(newTag);
      }
      setNewTagCategory('');
      setNewTagValue('');
      setShowNewTagForm(false);
    }
  };

  const handleRemoveNewTag = (clipId: number, index: number) => {
    setClipEdits(prev => {
      const edit = prev[clipId];
      if (!edit) return prev;
      return {
        ...prev,
        [clipId]: {
          ...edit,
          newTags: edit.newTags.filter((_, i) => i !== index)
        }
      };
    });
  };

  const handleSave = async () => {
    try {
      setSaving(true);
      setError(null);

      const updates: BulkUpdateClip[] = clips.map(clip => {
        const edit = clipEdits[clip.id];
        if (!edit) return null;

        const update: BulkUpdateClip = { clipId: clip.id };
        
        if (edit.title !== clip.title) {
          update.title = edit.title;
        }
        if (edit.description !== clip.description) {
          update.description = edit.description;
        }
        const currentTagIds = clip.tags.map(t => t.id).sort();
        const newTagIds = [...edit.tagIds].sort();
        if (JSON.stringify(currentTagIds) !== JSON.stringify(newTagIds)) {
          update.tagIds = edit.tagIds;
        }
        if (edit.newTags && edit.newTags.length > 0) {
          update.newTags = edit.newTags;
        }

        // Only include if there are actual changes
        return Object.keys(update).length > 1 ? update : null;
      }).filter((u): u is BulkUpdateClip => u !== null);

      if (updates.length === 0) {
        setError('No changes to save');
        return;
      }

      await bulkUpdateClips(updates);
      await loadData();
      setSelectedClipIds(new Set());
      setShowNewTagForm(false);
      
      // Show success message
      alert(`Successfully updated ${updates.length} clip(s)`);
    } catch (err: any) {
      setError(err.message || 'Failed to save changes');
    } finally {
      setSaving(false);
    }
  };

  if (loading) {
    return (
      <div className="min-h-screen bg-[#1a1a1a] flex items-center justify-center">
        <p className="text-blue-600">Loading unclassified clips...</p>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-[#1a1a1a]">
      <div className="container mx-auto px-4 py-8">
        <div className="flex justify-between items-center mb-6">
          <div>
            <h1 className="text-3xl font-bold text-white mb-2">Bulk Classify Clips</h1>
            <p className="text-gray-400">
              {filteredClips.length} unclassified clip{filteredClips.length !== 1 ? 's' : ''} found
            </p>
          </div>
          <div className="flex gap-2">
            <button
              onClick={() => router.push('/')}
              className="px-4 py-2 bg-slate-200 text-slate-700 rounded-lg hover:bg-slate-300 transition-colors"
            >
              Back to Clips
            </button>
            <button
              onClick={handleSave}
              disabled={saving}
              className="px-6 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50 transition-colors font-medium"
            >
              {saving ? 'Saving...' : 'Save All Changes'}
            </button>
          </div>
        </div>

        {error && (
          <div className="bg-red-900/20 border border-red-700 text-red-300 px-4 py-3 rounded mb-4">
            {error}
          </div>
        )}

        {/* Search */}
        <div className="mb-4">
          <input
            type="text"
            placeholder="Search clips..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            className="w-full px-4 py-2 border border-[#303030] rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 bg-[#2a2a2a] text-white"
          />
        </div>

        {/* Batch operations */}
        {selectedClipIds.size > 0 && (
          <div className="bg-blue-900/20 border border-blue-700 text-blue-300 px-4 py-3 rounded mb-4">
            <div className="space-y-3">
              <div className="flex items-center justify-between">
                <span>{selectedClipIds.size} clip(s) selected</span>
                <div className="flex gap-2">
                  {Object.entries(tagsByCategory).map(([category, tags]) => (
                    <div key={category} className="flex gap-1">
                      {tags.slice(0, 3).map(tag => (
                        <button
                          key={tag.id}
                          onClick={() => handleApplyTagsToSelected([tag.id])}
                          className="px-2 py-1 text-xs bg-blue-600 text-white rounded hover:bg-blue-700"
                        >
                          +{tag.value}
                        </button>
                      ))}
                    </div>
                  ))}
                </div>
              </div>
              <div className="flex items-center gap-2">
                {!showNewTagForm ? (
                  <button
                    type="button"
                    onClick={() => setShowNewTagForm(true)}
                    className="px-3 py-1 text-xs bg-green-600 text-white rounded hover:bg-green-700"
                  >
                    + Create New Tag for Selected
                  </button>
                ) : (
                  <div className="flex gap-2 items-center">
                    <select
                      value={newTagCategory}
                      onChange={(e) => setNewTagCategory(e.target.value)}
                      className="px-2 py-1 text-xs border border-blue-600 rounded bg-[#1a1a1a] text-white"
                    >
                      <option value="">Category</option>
                      {categories.map((cat) => (
                        <option key={cat} value={cat}>
                          {cat.replace(/([A-Z])/g, ' $1').trim()}
                        </option>
                      ))}
                    </select>
                    <input
                      type="text"
                      value={newTagValue}
                      onChange={(e) => setNewTagValue(e.target.value)}
                      onKeyDown={(e) => {
                        if (e.key === 'Enter') {
                          e.preventDefault();
                          handleCreateNewTag();
                        }
                      }}
                      placeholder="Tag name"
                      className="px-2 py-1 text-xs border border-blue-600 rounded bg-[#1a1a1a] text-white w-32"
                    />
                    <button
                      type="button"
                      onClick={handleCreateNewTag}
                      disabled={!newTagCategory || !newTagValue.trim()}
                      className="px-2 py-1 text-xs bg-green-600 text-white rounded hover:bg-green-700 disabled:opacity-50"
                    >
                      Add
                    </button>
                    <button
                      type="button"
                      onClick={() => {
                        setShowNewTagForm(false);
                        setNewTagCategory('');
                        setNewTagValue('');
                      }}
                      className="px-2 py-1 text-xs bg-slate-600 text-white rounded hover:bg-slate-700"
                    >
                      Cancel
                    </button>
                  </div>
                )}
              </div>
            </div>
          </div>
        )}

        {/* Clips table */}
        {filteredClips.length === 0 ? (
          <div className="text-center py-12">
            <p className="text-blue-600">No unclassified clips found.</p>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full border-collapse">
              <thead>
                <tr className="border-b border-[#303030]">
                  <th className="text-left p-3 text-white">
                    <input
                      type="checkbox"
                      checked={selectedClipIds.size === filteredClips.length && filteredClips.length > 0}
                      onChange={handleSelectAll}
                      className="w-4 h-4"
                    />
                  </th>
                  <th className="text-left p-3 text-white">Filename</th>
                  <th className="text-left p-3 text-white">Title</th>
                  <th className="text-left p-3 text-white">Description</th>
                  <th className="text-left p-3 text-white">Tags</th>
                </tr>
              </thead>
              <tbody>
                {filteredClips.map(clip => {
                  const edit = clipEdits[clip.id];
                  if (!edit) return null;

                  return (
                    <tr
                      key={clip.id}
                      className="border-b border-[#303030] hover:bg-[#252525]"
                    >
                      <td className="p-3">
                        <input
                          type="checkbox"
                          checked={selectedClipIds.has(clip.id)}
                          onChange={() => handleSelectClip(clip.id)}
                          className="w-4 h-4"
                        />
                      </td>
                      <td className="p-3 text-gray-300 text-sm font-mono">
                        {clip.locationString.split('\\').pop() || clip.locationString}
                      </td>
                      <td className="p-3">
                        <input
                          type="text"
                          value={edit.title}
                          onChange={(e) => handleTitleChange(clip.id, e.target.value)}
                          className="w-full px-2 py-1 border border-[#303030] rounded bg-[#2a2a2a] text-white text-sm"
                        />
                      </td>
                      <td className="p-3">
                        <textarea
                          value={edit.description}
                          onChange={(e) => handleDescriptionChange(clip.id, e.target.value)}
                          rows={2}
                          className="w-full px-2 py-1 border border-[#303030] rounded bg-[#2a2a2a] text-white text-sm resize-none"
                        />
                      </td>
                      <td className="p-3">
                        <div className="space-y-2">
                          <div className="flex flex-wrap gap-1 max-w-md">
                            {Object.entries(tagsByCategory).map(([category, tags]) => (
                              <div key={category} className="flex flex-wrap gap-1">
                                {tags.map(tag => (
                                  <button
                                    key={tag.id}
                                    onClick={() => handleTagToggle(clip.id, tag.id)}
                                    className={`px-2 py-1 text-xs rounded ${
                                      edit.tagIds.includes(tag.id)
                                        ? 'bg-blue-600 text-white'
                                        : 'bg-[#303030] text-gray-300 hover:bg-[#404040]'
                                    }`}
                                  >
                                    {tag.value}
                                  </button>
                                ))}
                              </div>
                            ))}
                          </div>
                          {edit.newTags && edit.newTags.length > 0 && (
                            <div className="flex flex-wrap gap-1">
                              {edit.newTags.map((newTag, idx) => (
                                <div key={idx} className="flex items-center gap-1 bg-green-800/50 px-2 py-1 rounded">
                                  <span className="text-xs text-green-200">
                                    {newTag.category.replace(/([A-Z])/g, ' $1').trim()}: {newTag.value}
                                  </span>
                                  <button
                                    type="button"
                                    onClick={() => handleRemoveNewTag(clip.id, idx)}
                                    className="text-xs bg-red-600 hover:bg-red-700 text-white px-1 rounded"
                                  >
                                    ×
                                  </button>
                                </div>
                              ))}
                            </div>
                          )}
                        </div>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
}


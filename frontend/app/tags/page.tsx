'use client';

import { useState, useEffect } from 'react';
import { Tag, CreateTagDto } from '@/types';
import { getTags, getTagCategories, createTag } from '@/lib/api/tags';

export default function TagsPage() {
  const [tagsByCategory, setTagsByCategory] = useState<Record<string, Tag[]>>({});
  const [categories, setCategories] = useState<string[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [isCreating, setIsCreating] = useState(false);
  const [newTag, setNewTag] = useState<CreateTagDto>({ category: '', value: '' });
  const [createError, setCreateError] = useState<string | null>(null);

  useEffect(() => {
    loadData();
  }, []);

  const loadData = async () => {
    try {
      setLoading(true);
      const [tags, cats] = await Promise.all([getTags(), getTagCategories()]);
      setTagsByCategory(tags);
      setCategories(cats);
      setError(null);
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Failed to load tags');
      console.error('Error loading tags:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleCreateTag = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!newTag.category || !newTag.value.trim()) {
      setCreateError('Category and value are required');
      return;
    }

    try {
      setIsCreating(true);
      setCreateError(null);
      await createTag({
        category: newTag.category,
        value: newTag.value.trim(),
      });
      setNewTag({ category: '', value: '' });
      await loadData(); // Reload tags
    } catch (err: unknown) {
      setCreateError(err instanceof Error ? err.message : 'Failed to create tag');
    } finally {
      setIsCreating(false);
    }
  };

  const formatCategoryName = (category: string) => {
    return category.replace(/([A-Z])/g, ' $1').trim();
  };

  if (loading) {
    return (
      <div className="min-h-screen bg-[#121212] flex items-center justify-center">
        <p className="text-[#007BFF]">Loading tags...</p>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-[#121212]">
      <div className="container mx-auto px-4 py-8">
        <h1 className="text-3xl font-bold text-white mb-6">Manage Tags</h1>

        {error && (
          <div className="bg-red-900/20 border border-red-700 text-red-300 px-4 py-3 rounded mb-6">
            {error}
          </div>
        )}

        {/* Create Tag Form */}
        <div className="bg-[#202020] rounded-lg shadow-md border border-[#303030] p-6 mb-6">
          <h2 className="text-xl font-semibold text-white mb-4">Create New Tag</h2>
          <form onSubmit={handleCreateTag} className="space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <div>
                <label htmlFor="category" className="block text-sm font-medium text-gray-300 mb-2">
                  Category
                </label>
                <select
                  id="category"
                  value={newTag.category}
                  onChange={(e) => setNewTag({ ...newTag, category: e.target.value })}
                  required
                  className="w-full px-4 py-2 border border-[#303030] rounded-lg focus:outline-none focus:ring-2 focus:ring-[#007BFF] focus:border-[#007BFF] bg-[#202020] text-white"
                >
                  <option value="">Select a category</option>
                  {categories.map((cat) => (
                    <option key={cat} value={cat}>
                      {formatCategoryName(cat)}
                    </option>
                  ))}
                </select>
              </div>
              <div>
                <label htmlFor="value" className="block text-sm font-medium text-gray-300 mb-2">
                  Tag Value
                </label>
                <input
                  type="text"
                  id="value"
                  value={newTag.value}
                  onChange={(e) => setNewTag({ ...newTag, value: e.target.value })}
                  required
                  placeholder="e.g., Flick, PC Attack, Defender"
                  className="w-full px-4 py-2 border border-[#303030] rounded-lg focus:outline-none focus:ring-2 focus:ring-[#007BFF] focus:border-[#007BFF] bg-[#202020] text-white placeholder:text-gray-500"
                />
              </div>
            </div>
            {createError && (
              <div className="bg-red-900/20 border border-red-700 text-red-300 px-4 py-3 rounded">
                {createError}
              </div>
            )}
            <button
              type="submit"
              disabled={isCreating}
              className="px-6 py-2 bg-[#007BFF] text-white rounded-lg hover:bg-[#0056b3] disabled:opacity-50 disabled:cursor-not-allowed transition-colors font-medium shadow-sm hover:shadow-md"
            >
              {isCreating ? 'Creating...' : 'Create Tag'}
            </button>
          </form>
        </div>

        {/* Tags List by Category */}
        <div className="bg-[#202020] rounded-lg shadow-md border border-[#303030] p-6">
          <h2 className="text-xl font-semibold text-white mb-4">Existing Tags</h2>
          {Object.keys(tagsByCategory).length === 0 ? (
            <p className="text-gray-400">No tags have been created yet. Create your first tag above!</p>
          ) : (
            <div className="space-y-6">
              {categories.map((category) => {
                const tags = tagsByCategory[category] || [];
                return (
                  <div key={category}>
                    <h3 className="text-lg font-medium text-gray-300 mb-3 capitalize">
                      {formatCategoryName(category)}
                    </h3>
                    {tags.length === 0 ? (
                      <p className="text-sm text-gray-500 italic">No tags in this category</p>
                    ) : (
                      <div className="flex flex-wrap gap-2">
                        {tags.map((tag) => (
                          <span
                            key={tag.id}
                            className="px-3 py-1 bg-[#007BFF]/20 text-[#007BFF] rounded-full text-sm border border-[#007BFF]/30"
                          >
                            {tag.value}
                          </span>
                        ))}
                      </div>
                    )}
                  </div>
                );
              })}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}


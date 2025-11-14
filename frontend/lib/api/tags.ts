import { Tag, CreateTagDto } from '@/types';

const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5059';

export async function getTags(): Promise<Record<string, Tag[]>> {
  const response = await fetch(`${API_BASE_URL}/api/tags`);
  if (!response.ok) {
    throw new Error('Failed to fetch tags');
  }
  return response.json();
}

export async function getTagCategories(): Promise<string[]> {
  const response = await fetch(`${API_BASE_URL}/api/tags/categories`);
  if (!response.ok) {
    throw new Error('Failed to fetch tag categories');
  }
  return response.json();
}

export async function createTag(dto: CreateTagDto): Promise<Tag> {
  const response = await fetch(`${API_BASE_URL}/api/tags`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(dto),
  });
  if (!response.ok) {
    const error = await response.text();
    throw new Error(error || 'Failed to create tag');
  }
  return response.json();
}


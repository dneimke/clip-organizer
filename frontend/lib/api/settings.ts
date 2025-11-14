import { Setting, RootFolderSetting } from '@/types';

const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5059';

export async function getRootFolder(): Promise<RootFolderSetting> {
  const response = await fetch(`${API_BASE_URL}/api/settings/root-folder`);
  if (!response.ok) {
    throw new Error('Failed to fetch root folder setting');
  }
  return response.json();
}

export async function setRootFolder(rootFolderPath: string): Promise<RootFolderSetting> {
  const response = await fetch(`${API_BASE_URL}/api/settings/root-folder`, {
    method: 'PUT',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({ rootFolderPath }),
  });
  if (!response.ok) {
    const error = await response.text();
    throw new Error(error || 'Failed to set root folder');
  }
  return response.json();
}

export async function getSettings(): Promise<Setting[]> {
  const response = await fetch(`${API_BASE_URL}/api/settings`);
  if (!response.ok) {
    throw new Error('Failed to fetch settings');
  }
  return response.json();
}


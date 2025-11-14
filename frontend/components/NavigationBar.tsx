'use client';

import { useState, useEffect, useCallback } from 'react';
import { useRouter, usePathname } from 'next/navigation';
import { getUnclassifiedClips } from '@/lib/api/clips';

export default function NavigationBar() {
  const router = useRouter();
  const pathname = usePathname();
  const [unclassifiedCount, setUnclassifiedCount] = useState(0);

  const loadUnclassifiedCount = useCallback(async () => {
    try {
      const unclassified = await getUnclassifiedClips();
      setUnclassifiedCount(unclassified.length);
    } catch (err) {
      console.error('Failed to load unclassified count:', err);
    }
  }, []);

  useEffect(() => {
    if (pathname !== '/') {
      loadUnclassifiedCount();
    }
  }, [pathname, loadUnclassifiedCount]);

  // Hide navigation bar on home page since we have our own header
  if (pathname === '/') {
    return null;
  }

  return (
    <nav className="bg-[#121212] border-b border-[#303030]">
      <div className="container mx-auto px-6 py-4">
        <div className="flex justify-between items-center">
          <h1 
            onClick={() => router.push('/')}
            className="text-3xl font-bold text-white cursor-pointer hover:text-[#007BFF] transition-colors"
          >
            Clip AI
          </h1>
          <div className="flex gap-2">
            {unclassifiedCount > 0 && (
              <button
                onClick={() => router.push('/clips/bulk-classify')}
                className="px-4 py-2 bg-yellow-600 text-white rounded-lg hover:bg-yellow-700 transition-colors font-medium flex items-center gap-2 relative"
              >
                Unclassified
                <span className="bg-red-500 text-white text-xs font-bold rounded-full px-2 py-0.5">
                  {unclassifiedCount}
                </span>
              </button>
            )}
            <button
              onClick={() => router.push('/clips/bulk-upload')}
              className="px-4 py-2 bg-green-600 text-white rounded-lg hover:bg-green-700 transition-colors font-medium"
            >
              Bulk Upload
            </button>
            <button
              onClick={() => router.push('/clips/sync')}
              className="px-4 py-2 bg-purple-600 text-white rounded-lg hover:bg-purple-700 transition-colors font-medium"
            >
              Sync Filesystem
            </button>
            <button
              onClick={() => router.push('/tags')}
              className="px-4 py-2 bg-[#202020] text-white rounded-lg hover:bg-[#303030] transition-colors font-medium border border-[#303030]"
            >
              Manage Tags
            </button>
            <button
              onClick={() => router.push('/settings')}
              className="px-4 py-2 bg-[#202020] text-white rounded-lg hover:bg-[#303030] transition-colors font-medium border border-[#303030]"
            >
              Settings
            </button>
            <button
              onClick={() => router.push('/clips/new')}
              className="px-4 py-2 bg-[#007BFF] text-white rounded-lg hover:bg-[#0056b3] transition-colors font-medium"
            >
              + New Clip
            </button>
          </div>
        </div>
      </div>
    </nav>
  );
}



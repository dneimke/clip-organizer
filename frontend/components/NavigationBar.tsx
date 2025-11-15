'use client';

import { useState, useEffect, useCallback, useRef } from 'react';
import { usePathname, useRouter } from 'next/navigation';
import { getUnclassifiedClips, syncClips } from '@/lib/api/clips';
import { getRootFolder } from '@/lib/api/settings';
import Link from 'next/link';
import Toast from './Toast';

export default function NavigationBar() {
  const pathname = usePathname();
  const router = useRouter();
  const [unclassifiedCount, setUnclassifiedCount] = useState(0);
  const [menuOpen, setMenuOpen] = useState(false);
  const [isQuickSyncing, setIsQuickSyncing] = useState(false);
  const [toast, setToast] = useState<{ message: string; type: 'success' | 'error' | 'info' } | null>(null);
  const menuRef = useRef<HTMLDivElement>(null);

  const loadUnclassifiedCount = useCallback(async () => {
    try {
      const unclassified = await getUnclassifiedClips();
      setUnclassifiedCount(unclassified.length);
    } catch (err) {
      console.error('Failed to load unclassified count:', err);
    }
  }, []);

  useEffect(() => {
    loadUnclassifiedCount();
  }, [pathname, loadUnclassifiedCount]);

  // Close menu when clicking outside
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (menuRef.current && !menuRef.current.contains(event.target as Node)) {
        setMenuOpen(false);
      }
    };

    if (menuOpen) {
      document.addEventListener('mousedown', handleClickOutside);
    }

    return () => {
      document.removeEventListener('mousedown', handleClickOutside);
    };
  }, [menuOpen]);

  const handleQuickSync = async () => {
    setIsQuickSyncing(true);
    setToast(null);
    setMenuOpen(false);

    try {
      // Check if root folder is configured
      const rootFolderSetting = await getRootFolder();
      if (!rootFolderSetting.rootFolderPath) {
        setToast({
          message: 'Root folder not configured. Please configure it in Settings first.',
          type: 'error',
        });
        setIsQuickSyncing(false);
        return;
      }

      // Perform sync with configured root folder
      const result = await syncClips();
      
      // Show success message with summary
      const summary = `Sync complete: ${result.totalScanned} scanned, ${result.totalAdded} added, ${result.totalRemoved} removed`;
      setToast({
        message: summary,
        type: 'success',
      });

      // Reload unclassified count
      await loadUnclassifiedCount();
      
      // Refresh the page if we're on the home page
      if (pathname === '/') {
        router.refresh();
      }
    } catch (err: any) {
      setToast({
        message: err.message || 'Failed to sync clips',
        type: 'error',
      });
    } finally {
      setIsQuickSyncing(false);
    }
  };

  const isActive = (path: string) => {
    if (path === '/') {
      return pathname === '/';
    }
    return pathname.startsWith(path);
  };

  const MenuLink = ({ href, icon, label, onClick }: { 
    href: string; 
    icon: React.ReactNode; 
    label: string;
    onClick?: () => void;
  }) => {
    const active = isActive(href);
    return (
      <Link
        href={href}
        onClick={() => {
          setMenuOpen(false);
          onClick?.();
        }}
        className={`flex items-center gap-3 px-4 py-2 text-sm transition-colors ${
          active
            ? 'bg-[#007BFF]/20 text-[#007BFF]'
            : 'text-gray-300 hover:bg-[#303030] hover:text-white'
        }`}
        aria-current={active ? 'page' : undefined}
      >
        <span className="w-5 h-5 flex-shrink-0">{icon}</span>
        <span>{label}</span>
      </Link>
    );
  };

  return (
    <>
      <nav className="bg-[#121212] border-b border-[#303030] sticky top-0 z-50" aria-label="Main navigation">
        <div className="container mx-auto px-4 sm:px-6 py-3 sm:py-4">
          <div className="flex items-center justify-between gap-4">
            {/* Logo */}
            <Link 
              href="/"
              className="text-2xl sm:text-3xl font-bold text-white hover:text-[#007BFF] transition-colors flex-shrink-0"
              aria-label="Clip AI - Home"
            >
              Clip AI
            </Link>
            
            {/* Primary Actions - Always Visible */}
            <div className="flex items-center gap-2 flex-1 justify-end">
              {/* Quick Sync - Primary Action */}
              <button
                onClick={handleQuickSync}
                disabled={isQuickSyncing}
                className="px-4 py-2 bg-[#007BFF] text-white rounded-lg hover:bg-[#0056b3] disabled:opacity-50 disabled:cursor-not-allowed transition-colors font-medium flex items-center gap-2"
                title="Quick sync with configured root folder"
                aria-label="Quick sync with configured root folder"
              >
                <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
                </svg>
                <span className="hidden sm:inline">{isQuickSyncing ? 'Syncing...' : 'Quick Sync'}</span>
              </button>
              
              {/* New Clip - Primary Action */}
              <Link
                href="/clips/new"
                className={`px-4 py-2 rounded-lg transition-colors font-medium flex items-center gap-2 ${
                  isActive('/clips/new')
                    ? 'bg-[#007BFF] text-white ring-2 ring-[#007BFF] ring-offset-2 ring-offset-[#121212]'
                    : 'bg-[#007BFF] text-white hover:bg-[#0056b3]'
                }`}
                aria-label="Add new clip"
                aria-current={isActive('/clips/new') ? 'page' : undefined}
              >
                <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
                </svg>
                <span className="hidden sm:inline">New Clip</span>
              </Link>
              
              {/* Unclassified - Contextual Badge */}
              {unclassifiedCount > 0 && (
                <Link
                  href="/clips/bulk-classify"
                  className="px-4 py-2 bg-yellow-600 text-white rounded-lg hover:bg-yellow-700 transition-colors font-medium flex items-center gap-2 relative"
                  aria-label={`${unclassifiedCount} unclassified clips`}
                >
                  <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M7 7h.01M7 3h5c.512 0 1.024.195 1.414.586l7 7a2 2 0 010 2.828l-7 7a2 2 0 01-2.828 0l-7-7A1.994 1.994 0 013 12V7a4 4 0 014-4z" />
                  </svg>
                  <span className="hidden sm:inline">Unclassified</span>
                  <span className="bg-red-500 text-white text-xs font-bold rounded-full px-2 py-0.5 min-w-[20px] text-center">
                    {unclassifiedCount}
                  </span>
                </Link>
              )}
              
              {/* Hamburger Menu - Secondary Actions */}
              <div className="relative" ref={menuRef}>
                <button
                  onClick={() => setMenuOpen(!menuOpen)}
                  className={`p-2 rounded-lg transition-colors ${
                    menuOpen 
                      ? 'bg-[#303030] text-white' 
                      : 'text-gray-400 hover:bg-[#303030] hover:text-white'
                  }`}
                  aria-label="More options"
                  aria-expanded={menuOpen}
                  aria-haspopup="true"
                >
                  <svg 
                    className="w-6 h-6" 
                    fill="none" 
                    stroke="currentColor" 
                    viewBox="0 0 24 24"
                    aria-hidden="true"
                  >
                    {menuOpen ? (
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                    ) : (
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 6h16M4 12h16M4 18h16" />
                    )}
                  </svg>
                </button>
                
                {menuOpen && (
                  <div className="absolute right-0 mt-2 w-56 bg-[#1a1a1a] border border-[#303030] rounded-lg shadow-xl py-2 z-50">
                    <MenuLink
                      href="/clips/bulk-upload"
                      icon={
                        <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M7 16a4 4 0 01-.88-7.903A5 5 0 1115.9 6L16 6a5 5 0 011 9.9M15 13l-3-3m0 0l-3 3m3-3v12" />
                        </svg>
                      }
                      label="Bulk Upload"
                    />
                    <MenuLink
                      href="/clips/sync"
                      icon={
                        <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
                        </svg>
                      }
                      label="Sync Filesystem"
                    />
                    <MenuLink
                      href="/tags"
                      icon={
                        <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M7 7h.01M7 3h5c.512 0 1.024.195 1.414.586l7 7a2 2 0 010 2.828l-7 7a2 2 0 01-2.828 0l-7-7A1.994 1.994 0 013 12V7a4 4 0 014-4z" />
                        </svg>
                      }
                      label="Manage Tags"
                    />
                    <div className="border-t border-[#303030] my-2" />
                    <MenuLink
                      href="/settings"
                      icon={
                        <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z" />
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                        </svg>
                      }
                      label="Settings"
                    />
                  </div>
                )}
              </div>
            </div>
          </div>
        </div>
      </nav>
      
      {/* Toast Notification */}
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



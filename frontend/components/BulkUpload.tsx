'use client';

import { useState, useRef, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { bulkUploadClips } from '@/lib/api/clips';
import { BulkUploadResponse } from '@/types';

interface BulkUploadProps {
  onComplete?: () => void;
  onClose?: () => void;
}

export default function BulkUpload({ onComplete, onClose }: BulkUploadProps) {
  const router = useRouter();
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [filePaths, setFilePaths] = useState<string[]>([]);
  const [basePath, setBasePath] = useState<string>('');
  const [isUploading, setIsUploading] = useState(false);
  const [result, setResult] = useState<BulkUploadResponse | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [dragActive, setDragActive] = useState(false);
  const [showPathInput, setShowPathInput] = useState(false);

  const validVideoExtensions = ['.mp4', '.webm', '.mov', '.avi', '.ogg'];

  const handleFileSelect = (files: FileList | null) => {
    if (!files) return;

    const paths: string[] = [];
    
    for (let i = 0; i < files.length; i++) {
      const file = files[i];
      const extension = file.name.substring(file.name.lastIndexOf('.')).toLowerCase();
      
      if (validVideoExtensions.includes(extension)) {
        // Add filename to the list - user will need to provide full path
        paths.push(file.name);
      }
    }

    setFilePaths(prev => {
      const newPaths = [...prev, ...paths];
      // Remove duplicates
      return [...new Set(newPaths)];
    });
    setError(null);
    // Show path input area after files are selected
    if (paths.length > 0) {
      setShowPathInput(true);
    }
  };

  const handleApplyBasePath = () => {
    if (!basePath.trim()) {
      setError('Please enter a base path');
      return;
    }

    // Ensure base path ends with backslash
    const normalizedBase = basePath.trim().replace(/\\$/, '') + '\\';
    
    setFilePaths(prev => prev.map(path => {
      // If path is already a full path, don't modify it
      if (path.includes(':\\') || path.startsWith('\\\\')) {
        return path;
      }
      // Otherwise, prepend the base path
      return normalizedBase + path;
    }));
    setError(null);
  };

  const handleDragEnter = (e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
    if (e.dataTransfer.types.includes('Files')) {
      setDragActive(true);
    }
  };

  const handleDragLeave = (e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
    // Only set dragActive to false if we're leaving the drop zone itself, not a child element
    const rect = e.currentTarget.getBoundingClientRect();
    const x = e.clientX;
    const y = e.clientY;
    if (x < rect.left || x > rect.right || y < rect.top || y > rect.bottom) {
      setDragActive(false);
    }
  };

  const handleDragOver = (e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
    if (e.dataTransfer.types.includes('Files')) {
      e.dataTransfer.dropEffect = 'copy';
    }
  };

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
    setDragActive(false);

    if (e.dataTransfer.files && e.dataTransfer.files.length > 0) {
      handleFileSelect(e.dataTransfer.files);
    }
  };

  const handleClickBrowse = () => {
    fileInputRef.current?.click();
  };

  const handleFileInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files) {
      handleFileSelect(e.target.files);
      // Reset the input so the same file can be selected again if needed
      if (fileInputRef.current) {
        fileInputRef.current.value = '';
      }
    }
  };

  // Prevent body scroll when modal is open
  useEffect(() => {
    document.body.style.overflow = 'hidden';
    return () => {
      document.body.style.overflow = 'unset';
    };
  }, []);

  const handleUpload = async () => {
    if (filePaths.length === 0) {
      setError('Please select at least one file');
      return;
    }

    setIsUploading(true);
    setError(null);
    setResult(null);

    try {
      // Note: Browser security prevents accessing full file paths
      // For now, we'll use filenames. In a desktop app, these would be full paths
      const response = await bulkUploadClips(filePaths);
      setResult(response);
      
      if (response.successes.length > 0) {
        if (onComplete) {
          onComplete();
        }
        // Close modal after successful upload
        setTimeout(() => {
          if (onClose) {
            onClose();
          } else {
            router.push('/');
          }
        }, 1500);
      }
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Failed to upload clips');
    } finally {
      setIsUploading(false);
    }
  };

  const handleCancel = () => {
    if (onClose) {
      onClose();
    } else {
      router.back();
    }
  };

  return (
    <div 
      className="fixed inset-0 bg-black/50 flex items-center justify-center z-50" 
      onClick={handleCancel}
      onDragOver={(e) => {
        e.preventDefault();
        e.stopPropagation();
      }}
      onDrop={(e) => {
        e.preventDefault();
        e.stopPropagation();
      }}
    >
      <div 
        className="bg-[#3a3a3a] rounded-lg p-6 w-full max-w-2xl mx-4 shadow-xl"
        onClick={(e) => e.stopPropagation()}
        onDragOver={(e) => {
          e.preventDefault();
          e.stopPropagation();
        }}
      >
        {/* Title */}
        <h2 className="text-xl font-semibold text-white mb-6">Add New Clips</h2>

        {/* Drag & Drop Area */}
        <div
          className={`border-2 border-dashed rounded-lg p-12 text-center cursor-pointer transition-colors ${
            dragActive 
              ? 'border-blue-500 bg-blue-500/10' 
              : 'border-blue-400 bg-[#1e1e1e] hover:bg-[#252525]'
          }`}
          onDragEnter={handleDragEnter}
          onDragLeave={handleDragLeave}
          onDragOver={handleDragOver}
          onDrop={handleDrop}
          onClick={handleClickBrowse}
        >
          {/* Upload Icon */}
          <div className="flex justify-center mb-4">
            <svg 
              className="w-16 h-16 text-white/70" 
              fill="none" 
              stroke="currentColor" 
              viewBox="0 0 24 24"
            >
              <path 
                strokeLinecap="round" 
                strokeLinejoin="round" 
                strokeWidth={2} 
                d="M7 16a4 4 0 01-.88-7.903A5 5 0 1115.9 6L16 6a5 5 0 011 9.9M15 13l-3-3m0 0l-3 3m3-3v12" 
              />
            </svg>
          </div>
          
          {/* Text */}
          <p className="text-gray-300 text-lg mb-2">Drag & drop video files here</p>
          <p className="text-orange-300 text-sm cursor-pointer hover:text-orange-200 mb-2">
            or click to browse
          </p>
          <p className="text-yellow-400 text-xs mt-3 pt-3 border-t border-yellow-400/20">
            Note: You&apos;ll need to provide full file paths after selecting files
          </p>
        </div>

        {/* Hidden file input */}
        <input
          ref={fileInputRef}
          type="file"
          multiple
          accept="video/*,.mp4,.webm,.mov,.avi,.ogg"
          onChange={handleFileInputChange}
          className="hidden"
        />

        {/* File paths input area */}
        {showPathInput && (
          <div className="mt-4 space-y-3">
            <div className="bg-yellow-900/20 border border-yellow-700 text-yellow-300 px-4 py-3 rounded text-sm">
              <p className="font-semibold mb-1">Browser Limitation</p>
              <p>Browsers cannot access full file paths for security reasons. Please provide the full Windows file paths below.</p>
            </div>

            <div>
              <label htmlFor="basePath" className="block text-sm font-medium text-white mb-2">
                Base Directory Path (optional - applies to filenames only)
              </label>
              <div className="flex gap-2">
                <input
                  id="basePath"
                  type="text"
                  value={basePath}
                  onChange={(e) => setBasePath(e.target.value)}
                  placeholder="C:\\Videos"
                  className="flex-1 px-3 py-2 border border-[#303030] rounded-lg bg-[#2a2a2a] text-white text-sm font-mono placeholder:text-gray-500"
                />
                <button
                  onClick={handleApplyBasePath}
                  className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors text-sm"
                >
                  Apply to Filenames
                </button>
              </div>
              <p className="text-xs text-gray-500 mt-1">
                Enter the folder path (e.g., C:\Videos) and click &quot;Apply&quot; to prepend it to filenames
              </p>
            </div>

            <div>
              <div className="flex justify-between items-center mb-2">
                <label htmlFor="filePaths" className="block text-sm font-medium text-white">
                  File Paths (one per line) - Edit as needed
                </label>
                <button
                  onClick={() => {
                    setFilePaths([]);
                    setBasePath('');
                    setShowPathInput(false);
                  }}
                  className="text-xs text-red-400 hover:text-red-300"
                >
                  Clear All
                </button>
              </div>
              <textarea
                id="filePaths"
                value={filePaths.join('\n')}
                onChange={(e) => {
                  const paths = e.target.value.split('\n').filter(p => p.trim().length > 0);
                  setFilePaths(paths);
                  setError(null);
                }}
                rows={10}
                className="w-full px-3 py-2 border border-[#303030] rounded-lg bg-[#2a2a2a] text-white text-sm font-mono placeholder:text-gray-500 resize-none"
                placeholder="C:\Videos\clip1.mp4&#10;C:\Videos\clip2.mp4"
              />
              <p className="text-xs text-gray-500 mt-1">
                {filePaths.length} file{filePaths.length !== 1 ? 's' : ''} • Enter full Windows file paths (e.g., C:\Videos\clip.mp4)
              </p>
            </div>
          </div>
        )}

        {/* Error message */}
        {error && (
          <div className="mt-4 bg-red-900/20 border border-red-700 text-red-300 px-4 py-3 rounded text-sm">
            {error}
          </div>
        )}

        {/* Success message */}
        {result && result.successes.length > 0 && (
          <div className="mt-4 bg-green-900/20 border border-green-700 text-green-300 px-4 py-3 rounded text-sm">
            Successfully uploaded {result.successes.length} clip(s)
          </div>
        )}

        {/* Action buttons */}
        <div className="flex justify-end gap-3 mt-6">
          <button
            onClick={handleCancel}
            className="px-4 py-2 bg-[#3a3a3a] text-white rounded-lg hover:bg-[#4a4a4a] transition-colors font-medium"
          >
            Cancel
          </button>
          <button
            onClick={handleUpload}
            disabled={isUploading || filePaths.length === 0}
            className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors font-medium"
          >
            {isUploading ? 'Uploading...' : 'Add Clips'}
          </button>
        </div>
      </div>
    </div>
  );
}


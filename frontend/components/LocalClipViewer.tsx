'use client';

import { useState, useRef } from 'react';

interface LocalClipViewerProps {
  filePath: string;
  clipId: number;
}

const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5059';

export default function LocalClipViewer({ filePath, clipId }: LocalClipViewerProps) {
  const [copied, setCopied] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const videoRef = useRef<HTMLVideoElement>(null);

  const handleCopyPath = async () => {
    try {
      await navigator.clipboard.writeText(filePath);
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    } catch (err) {
      console.error('Failed to copy path:', err);
      // Fallback for older browsers
      const textArea = document.createElement('textarea');
      textArea.value = filePath;
      textArea.style.position = 'fixed';
      textArea.style.left = '-999999px';
      document.body.appendChild(textArea);
      textArea.select();
      try {
        document.execCommand('copy');
        setCopied(true);
        setTimeout(() => setCopied(false), 2000);
      } catch (err) {
        console.error('Fallback copy failed:', err);
      }
      document.body.removeChild(textArea);
    }
  };

  const videoUrl = `${API_BASE_URL}/api/clips/${clipId}/video`;

  return (
    <div className="w-full">
      {/* Video Player */}
      <div className="w-full aspect-video bg-black rounded-lg overflow-hidden mb-4">
        <video
          ref={videoRef}
          src={videoUrl}
          controls
          className="w-full h-full"
          onError={(e) => {
            console.error('Video playback error:', e);
            setError('Failed to load video. The file may not exist or may be in an unsupported format.');
          }}
        >
          Your browser does not support the video tag.
        </video>
      </div>

      {/* Error Message */}
      {error && (
        <div className="bg-red-100 border border-red-400 text-red-700 px-4 py-3 rounded mb-4">
          {error}
        </div>
      )}

      {/* File Path Info */}
      <div className="bg-gray-100 border-2 border-dashed border-gray-300 rounded-lg p-4">
        <p className="text-sm text-gray-600 mb-2">Local File Path:</p>
        <p className="text-sm font-mono text-gray-800 mb-3 break-all bg-white p-3 rounded border border-gray-300">
          {filePath}
        </p>
        <button
          onClick={handleCopyPath}
          className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors"
        >
          {copied ? '✓ Copied!' : 'Copy Path to Clipboard'}
        </button>
      </div>
    </div>
  );
}


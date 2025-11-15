'use client';

import { SyncPreviewResponse } from '@/types';

interface ReconciliationSummaryProps {
  preview: SyncPreviewResponse;
}

export default function ReconciliationSummary({ preview }: ReconciliationSummaryProps) {
  return (
    <div className="bg-[#1a1a1a] border border-[#303030] rounded-lg p-6">
      <h2 className="text-xl font-semibold mb-4">Reconciliation Summary</h2>
      <div className="grid grid-cols-2 md:grid-cols-5 gap-4">
        <div className="bg-[#2a2a2a] rounded-lg p-4">
          <div className="text-2xl font-bold text-blue-400">{preview.totalScanned}</div>
          <div className="text-sm text-gray-400 mt-1">Files Scanned</div>
        </div>
        <div className="bg-[#2a2a2a] rounded-lg p-4">
          <div className="text-2xl font-bold text-green-400">{preview.matchedFilesCount}</div>
          <div className="text-sm text-gray-400 mt-1">Matched</div>
        </div>
        <div className="bg-[#2a2a2a] rounded-lg p-4">
          <div className="text-2xl font-bold text-yellow-400">{preview.newFilesCount}</div>
          <div className="text-sm text-gray-400 mt-1">New Files</div>
        </div>
        <div className="bg-[#2a2a2a] rounded-lg p-4">
          <div className="text-2xl font-bold text-red-400">{preview.missingFilesCount}</div>
          <div className="text-sm text-gray-400 mt-1">Missing Files</div>
        </div>
        <div className="bg-[#2a2a2a] rounded-lg p-4">
          <div className="text-2xl font-bold text-gray-400">{preview.errorCount}</div>
          <div className="text-sm text-gray-400 mt-1">Errors</div>
        </div>
      </div>
      {preview.rootFolderPath && (
        <div className="mt-4 text-sm text-gray-400">
          <span className="font-medium">Root Folder:</span>{' '}
          <span className="font-mono">{preview.rootFolderPath}</span>
        </div>
      )}
    </div>
  );
}


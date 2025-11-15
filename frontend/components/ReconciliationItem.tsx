'use client';

import { ReconciliationItem as ReconciliationItemType } from '@/types';

interface ReconciliationItemProps {
  item: ReconciliationItemType;
  isSelected: boolean;
  onToggle: () => void;
  selectable: boolean;
}

export default function ReconciliationItem({ item, isSelected, onToggle, selectable }: ReconciliationItemProps) {
  const formatFileSize = (bytes?: number) => {
    if (!bytes) return 'N/A';
    const mb = bytes / (1024 * 1024);
    if (mb >= 1) {
      return `${mb.toFixed(2)} MB`;
    }
    const kb = bytes / 1024;
    return `${kb.toFixed(2)} KB`;
  };

  const formatDate = (dateString?: string) => {
    if (!dateString) return 'N/A';
    try {
      const date = new Date(dateString);
      return date.toLocaleDateString() + ' ' + date.toLocaleTimeString();
    } catch {
      return 'N/A';
    }
  };

  const getStatusColor = () => {
    switch (item.status) {
      case 'matched':
        return 'border-green-500/50 bg-green-500/10';
      case 'new':
        return 'border-yellow-500/50 bg-yellow-500/10';
      case 'missing':
        return 'border-red-500/50 bg-red-500/10';
      case 'error':
        return 'border-gray-500/50 bg-gray-500/10';
      default:
        return 'border-[#303030] bg-[#2a2a2a]';
    }
  };

  const getStatusBadge = () => {
    switch (item.status) {
      case 'matched':
        return <span className="px-2 py-1 text-xs bg-green-500 text-white rounded font-medium">Matched</span>;
      case 'new':
        return <span className="px-2 py-1 text-xs bg-yellow-500 text-yellow-900 rounded font-medium">New File</span>;
      case 'missing':
        return <span className="px-2 py-1 text-xs bg-red-500 text-white rounded font-medium">Missing</span>;
      case 'error':
        return <span className="px-2 py-1 text-xs bg-gray-500 text-white rounded font-medium">Error</span>;
      default:
        return null;
    }
  };

  const fileName = item.filePath.split(/[/\\]/).pop() || item.filePath;

  return (
    <div className={`border rounded-lg p-4 ${getStatusColor()} ${selectable && (item.status === 'new' || item.status === 'missing') ? 'cursor-pointer hover:opacity-80' : ''}`}>
      <div className="flex items-start gap-3">
        {selectable && (item.status === 'new' || item.status === 'missing') && (
          <input
            type="checkbox"
            checked={isSelected}
            onChange={onToggle}
            className="mt-1 w-4 h-4 text-blue-600 bg-gray-100 border-gray-300 rounded focus:ring-blue-500"
          />
        )}
        <div className="flex-1 min-w-0">
          <div className="flex items-center justify-between mb-2">
            <h3 className="font-medium text-white truncate">{item.title || fileName}</h3>
            {getStatusBadge()}
          </div>
          
          <div className="text-xs text-gray-400 font-mono mb-2 break-all">
            {item.filePath}
          </div>

          {item.directory && (
            <div className="text-xs text-gray-500 mb-2">
              📁 {item.directory}
            </div>
          )}

          <div className="flex flex-wrap gap-4 text-xs text-gray-400 mt-2">
            {item.fileSize !== undefined && (
              <span>Size: {formatFileSize(item.fileSize)}</span>
            )}
            {item.lastModified && (
              <span>Modified: {formatDate(item.lastModified)}</span>
            )}
            {item.clipId && (
              <span>Clip ID: {item.clipId}</span>
            )}
          </div>

          {item.description && (
            <p className="text-sm text-gray-300 mt-2 line-clamp-2">{item.description}</p>
          )}

          {item.tags && item.tags.length > 0 && (
            <div className="flex flex-wrap gap-2 mt-2">
              {item.tags.map((tag) => (
                <span
                  key={tag.id}
                  className="px-2 py-1 text-xs bg-[#007BFF] text-white rounded-full font-medium"
                >
                  {tag.value}
                </span>
              ))}
            </div>
          )}

          {item.errorMessage && (
            <div className="mt-2 text-xs text-red-400">
              ⚠ {item.errorMessage}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}


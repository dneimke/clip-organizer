'use client';

import { useState, FormEvent } from 'react';

interface AIQueryInputProps {
  onQuerySubmit: (query: string) => void;
  isLoading?: boolean;
  placeholder?: string;
}

export default function AIQueryInput({ 
  onQuerySubmit, 
  isLoading = false,
  placeholder = "Ask me anything about your clips... (e.g., 'Show me clips with successful PC attacks')"
}: AIQueryInputProps) {
  const [query, setQuery] = useState('');

  const handleSubmit = (e: FormEvent) => {
    e.preventDefault();
    if (query.trim() && !isLoading) {
      onQuerySubmit(query.trim());
    }
  };

  return (
    <form onSubmit={handleSubmit} className="w-full">
      <div className="relative">
        <input
          type="text"
          value={query}
          onChange={(e) => setQuery(e.target.value)}
          placeholder={placeholder}
          disabled={isLoading}
          className="w-full px-4 py-3 pl-12 pr-24 border border-[#303030] rounded-lg focus:outline-none focus:ring-2 focus:ring-[#007BFF] focus:border-[#007BFF] bg-[#202020] text-white placeholder:text-gray-500 disabled:opacity-50 disabled:cursor-not-allowed text-lg"
        />
        <svg
          className="absolute left-4 top-3.5 h-5 w-5 text-gray-500"
          fill="none"
          stroke="currentColor"
          viewBox="0 0 24 24"
        >
          <path
            strokeLinecap="round"
            strokeLinejoin="round"
            strokeWidth={2}
            d="M8.228 9c.549-1.165 2.03-2 3.772-2 2.21 0 4 1.343 4 3 0 1.4-1.278 2.575-3.006 2.907-.542.104-.994.54-.994 1.093m0 3h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
          />
        </svg>
        <button
          type="submit"
          disabled={!query.trim() || isLoading}
          className="absolute right-2 top-2 px-4 py-1.5 bg-[#007BFF] text-white rounded-md hover:bg-[#0056b3] disabled:opacity-50 disabled:cursor-not-allowed transition-colors font-medium flex items-center gap-2"
        >
          {isLoading ? (
            <>
              <svg className="animate-spin h-4 w-4" fill="none" viewBox="0 0 24 24">
                <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
              </svg>
              <span>Parsing...</span>
            </>
          ) : (
            <>
              <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
              </svg>
              <span>Search</span>
            </>
          )}
        </button>
      </div>
      <div className="mt-2 text-xs text-gray-400">
        <p>Try: &quot;Show me clips with successful PC attacks&quot;, &quot;Find videos in the midfield area&quot;, &quot;What unclassified clips do I have?&quot;</p>
      </div>
    </form>
  );
}


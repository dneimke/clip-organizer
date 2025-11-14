'use client';

import ClipForm from '@/components/ClipForm';

export default function NewClipPage() {
  return (
    <div className="min-h-screen bg-[#121212]">
      <div className="container mx-auto px-4 py-8">
        <h1 className="text-3xl font-bold text-white mb-6">Add New Video Clip</h1>
        <div className="bg-[#1e1e1e] rounded-lg shadow-lg border border-[#303030] p-6">
          <ClipForm />
        </div>
      </div>
    </div>
  );
}


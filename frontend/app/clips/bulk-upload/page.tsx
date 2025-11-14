'use client';

import { useRouter } from 'next/navigation';
import BulkUpload from '@/components/BulkUpload';

export default function BulkUploadPage() {
  const router = useRouter();

  return (
    <BulkUpload 
      onClose={() => router.push('/')}
      onComplete={() => {
        // Optionally navigate to bulk classify after completion
        router.push('/clips/bulk-classify');
      }}
    />
  );
}


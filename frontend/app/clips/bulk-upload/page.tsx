'use client';

import { useRouter } from 'next/navigation';
import BulkUpload from '@/components/BulkUpload';

export default function BulkUploadPage() {
  const router = useRouter();

  return (
    <BulkUpload 
      onClose={() => router.push('/')}
      onComplete={() => {
        // Navigate to home page after completion
        router.push('/');
      }}
    />
  );
}


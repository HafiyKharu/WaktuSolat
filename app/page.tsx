import { Suspense } from 'react';
import PrayerTimesDisplay from '@/components/PrayerTimesDisplay';
import ZoneSelector from '@/components/ZoneSelector';
import { Clock } from 'lucide-react';

export default function Home() {
  return (
    <main className="min-h-screen bg-gradient-to-br from-blue-50 to-indigo-100 dark:from-gray-900 dark:to-gray-800">
      <div className="container mx-auto px-4 py-8">
        <header className="text-center mb-8">
          <div className="flex items-center justify-center gap-3 mb-4">
            <Clock className="w-10 h-10 text-indigo-600 dark:text-indigo-400" />
            <h1 className="text-4xl font-bold text-gray-800 dark:text-white">
              Waktu Solat Malaysia
            </h1>
          </div>
          <p className="text-gray-600 dark:text-gray-300">
            Jadual waktu solat dari e-solat.gov.my
          </p>
        </header>

        <div className="max-w-4xl mx-auto">
          <Suspense fallback={<LoadingSkeleton />}>
            <ZoneSelector />
          </Suspense>

          <Suspense fallback={<LoadingSkeleton />}>
            <PrayerTimesDisplay />
          </Suspense>
        </div>
      </div>
    </main>
  );
}

function LoadingSkeleton() {
  return (
    <div className="bg-white dark:bg-gray-800 rounded-lg shadow-lg p-6 mb-6 animate-pulse">
      <div className="h-8 bg-gray-200 dark:bg-gray-700 rounded w-3/4 mb-4"></div>
      <div className="h-4 bg-gray-200 dark:bg-gray-700 rounded w-1/2 mb-6"></div>
      <div className="space-y-4">
        {[1, 2, 3, 4, 5].map((i) => (
          <div key={i} className="h-16 bg-gray-200 dark:bg-gray-700 rounded"></div>
        ))}
      </div>
    </div>
  );
}

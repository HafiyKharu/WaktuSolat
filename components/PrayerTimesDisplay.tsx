'use client';

import { useEffect, useState } from 'react';
import { WaktuSolatData, NextPrayer, PrayerName } from '@/lib/types';
import { getTimeRemaining, formatDate, parseTime } from '@/lib/utils';
import { 
  Sun, 
  Sunrise, 
  Sunset, 
  Moon,
  Clock,
  MapPin,
  Calendar
} from 'lucide-react';

const PRAYER_ICONS: Record<PrayerName, React.ReactNode> = {
  imsak: <Moon className="w-5 h-5" />,
  subuh: <Sunrise className="w-5 h-5" />,
  syuruk: <Sun className="w-5 h-5" />,
  dhuha: <Sun className="w-5 h-5" />,
  zohor: <Sun className="w-5 h-5" />,
  asar: <Sun className="w-5 h-5" />,
  maghrib: <Sunset className="w-5 h-5" />,
  isyak: <Moon className="w-5 h-5" />,
};

const PRAYER_NAMES: Record<PrayerName, string> = {
  imsak: 'Imsak',
  subuh: 'Subuh',
  syuruk: 'Syuruk',
  dhuha: 'Dhuha',
  zohor: 'Zohor',
  asar: 'Asar',
  maghrib: 'Maghrib',
  isyak: 'Isyak',
};

export default function PrayerTimesDisplay() {
  const [prayerTimes, setPrayerTimes] = useState<WaktuSolatData | null>(null);
  const [nextPrayer, setNextPrayer] = useState<NextPrayer | null>(null);
  const [timeRemaining, setTimeRemaining] = useState<string>('');
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    fetchPrayerTimes();
  }, []);

  useEffect(() => {
    if (prayerTimes) {
      updateNextPrayer();
      const interval = setInterval(updateNextPrayer, 1000);
      return () => clearInterval(interval);
    }
  }, [prayerTimes]);

  async function fetchPrayerTimes() {
    try {
      setLoading(true);
      const zone = localStorage.getItem('selectedZone') || 'SGR01';
      const response = await fetch(`/api/prayer-times?zone=${zone}`);
      
      if (!response.ok) throw new Error('Failed to fetch prayer times');
      
      const data = await response.json();
      setPrayerTimes(data);
    } catch (error) {
      console.error('Error fetching prayer times:', error);
    } finally {
      setLoading(false);
    }
  }

  function updateNextPrayer() {
    if (!prayerTimes) return;

    const now = new Date();
    const prayers: { name: PrayerName; time: string }[] = [
      { name: 'imsak', time: prayerTimes.imsak },
      { name: 'subuh', time: prayerTimes.subuh },
      { name: 'syuruk', time: prayerTimes.syuruk },
      { name: 'dhuha', time: prayerTimes.dhuha },
      { name: 'zohor', time: prayerTimes.zohor },
      { name: 'asar', time: prayerTimes.asar },
      { name: 'maghrib', time: prayerTimes.maghrib },
      { name: 'isyak', time: prayerTimes.isyak },
    ];

    let next: NextPrayer | null = null;

    for (const prayer of prayers) {
      const prayerTime = parseTime(prayer.time);
      if (prayerTime > now) {
        next = {
          name: prayer.name,
          time: prayer.time,
          timeRemaining: getTimeRemaining(prayer.time),
        };
        break;
      }
    }

    if (!next) {
      // All prayers passed, next is tomorrow's Imsak
      next = {
        name: 'imsak',
        time: prayers[0].time,
        timeRemaining: getTimeRemaining(prayers[0].time),
      };
    }

    setNextPrayer(next);
    setTimeRemaining(next.timeRemaining);
  }

  if (loading) {
    return <LoadingSkeleton />;
  }

  if (!prayerTimes) {
    return (
      <div className="bg-white dark:bg-gray-800 rounded-lg shadow-lg p-8 text-center">
        <p className="text-gray-600 dark:text-gray-300">
          No prayer times available. Please select a zone and scrape data.
        </p>
      </div>
    );
  }

  const prayers: PrayerName[] = ['imsak', 'subuh', 'syuruk', 'dhuha', 'zohor', 'asar', 'maghrib', 'isyak'];

  return (
    <div className="space-y-6 animate-fade-in">
      {/* Location and Date Info */}
      <div className="bg-white dark:bg-gray-800 rounded-lg shadow-lg p-6">
        <div className="flex items-center gap-2 mb-2">
          <MapPin className="w-5 h-5 text-indigo-600 dark:text-indigo-400" />
          <h2 className="text-xl font-semibold text-gray-800 dark:text-white">
            {prayerTimes.czone}
          </h2>
        </div>
        <p className="text-gray-600 dark:text-gray-300 mb-2">{prayerTimes.cbearing}</p>
        <div className="flex items-center gap-2 text-sm text-gray-500 dark:text-gray-400">
          <Calendar className="w-4 h-4" />
          <span>{prayerTimes.tarikhMasehi}</span>
          <span className="mx-2">•</span>
          <span>{prayerTimes.tarikhHijrah}</span>
        </div>
      </div>

      {/* Next Prayer Countdown */}
      {nextPrayer && (
        <div className="bg-gradient-to-r from-indigo-500 to-purple-600 text-white rounded-lg shadow-lg p-6">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm opacity-90 mb-1">Solat Seterusnya</p>
              <h3 className="text-2xl font-bold">{PRAYER_NAMES[nextPrayer.name]}</h3>
              <p className="text-lg mt-1">{nextPrayer.time}</p>
            </div>
            <div className="text-right">
              <div className="flex items-center gap-2 mb-2">
                <Clock className="w-6 h-6" />
              </div>
              <p className="text-3xl font-mono font-bold">{timeRemaining}</p>
            </div>
          </div>
        </div>
      )}

      {/* Prayer Times List */}
      <div className="bg-white dark:bg-gray-800 rounded-lg shadow-lg overflow-hidden">
        <div className="divide-y divide-gray-200 dark:divide-gray-700">
          {prayers.map((prayer) => {
            const time = prayerTimes[prayer];
            const isNext = nextPrayer?.name === prayer;
            
            return (
              <div
                key={prayer}
                className={`flex items-center justify-between p-4 transition-colors ${
                  isNext
                    ? 'bg-indigo-50 dark:bg-indigo-900/20 border-l-4 border-indigo-600'
                    : 'hover:bg-gray-50 dark:hover:bg-gray-700/50'
                }`}
              >
                <div className="flex items-center gap-3">
                  <div className="text-indigo-600 dark:text-indigo-400">
                    {PRAYER_ICONS[prayer]}
                  </div>
                  <span className={`font-medium ${
                    isNext
                      ? 'text-indigo-900 dark:text-indigo-100'
                      : 'text-gray-800 dark:text-gray-200'
                  }`}>
                    {PRAYER_NAMES[prayer]}
                  </span>
                </div>
                <span className={`text-lg font-mono ${
                  isNext
                    ? 'text-indigo-900 dark:text-indigo-100 font-bold'
                    : 'text-gray-700 dark:text-gray-300'
                }`}>
                  {time}
                </span>
              </div>
            );
          })}
        </div>
      </div>
    </div>
  );
}

function LoadingSkeleton() {
  return (
    <div className="space-y-6">
      <div className="bg-white dark:bg-gray-800 rounded-lg shadow-lg p-6 animate-pulse">
        <div className="h-6 bg-gray-200 dark:bg-gray-700 rounded w-2/3 mb-2"></div>
        <div className="h-4 bg-gray-200 dark:bg-gray-700 rounded w-1/2 mb-4"></div>
        <div className="h-4 bg-gray-200 dark:bg-gray-700 rounded w-1/3"></div>
      </div>
      <div className="bg-white dark:bg-gray-800 rounded-lg shadow-lg p-6 animate-pulse">
        <div className="h-20 bg-gray-200 dark:bg-gray-700 rounded"></div>
      </div>
      <div className="bg-white dark:bg-gray-800 rounded-lg shadow-lg p-6 animate-pulse space-y-4">
        {[1, 2, 3, 4, 5, 6, 7, 8].map((i) => (
          <div key={i} className="h-12 bg-gray-200 dark:bg-gray-700 rounded"></div>
        ))}
      </div>
    </div>
  );
}

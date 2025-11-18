'use client';

import { useEffect, useState } from 'react';
import { ZoneGroup } from '@/lib/types';
import { MapPin, RefreshCw } from 'lucide-react';

export default function ZoneSelector() {
  const [zones, setZones] = useState<ZoneGroup[]>([]);
  const [selectedZone, setSelectedZone] = useState<string>('');
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);

  useEffect(() => {
    fetchZones();
    const savedZone = localStorage.getItem('selectedZone');
    if (savedZone) {
      setSelectedZone(savedZone);
    }
  }, []);

  async function fetchZones() {
    try {
      setLoading(true);
      const response = await fetch('/api/zones');
      
      if (!response.ok) throw new Error('Failed to fetch zones');
      
      const data = await response.json();
      setZones(data);
      
      if (!selectedZone && data.length > 0 && data[0].zones.length > 0) {
        setSelectedZone(data[0].zones[0].code);
      }
    } catch (error) {
      console.error('Error fetching zones:', error);
    } finally {
      setLoading(false);
    }
  }

  async function handleZoneChange(code: string) {
    setSelectedZone(code);
    localStorage.setItem('selectedZone', code);
    
    // Trigger refresh of prayer times
    window.location.reload();
  }

  async function handleRefresh() {
    try {
      setRefreshing(true);
      const response = await fetch('/api/scrape/zones', {
        method: 'POST',
      });
      
      if (!response.ok) throw new Error('Failed to scrape zones');
      
      await fetchZones();
    } catch (error) {
      console.error('Error scraping zones:', error);
      alert('Failed to refresh zones. Please try again.');
    } finally {
      setRefreshing(false);
    }
  }

  if (loading) {
    return (
      <div className="bg-white dark:bg-gray-800 rounded-lg shadow-lg p-6 mb-6 animate-pulse">
        <div className="h-10 bg-gray-200 dark:bg-gray-700 rounded"></div>
      </div>
    );
  }

  return (
    <div className="bg-white dark:bg-gray-800 rounded-lg shadow-lg p-6 mb-6">
      <div className="flex items-center gap-4">
        <div className="flex-1">
          <label className="flex items-center gap-2 text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
            <MapPin className="w-4 h-4" />
            Pilih Zon
          </label>
          <select
            value={selectedZone}
            onChange={(e) => handleZoneChange(e.target.value)}
            className="w-full px-4 py-2 border border-gray-300 dark:border-gray-600 rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-transparent bg-white dark:bg-gray-700 text-gray-900 dark:text-white"
          >
            {zones.map((group) => (
              <optgroup key={group.state} label={group.state}>
                {group.zones.map((zone) => (
                  <option key={zone.code} value={zone.code}>
                    {zone.location}
                  </option>
                ))}
              </optgroup>
            ))}
          </select>
        </div>
        
        <button
          onClick={handleRefresh}
          disabled={refreshing}
          className="mt-7 px-4 py-2 bg-indigo-600 hover:bg-indigo-700 disabled:bg-gray-400 text-white rounded-lg transition-colors flex items-center gap-2"
        >
          <RefreshCw className={`w-4 h-4 ${refreshing ? 'animate-spin' : ''}`} />
          {refreshing ? 'Refreshing...' : 'Refresh Zones'}
        </button>
      </div>
    </div>
  );
}

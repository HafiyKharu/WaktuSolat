'use client';

import { useEffect, useMemo, useRef, useState } from 'react';
import { ZoneGroup } from '@/lib/types';
import { MapPin, RefreshCw, Search, Check } from 'lucide-react';

type FlatZone = { code: string; location: string; state: string };

export default function ZoneSelector() {
  const [zones, setZones] = useState<ZoneGroup[]>([]);
  const [selectedZone, setSelectedZone] = useState<string>('');
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);

  // Combobox UI state
  const [open, setOpen] = useState(false);
  const [query, setQuery] = useState('');
  const containerRef = useRef<HTMLDivElement | null>(null);

  // Initialize from localStorage first to avoid flicker/reset
  useEffect(() => {
    const saved = localStorage.getItem('selectedZone');
    if (saved) setSelectedZone(saved);
    fetchZones();
  }, []);

  // If no saved selection, set a sensible default once zones arrive
  useEffect(() => {
    if (!selectedZone && zones.length > 0 && zones[0].zones.length > 0) {
      setSelectedZone(zones[0].zones[0].code);
    }
  }, [zones, selectedZone]);

  useEffect(() => {
    function onClickOutside(e: MouseEvent) {
      if (containerRef.current && !containerRef.current.contains(e.target as Node)) {
        setOpen(false);
      }
    }
    document.addEventListener('mousedown', onClickOutside);
    return () => document.removeEventListener('mousedown', onClickOutside);
  }, []);

  async function fetchZones() {
    try {
      setLoading(true);
      const response = await fetch('/api/zones');
      if (!response.ok) throw new Error('Failed to fetch zones');
      const data = await response.json();
      setZones(data);
    } catch (error) {
      console.error('Error fetching zones:', error);
    } finally {
      setLoading(false);
    }
  }

  function handleSelect(code: string) {
    setSelectedZone(code);
    localStorage.setItem('selectedZone', code);
    setOpen(false);
    // Reload to let PrayerTimesDisplay refetch based on new zone
    window.location.reload();
  }

  async function handleRefresh() {
    try {
      setRefreshing(true);
      const response = await fetch('/api/scrape/zones', { method: 'POST' });
      if (!response.ok) throw new Error('Failed to scrape zones');
      await fetchZones();
    } catch (error) {
      console.error('Error scraping zones:', error);
      alert('Failed to refresh zones. Please try again.');
    } finally {
      setRefreshing(false);
    }
  }

  const flatZones: FlatZone[] = useMemo(() => {
    return zones.flatMap((g) => g.zones.map((z) => ({ code: z.code, location: z.location, state: g.state })));
  }, [zones]);

  const filteredZones = useMemo(() => {
    const q = query.trim().toLowerCase();
    if (!q) return flatZones;
    return flatZones.filter((z) =>
      z.location.toLowerCase().includes(q) || z.code.toLowerCase().includes(q) || z.state.toLowerCase().includes(q)
    );
  }, [flatZones, query]);

  const selectedLabel = useMemo(() => {
    const found = flatZones.find((z) => z.code === selectedZone);
    return found ? `${found.location} (${found.code})` : selectedZone || 'Pilih zon...';
  }, [flatZones, selectedZone]);

  if (loading) {
    return (
      <div className="bg-white dark:bg-gray-800 rounded-lg shadow-lg p-6 mb-6 animate-pulse">
        <div className="h-10 bg-gray-200 dark:bg-gray-700 rounded"></div>
      </div>
    );
  }

  return (
    <div className="bg-white dark:bg-gray-800 rounded-lg shadow-lg p-6 mb-6">
      <div className="flex items-start gap-4" ref={containerRef}>
        <div className="flex-1">
          <label className="flex items-center gap-2 text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
            <MapPin className="w-4 h-4" />
            Pilih Zon
          </label>

          {/* Combobox trigger */}
          <button
            type="button"
            onClick={() => setOpen((o) => !o)}
            className="w-full px-4 py-2 border border-gray-300 dark:border-gray-600 rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-transparent bg-white dark:bg-gray-700 text-gray-900 dark:text-white text-left flex items-center justify-between"
            aria-haspopup="listbox"
            aria-expanded={open}
          >
            <span className="truncate">{selectedLabel}</span>
            <svg className={`w-4 h-4 ml-2 transition-transform ${open ? 'rotate-180' : ''}`} viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
              <path fillRule="evenodd" d="M5.23 7.21a.75.75 0 011.06.02L10 10.94l3.71-3.71a.75.75 0 111.06 1.06l-4.24 4.24a.75.75 0 01-1.06 0L5.25 8.29a.75.75 0 01-.02-1.08z" clipRule="evenodd" />
            </svg>
          </button>

          {/* Dropdown panel */}
          {open && (
            <div className="relative">
              <div className="absolute z-20 mt-2 w-full bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg shadow-lg">
                {/* Search bar */}
                <div className="p-2 border-b border-gray-200 dark:border-gray-700">
                  <div className="flex items-center gap-2 px-2 py-1.5 rounded-md bg-gray-50 dark:bg-gray-700">
                    <Search className="w-4 h-4 text-gray-500" />
                    <input
                      autoFocus
                      type="text"
                      value={query}
                      onChange={(e) => setQuery(e.target.value)}
                      placeholder="Cari nama tempat, kod atau negeri..."
                      className="w-full bg-transparent outline-none text-sm text-gray-900 dark:text-gray-100 placeholder:text-gray-400"
                    />
                  </div>
                </div>

                {/* Results */}
                <div className="max-h-80 overflow-auto py-1">
                  {query ? (
                    filteredZones.length === 0 ? (
                      <div className="px-4 py-6 text-sm text-gray-500 dark:text-gray-400">Tiada hasil dijumpai.</div>
                    ) : (
                      filteredZones.map((z) => (
                        <button
                          key={z.code}
                          onClick={() => handleSelect(z.code)}
                          className={`w-full text-left px-4 py-2 hover:bg-gray-50 dark:hover:bg-gray-700/60 flex items-center justify-between ${
                            selectedZone === z.code ? 'bg-indigo-50 dark:bg-indigo-900/20' : ''
                          }`}
                          role="option"
                          aria-selected={selectedZone === z.code}
                        >
                          <div>
                            <div className="text-sm text-gray-900 dark:text-gray-100">{z.location}</div>
                            <div className="text-xs text-gray-500 dark:text-gray-400">{z.state} • {z.code}</div>
                          </div>
                          {selectedZone === z.code && <Check className="w-4 h-4 text-indigo-600" />}
                        </button>
                      ))
                    )
                  ) : (
                    zones.map((group) => (
                      <div key={group.state}>
                        <div className="sticky top-0 z-10 bg-white/90 dark:bg-gray-800/90 backdrop-blur px-4 py-1 text-xs font-semibold text-gray-600 dark:text-gray-300 border-b border-gray-100 dark:border-gray-700">
                          {group.state}
                        </div>
                        {group.zones.map((z) => (
                          <button
                            key={z.code}
                            onClick={() => handleSelect(z.code)}
                            className={`w-full text-left px-4 py-2 hover:bg-gray-50 dark:hover:bg-gray-700/60 flex items-center justify-between ${
                              selectedZone === z.code ? 'bg-indigo-50 dark:bg-indigo-900/20' : ''
                            }`}
                            role="option"
                            aria-selected={selectedZone === z.code}
                          >
                            <div>
                              <div className="text-sm text-gray-900 dark:text-gray-100">{z.location}</div>
                              <div className="text-xs text-gray-500 dark:text-gray-400">{group.state} • {z.code}</div>
                            </div>
                            {selectedZone === z.code && <Check className="w-4 h-4 text-indigo-600" />}
                          </button>
                        ))}
                      </div>
                    ))
                  )}
                </div>
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}

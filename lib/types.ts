export interface WaktuSolatData {
  id: number;
  czone: string;
  cbearing: string;
  tarikhMasehi: string;
  tarikhHijrah: string;
  imsak: string;
  subuh: string;
  syuruk: string;
  dhuha: string;
  zohor: string;
  asar: string;
  maghrib: string;
  isyak: string;
  createdAt: Date;
}

export interface ZoneData {
  id: number;
  code: string;
  state: string;
  location: string;
  createdAt: Date;
}

export interface ZoneGroup {
  state: string;
  zones: ZoneOption[];
}

export interface ZoneOption {
  code: string;
  location: string;
}

export interface ScrapedPrayerTime {
  czone: string;
  cbearing: string;
  tarikhMasehi: string;
  tarikhHijrah: string;
  imsak: string;
  subuh: string;
  syuruk: string;
  dhuha: string;
  zohor: string;
  asar: string;
  maghrib: string;
  isyak: string;
}

export interface ScrapeResult {
  success: boolean;
  data?: ScrapedPrayerTime;
  error?: string;
  zone?: string;
}

export interface ParallelScrapeResult {
  successful: number;
  failed: number;
  total: number;
  results: ScrapeResult[];
}

export type PrayerName = 'imsak' | 'subuh' | 'syuruk' | 'dhuha' | 'zohor' | 'asar' | 'maghrib' | 'isyak';

export interface NextPrayer {
  name: PrayerName;
  time: string;
  timeRemaining: string;
}

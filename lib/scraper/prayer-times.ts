import { sleep } from '../utils';
import { ScrapedPrayerTime, ScrapeResult } from '../types';

const SOLAT_API_URL = 'https://www.e-solat.gov.my/index.php?r=esolatApi/takwimsolat&period=today&zone=';
const RETRY_ATTEMPTS = parseInt(process.env.SCRAPE_RETRY_ATTEMPTS || '3');
const TIMEOUT = parseInt(process.env.SCRAPE_TIMEOUT || '30000');

interface ESolatApiResponse {
  prayerTime: Array<{
    hijri: string;
    date: string;
    day: string;
    imsak: string;
    fajr: string;
    syuruk: string;
    dhuha?: string; // Optional as API may not return it
    dhuhr: string;
    asr: string;
    maghrib: string;
    isha: string;
  }>;
  status: string;
  serverTime: string;
  periodType: string;
  lang: string;
  zone: string;
  bearing?: string; // Optional as some zones don't have bearing
}

// Helper functions
function formatTime(time: string): string {
  // Convert HH:MM:SS to HH:MM
  if (time.includes(':')) {
    const parts = time.split(':');
    return `${parts[0]}:${parts[1]}`;
  }
  return time;
}

function decodeHtmlEntities(text: string): string {
  // Decode common HTML entities
  return text
    .replace(/&#176;/g, '°')
    .replace(/&#8242;/g, '′')
    .replace(/&#8243;/g, '″')
    .replace(/&deg;/g, '°')
    .replace(/&prime;/g, '′')
    .replace(/&Prime;/g, '″');
}

function calculateDhuhaTime(syuruk: string): string {
  // Dhuha is approximately 15-20 minutes after Syuruk
  // Simple calculation: add 15 minutes to syuruk
  const parts = syuruk.split(':');
  const hours = parseInt(parts[0]);
  const minutes = parseInt(parts[1]);
  
  const dhuhaMinutes = minutes + 15;
  const dhuhaHours = hours + Math.floor(dhuhaMinutes / 60);
  const finalMinutes = dhuhaMinutes % 60;
  
  return `${dhuhaHours.toString().padStart(2, '0')}:${finalMinutes.toString().padStart(2, '0')}`;
}

export async function scrapePrayerTimes(zone: string): Promise<ScrapeResult> {
  let attempt = 0;
  let lastError: string = '';

  while (attempt < RETRY_ATTEMPTS) {
    try {
      const result = await scrapePrayerTimesOnce(zone);
      
      if (result.success && result.data) {
        return result;
      } else {
        lastError = result.error || 'Unknown error';
      }
    } catch (error) {
      lastError = error instanceof Error ? error.message : 'Unknown error';
    }

    attempt++;
    if (attempt < RETRY_ATTEMPTS) {
      // Exponential backoff
      await sleep(1000 * Math.pow(2, attempt));
    }
  }

  return {
    success: false,
    error: `Failed after ${RETRY_ATTEMPTS} attempts. Last error: ${lastError}`,
    zone,
  };
}

async function scrapePrayerTimesOnce(zone: string): Promise<ScrapeResult> {
  try {
    const url = `${SOLAT_API_URL}${zone.toUpperCase()}`;
    
    const controller = new AbortController();
    const timeoutId = setTimeout(() => controller.abort(), TIMEOUT);

    const response = await fetch(url, {
      signal: controller.signal,
      headers: {
        'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36',
      },
    });

    clearTimeout(timeoutId);

    if (!response.ok) {
      return {
        success: false,
        error: `HTTP error! status: ${response.status}`,
        zone,
      };
    }

    const apiData: ESolatApiResponse = await response.json();

    if (apiData.status !== 'OK!' || !apiData.prayerTime || apiData.prayerTime.length === 0) {
      return {
        success: false,
        error: 'Invalid API response or no prayer time data',
        zone,
      };
    }

    const todayPrayer = apiData.prayerTime[0];
    
    // Decode HTML entities in bearing (if available)
    const bearing = apiData.bearing ? decodeHtmlEntities(apiData.bearing) : '';
    
    // Calculate Dhuha time if not provided by API
    const dhuhaTime = todayPrayer.dhuha 
      ? formatTime(todayPrayer.dhuha) 
      : calculateDhuhaTime(formatTime(todayPrayer.syuruk));
    
    const data: ScrapedPrayerTime = {
      czone: bearing ? `${apiData.zone} - ${bearing}` : apiData.zone,
      cbearing: bearing || apiData.zone,
      tarikhMasehi: todayPrayer.date,
      tarikhHijrah: todayPrayer.hijri,
      imsak: formatTime(todayPrayer.imsak),
      subuh: formatTime(todayPrayer.fajr),
      syuruk: formatTime(todayPrayer.syuruk),
      dhuha: dhuhaTime,
      zohor: formatTime(todayPrayer.dhuhr),
      asar: formatTime(todayPrayer.asr),
      maghrib: formatTime(todayPrayer.maghrib),
      isyak: formatTime(todayPrayer.isha),
    };

    if (!isValidPrayerData(data)) {
      return {
        success: false,
        error: 'Invalid or incomplete prayer time data',
        zone,
      };
    }

    return {
      success: true,
      data,
      zone,
    };
  } catch (error) {
    return {
      success: false,
      error: error instanceof Error ? error.message : 'Unknown error',
      zone,
    };
  }
}

function isValidPrayerData(data: ScrapedPrayerTime): boolean {
  const timePattern = /^\d{2}:\d{2}$/;
  
  return !!(
    data.czone &&
    data.cbearing &&
    data.tarikhMasehi &&
    timePattern.test(data.imsak) &&
    timePattern.test(data.subuh) &&
    timePattern.test(data.syuruk) &&
    timePattern.test(data.dhuha) &&
    timePattern.test(data.zohor) &&
    timePattern.test(data.asar) &&
    timePattern.test(data.maghrib) &&
    timePattern.test(data.isyak)
  );
}

export async function scrapeMultipleZones(
  zones: string[],
  maxConcurrent: number = 3
): Promise<ScrapeResult[]> {
  const results: ScrapeResult[] = [];
  const chunks: string[][] = [];

  // Split zones into chunks
  for (let i = 0; i < zones.length; i += maxConcurrent) {
    chunks.push(zones.slice(i, i + maxConcurrent));
  }

  // Process chunks sequentially, but zones within each chunk in parallel
  for (const chunk of chunks) {
    const chunkResults = await Promise.all(
      chunk.map((zone) => scrapePrayerTimes(zone))
    );
    results.push(...chunkResults);
  }

  return results;
}

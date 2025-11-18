import { Page } from 'puppeteer';
import { createPage, safeClosePage } from './browser';

const ZONES_URL = 'https://www.e-solat.gov.my/index.php';
const TIMEOUT = parseInt(process.env.SCRAPE_TIMEOUT || '30000');

export interface ScrapedZone {
  code: string;
  state: string;
  location: string;
}

export async function scrapeZones(): Promise<ScrapedZone[]> {
  let page: Page | null = null;

  try {
    page = await createPage();

    await page.goto(ZONES_URL, {
      waitUntil: 'networkidle2',
      timeout: TIMEOUT,
    });

    // Wait for the zone select element (note: id is lowercase 'inputzone')
    await page.waitForSelector('select#inputzone', {
      timeout: TIMEOUT,
    });

    // Extract zones grouped by state
    const zones = await page.evaluate(() => {
      const select = document.querySelector('select#inputzone');
      if (!select) return [] as { code: string; state: string; location: string }[];

      const data: { code: string; state: string; location: string }[] = [];

      const groups = Array.from(select.querySelectorAll('optgroup')) as HTMLOptGroupElement[];
      for (const group of groups) {
        const state = (group.getAttribute('label') || '').trim();
        const options = Array.from(group.querySelectorAll('option.hs')) as HTMLOptionElement[];
        for (const opt of options) {
          const code = (opt.value || '').trim();
          const text = (opt.textContent || '').trim();
          if (!code) continue;

          // Remove leading "CODE - " prefix from the option text to get location only
          let location = text;
          const prefix = code + ' - ';
          if (location.startsWith(prefix)) {
            location = location.substring(prefix.length).trim();
          }

          data.push({ code, state, location });
        }
      }

      return data;
    });

    return zones;
  } catch (error) {
    console.error('Error scraping zones:', error);
    throw new Error(
      `Failed to scrape zones: ${error instanceof Error ? error.message : 'Unknown error'}`
    );
  } finally {
    if (page) {
      await safeClosePage(page);
    }
  }
}

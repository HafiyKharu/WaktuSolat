import { NextResponse } from 'next/server';
import { prisma } from '@/lib/db';
import { scrapeZones } from '@/lib/scraper/zones';

export async function POST() {
  try {
    const scrapedZones = await scrapeZones();
    
    if (scrapedZones.length === 0) {
      return NextResponse.json(
        { error: 'No zones scraped' },
        { status: 400 }
      );
    }

    // Delete existing zones and insert new ones
    await prisma.zone.deleteMany({});
    await prisma.zone.createMany({
      data: scrapedZones,
    });

    return NextResponse.json({
      success: true,
      count: scrapedZones.length,
      message: `Successfully scraped and saved ${scrapedZones.length} zones`,
    });
  } catch (error) {
    console.error('Error scraping zones:', error);
    return NextResponse.json(
      { error: 'Failed to scrape zones' },
      { status: 500 }
    );
  }
}

import { NextRequest, NextResponse } from 'next/server';
import { prisma } from '@/lib/db';
import { scrapePrayerTimes } from '@/lib/scraper/prayer-times';
import { format } from 'date-fns';

export async function GET(request: NextRequest) {
  try {
    const searchParams = request.nextUrl.searchParams;
    const zone = searchParams.get('zone') || 'SGR01';
    const today = format(new Date(), 'dd-MMM-yyyy');

    // Try to get from database first
    let prayerTime = await prisma.waktuSolat.findFirst({
      where: {
        czone: {
          contains: zone,
          mode: 'insensitive',
        },
        tarikhMasehi: today,
      },
      orderBy: {
        createdAt: 'desc',
      },
    });

    // If not found, scrape and save
    if (!prayerTime) {
      const scrapeResult = await scrapePrayerTimes(zone);
      
      if (scrapeResult.success && scrapeResult.data) {
        prayerTime = await prisma.waktuSolat.create({
          data: scrapeResult.data,
        });
      } else {
        return NextResponse.json(
          { error: scrapeResult.error || 'Failed to scrape prayer times' },
          { status: 500 }
        );
      }
    }

    return NextResponse.json(prayerTime);
  } catch (error) {
    console.error('Error fetching prayer times:', error);
    return NextResponse.json(
      { error: 'Failed to fetch prayer times' },
      { status: 500 }
    );
  }
}

import { NextRequest, NextResponse } from 'next/server';
import { prisma } from '@/lib/db';
import { scrapeMultipleZones } from '@/lib/scraper/prayer-times';
import { ParallelScrapeResult } from '@/lib/types';

export const maxDuration = 60; // Vercel function timeout

export async function POST(request: NextRequest) {
  try {
    const body = await request.json();
    const { retryFailed = true } = body;
    const maxConcurrent = parseInt(process.env.SCRAPE_MAX_CONCURRENT || '3');

    // Get all zone codes
    const zones = await prisma.zone.findMany({
      select: { code: true },
    });

    if (zones.length === 0) {
      return NextResponse.json(
        { error: 'No zones found. Please scrape zones first.' },
        { status: 400 }
      );
    }

    const zoneCodes = zones.map((z: { code: any; }) => z.code);
    
    // First attempt
    const results = await scrapeMultipleZones(zoneCodes, maxConcurrent);
    
    let successful = 0;
    let failed = 0;
    const failedZones: string[] = [];

    // Save successful results
    for (const result of results) {
      if (result.success && result.data) {
        await prisma.waktuSolat.create({
          data: result.data,
        });
        successful++;
      } else {
        failed++;
        if (result.zone) {
          failedZones.push(result.zone);
        }
      }
    }

    // Retry failed zones if requested
    if (retryFailed && failedZones.length > 0) {
      const retryResults = await scrapeMultipleZones(failedZones, maxConcurrent);
      
      for (const result of retryResults) {
        if (result.success && result.data) {
          await prisma.waktuSolat.create({
            data: result.data,
          });
          successful++;
          failed--;
        }
      }
    }

    const response: ParallelScrapeResult = {
      successful,
      failed,
      total: zones.length,
      results,
    };

    return NextResponse.json(response);
  } catch (error) {
    console.error('Error scraping all zones:', error);
    return NextResponse.json(
      { error: 'Failed to scrape prayer times' },
      { status: 500 }
    );
  }
}

import { NextResponse } from 'next/server';
import { prisma } from '@/lib/db';
import { scrapeZones } from '@/lib/scraper/zones';
import { ZoneGroup } from '@/lib/types';

export async function GET() {
  try {
    // Check if zones exist in database
    const count = await prisma.zone.count();
    
    // If no zones, scrape them first
    if (count === 0) {
      await scrapeAndSaveZones();
    }

    // Get zones grouped by state
    const zones = await prisma.zone.findMany({
      orderBy: [
        { state: 'asc' },
        { location: 'asc' },
      ],
    });

    // Group zones by state
    const grouped: ZoneGroup[] = [];
    const stateMap = new Map<string, ZoneGroup>();

    zones.forEach((zone: { state: string; code: any; location: any; }) => {
      if (!stateMap.has(zone.state)) {
        const group: ZoneGroup = {
          state: zone.state,
          zones: [],
        };
        stateMap.set(zone.state, group);
        grouped.push(group);
      }
      
      stateMap.get(zone.state)!.zones.push({
        code: zone.code,
        location: zone.location,
      });
    });

    return NextResponse.json(grouped);
  } catch (error) {
    console.error('Error fetching zones:', error);
    return NextResponse.json(
      { error: 'Failed to fetch zones' },
      { status: 500 }
    );
  }
}

async function scrapeAndSaveZones() {
  const scrapedZones = await scrapeZones();
  
  if (scrapedZones.length > 0) {
    await prisma.zone.createMany({
      data: scrapedZones,
      skipDuplicates: true,
    });
  }
}

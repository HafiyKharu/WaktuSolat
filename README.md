# Waktu Solat Malaysia - Next.js

A modern web application for Malaysian prayer times, migrated from ASP.NET Core to Next.js.

## Features

- 🕌 Real-time prayer times from e-solat.gov.my
- 🌍 All Malaysian zones supported
- ⏰ Live countdown to next prayer
- 🎨 Modern UI with Tailwind CSS
- 🚀 Fast and responsive
- 📱 Mobile-friendly design
- 🌙 Dark mode support
- 🔄 Automatic data scraping
- 💾 PostgreSQL database

## Tech Stack

- **Framework**: Next.js 14 (App Router)
- **Language**: TypeScript
- **Database**: PostgreSQL with Prisma ORM
- **Scraping**: Puppeteer
- **Styling**: Tailwind CSS
- **Icons**: Lucide React
- **Deployment**: Vercel (recommended) or Docker

## Getting Started

### Prerequisites

- Node.js 18+ and npm/pnpm/yarn
- PostgreSQL 14+

### Installation

1. **Clone and install dependencies**:
```bash
npm install
```

2. **Setup environment variables**:
```bash
cp .env.example .env
```

Edit `.env` and update the `DATABASE_URL`:
```env
DATABASE_URL="postgresql://username:password@localhost:5432/waktusolat?schema=public"
```

3. **Initialize database**:
```bash
npm run db:push
```

4. **Run development server**:
```bash
npm run dev
```

Open [http://localhost:3000](http://localhost:3000)

## Database Schema

The application uses two main tables:

### waktu_solat
- Stores daily prayer times for each zone
- Automatically scraped when needed
- Indexed by zone and date for fast queries

### zones
- Contains all available prayer zones in Malaysia
- Grouped by state
- Auto-populated on first use

## API Routes

### GET /api/prayer-times?zone=SGR01
Get prayer times for a specific zone. Auto-scrapes if not in database.

### GET /api/zones
Get all available zones grouped by state. Auto-scrapes if empty.

### POST /api/scrape/zones
Manually scrape and refresh zone data.

### POST /api/scrape/all
Scrape prayer times for all zones (parallel processing).

## Scripts

```bash
# Development
npm run dev

# Production build
npm run build
npm start

# Database management
npm run db:generate  # Generate Prisma client
npm run db:push      # Push schema to database
npm run db:migrate   # Run migrations
npm run db:studio    # Open Prisma Studio

# Linting
npm run lint
```

## Migration from ASP.NET Core

This project is a complete migration from the original ASP.NET Core MVC application:

### Architecture Changes
- **Backend**: ASP.NET Core → Next.js API Routes
- **ORM**: Dapper → Prisma
- **Scraping**: Selenium → Puppeteer
- **Frontend**: Razor Views → React Components
- **Styling**: Bootstrap 5 → Tailwind CSS

### Key Improvements
- ✅ Better performance with React Server Components
- ✅ Improved DX with TypeScript
- ✅ Modern UI with Tailwind CSS
- ✅ Easier deployment (Vercel, Docker)
- ✅ Better mobile experience
- ✅ Automatic code splitting
- ✅ Built-in API routes

## Deployment

### Vercel (Recommended)

1. Push to GitHub
2. Import to Vercel
3. Set environment variables
4. Deploy

### Docker

```bash
docker build -t waktusolat .
docker run -p 3000:3000 --env-file .env waktusolat
```

### Manual

```bash
npm run build
npm start
```

## Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `DATABASE_URL` | PostgreSQL connection string | Required |
| `NODE_ENV` | Environment mode | `development` |
| `NEXT_PUBLIC_APP_URL` | Application URL | `http://localhost:3000` |
| `SCRAPE_RETRY_ATTEMPTS` | Retry attempts for scraping | `3` |
| `SCRAPE_MAX_CONCURRENT` | Max concurrent scrapes | `3` |
| `SCRAPE_TIMEOUT` | Scraping timeout (ms) | `30000` |

## License

MIT

## Credits

Data source: [e-solat.gov.my](https://www.e-solat.gov.my)

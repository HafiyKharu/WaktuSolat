/** @type {import('next').NextConfig} */
const nextConfig = {
  experimental: {
    serverActions: {
      bodySizeLimit: '2mb',
    },
  },
  webpack: (config) => {
    config.externals.push({
      'puppeteer': 'commonjs puppeteer',
    });
    return config;
  },
};

export default nextConfig;

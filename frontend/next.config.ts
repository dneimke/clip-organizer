import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  /* config options here */
  images: {
    remotePatterns: [
      {
        protocol: 'http',
        hostname: 'localhost',
        port: '5059',
        pathname: '/api/**',
      },
      {
        protocol: 'http',
        hostname: '127.0.0.1',
        port: '5059',
        pathname: '/api/**',
      },
      // Allow HTTPS redirects from backend in development
      {
        protocol: 'https',
        hostname: 'localhost',
        port: '7059',
        pathname: '/api/**',
      },
      {
        protocol: 'https',
        hostname: '127.0.0.1',
        port: '7059',
        pathname: '/api/**',
      },
    ],
    domains: [
      // YouTube thumbnail hosts
      'i.ytimg.com',
      'i1.ytimg.com',
      'i2.ytimg.com',
      'i3.ytimg.com',
      'img.youtube.com',
    ],
  },
  async rewrites() {
    return [
      { source: '/collections', destination: '/plans' },
      { source: '/collections/:id', destination: '/plans/:id' },
    ];
  },
};

export default nextConfig;

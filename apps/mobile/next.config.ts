import type { NextConfig } from 'next';

const nextConfig: NextConfig = {
    // Static export so Capacitor can wrap the built assets natively.
    output: 'export',
    images: {
        unoptimized: true,
    },
    trailingSlash: true,
};

export default nextConfig;

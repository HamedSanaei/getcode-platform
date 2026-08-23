import type { NextConfig } from "next";

const internalApi = process.env.INTERNAL_API_URL ?? "http://localhost:8080";

const nextConfig: NextConfig = {
  output: "standalone",
  poweredByHeader: false,
  reactStrictMode: true,
  async rewrites() {
    return [
      {
        source: "/api/:path*",
        destination: `${internalApi}/:path*`,
      },
    ];
  },
};

export default nextConfig;

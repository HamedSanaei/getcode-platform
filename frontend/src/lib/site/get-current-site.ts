import { headers } from "next/headers";
import { resolveSiteConfig } from "./site-config";

export async function getCurrentSite() {
  const requestHeaders = await headers();
  const host = requestHeaders.get("host") ?? "getcode.example";
  return resolveSiteConfig(host);
}

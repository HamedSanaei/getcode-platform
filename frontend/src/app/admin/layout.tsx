import type { Metadata } from 'next';
import { AdminGuard } from '@/components/admin/AdminShell';

export const metadata: Metadata = {
  title: 'Admin',
  // Admin surfaces stay out of indexes on every host.
  robots: { index: false, follow: false },
};

/**
 * M09-001 admin shell layout. The guard is a UX layer (loading / anonymous /
 * permission-denied states); the authoritative check is the server-side
 * `admin.access` policy enforced by every /api/admin/* route.
 */
export default function AdminLayout({ children }: { children: React.ReactNode }) {
  return <AdminGuard>{children}</AdminGuard>;
}

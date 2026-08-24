'use client';

/**
 * M09-001: principal context for the SPA.
 *
 * SECURITY NOTE: this data shapes navigation and copy only. Every privileged
 * API enforces authorization server-side via the permission policy; hiding or
 * showing UI is never a control. Shape mirrors GET /api/auth/principal
 * ({userId, roles[], permissions[]}) — stable canonical strings, no hard-coded
 * role name anywhere in components.
 */

export interface Principal {
  userId: string;
  roles: string[];
  permissions: string[];
}

export type PrincipalState =
  | { kind: 'loading' }
  | { kind: 'anonymous' }
  | { kind: 'authenticated'; principal: Principal };

export async function loadPrincipal(): Promise<PrincipalState> {
  try {
    const response = await fetch('/api/auth/principal', { headers: { accept: 'application/json' } });
    if (response.status === 401) {
      return { kind: 'anonymous' };
    }
    if (!response.ok) {
      throw new Error(`principal read failed: ${response.status}`);
    }
    const body = (await response.json()) as Principal;
    return { kind: 'authenticated', principal: body };
  } catch {
    // Network/5xx failures must never grant an admin view by accident.
    return { kind: 'anonymous' };
  }
}

import type { AnchorHTMLAttributes, ReactNode } from 'react';

/**
 * GetCode / Sidebar Item Variants — Penpot axis: State
 * (board 324404a7-ad1e-8048-8008-8774233749fc).
 * Active state is expressed with aria-current="page"; styling only.
 */
export interface SidebarItemProps extends Omit<AnchorHTMLAttributes<HTMLAnchorElement>, 'href' | 'aria-current'> {
  href: string;
  active?: boolean;
  icon?: ReactNode;
  children: ReactNode;
}

export function SidebarItem({ href, active = false, icon, children, ...rest }: SidebarItemProps) {
  return (
    <a
      href={href}
      className="gc-sidebar-item"
      aria-current={active ? 'page' : undefined}
      {...rest}
    >
      {icon}
      <span>{children}</span>
    </a>
  );
}

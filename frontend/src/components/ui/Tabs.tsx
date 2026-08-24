'use client';

import { useId, useRef, useState } from 'react';
import type { ReactNode } from 'react';

/**
 * GetCode / Tab Variants — Penpot axis: State
 * (board 324404a7-ad1e-8048-8008-877415456507).
 * WAI-ARIA tabs pattern: roving tabindex; Arrow/Home/End navigation with
 * arrow semantics that follow the host text direction (APG requirement).
 */
export interface TabItem {
  id: string;
  label: ReactNode;
  content: ReactNode;
}

export interface TabsProps {
  items: TabItem[];
  /** Accessible name for the tab list. */
  listLabel: string;
}

export function Tabs({ items, listLabel }: TabsProps) {
  const baseId = useId();
  const [activeId, setActiveId] = useState(items[0]?.id);
  const listRef = useRef<HTMLDivElement>(null);

  const focusTab = (itemId: string) => {
    setActiveId(itemId);
    const tabEl = listRef.current?.querySelector<HTMLButtonElement>(`#${CSS.escape(`${baseId}-tab-${itemId}`)}`);
    tabEl?.focus();
  };

  const step = (fromIndex: number, delta: number) => {
    const count = items.length;
    if (count === 0) return items[0]?.id;
    return items[(fromIndex + delta + count) % count].id;
  };

  const onKeyDown = (event: React.KeyboardEvent) => {
    const currentIndex = Math.max(
      0,
      items.findIndex((item) => item.id === activeId),
    );
    // Horizontal arrows must mirror in right-to-left hosts (WAI-ARIA APG).
    let nextId: string | undefined;
    if (event.key === 'ArrowRight') {
      const rtl = listRef.current ? getComputedStyle(listRef.current).direction === 'rtl' : false;
      event.preventDefault();
      nextId = step(currentIndex, rtl ? -1 : 1);
    } else if (event.key === 'ArrowLeft') {
      const rtl = listRef.current ? getComputedStyle(listRef.current).direction === 'rtl' : false;
      event.preventDefault();
      nextId = step(currentIndex, rtl ? 1 : -1);
    } else if (event.key === 'Home') {
      event.preventDefault();
      nextId = items[0]?.id;
    } else if (event.key === 'End') {
      event.preventDefault();
      nextId = items[items.length - 1]?.id;
    }

    if (nextId) {
      focusTab(nextId);
    }
  };

  const activeItem = items.find((item) => item.id === activeId) ?? items[0];

  return (
    <div>
      <div className="gc-tabs__list" role="tablist" aria-label={listLabel} ref={listRef} onKeyDown={onKeyDown}>
        {items.map((item) => {
          const selected = item.id === activeItem?.id;
          return (
            <button
              key={item.id}
              id={`${baseId}-tab-${item.id}`}
              type="button"
              role="tab"
              className="gc-tab"
              aria-selected={selected}
              aria-controls={`${baseId}-panel-${item.id}`}
              tabIndex={selected ? 0 : -1}
              onClick={() => setActiveId(item.id)}
            >
              {item.label}
            </button>
          );
        })}
      </div>
      {items.map((item) => (
        <div
          key={item.id}
          id={`${baseId}-panel-${item.id}`}
          role="tabpanel"
          aria-labelledby={`${baseId}-tab-${item.id}`}
          hidden={item.id !== activeItem?.id}
          tabIndex={0}
        >
          {item.content}
        </div>
      ))}
    </div>
  );
}

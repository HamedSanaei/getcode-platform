import './ui.css';
import type { ButtonHTMLAttributes, ReactNode } from 'react';

/**
 * GetCode / Button Variants — Penpot axes: Style × State × Size
 * (board 324404a7-ad1e-8048-8008-87746e33fea2).
 * State is native (hover/focus-visible/disabled); no business rules live here.
 */
export type ButtonStyle = 'primary' | 'accent' | 'secondary' | 'ghost';
export type ButtonSize = 'sm' | 'md' | 'lg';

export interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  buttonStyle?: ButtonStyle;
  size?: ButtonSize;
  children: ReactNode;
}

const STYLE_CLASS: Record<ButtonStyle, string> = {
  primary: 'gc-button--primary',
  accent: 'gc-button--accent',
  secondary: 'gc-button--secondary',
  ghost: 'gc-button--ghost',
};

const SIZE_CLASS: Record<ButtonSize, string> = {
  sm: 'gc-button--sm',
  md: 'gc-button--md',
  lg: 'gc-button--lg',
};

export function Button({ buttonStyle = 'primary', size = 'md', children, className, type = 'button', ...rest }: ButtonProps) {
  const classes = ['gc-button', STYLE_CLASS[buttonStyle], SIZE_CLASS[size], className].filter(Boolean).join(' ');
  return (
    <button type={type} className={classes} {...rest}>
      {children}
    </button>
  );
}

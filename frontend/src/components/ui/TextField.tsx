import { useId } from 'react';
import type { InputHTMLAttributes, ReactNode } from 'react';

/**
 * GetCode / Field Variants — Penpot axes: State × Type
 * (board 324404a7-ad1e-8048-8008-877413864530).
 * Accessible label/error wiring is built in; no validation rules are embedded.
 */
export interface TextFieldProps extends Omit<InputHTMLAttributes<HTMLInputElement>, 'id' | 'className'> {
  label: string;
  /** Shown under the input; wires aria-describedby + aria-invalid when set. */
  error?: string;
  hint?: ReactNode;
  hideLabel?: boolean;
}

export function TextField({ label, error, hint, hideLabel = false, required, ...rest }: TextFieldProps) {
  const inputId = useId();
  const errorId = `${inputId}-error`;
  const hintId = `${inputId}-hint`;
  const describedBy = [error ? errorId : null, hint ? hintId : null].filter(Boolean).join(' ') || undefined;

  return (
    <div className="gc-field">
      <label htmlFor={inputId} className={hideLabel ? 'gc-field__label gc-visually-hidden' : 'gc-field__label'}>
        {label}
        {required ? <span aria-hidden="true"> *</span> : null}
      </label>
      <input
        id={inputId}
        className="gc-field__input"
        aria-invalid={error ? true : undefined}
        aria-describedby={describedBy}
        required={required}
        {...rest}
      />
      {hint ? (
        <span id={hintId} className="gc-field__hint">
          {hint}
        </span>
      ) : null}
      {error ? (
        <span id={errorId} className="gc-field__error" role="alert">
          {error}
        </span>
      ) : null}
    </div>
  );
}

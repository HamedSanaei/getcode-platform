import { describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { axe } from 'jest-axe';
import { TextField } from '../../src/components/ui/TextField';

describe('TextField (GetCode / Field Variants)', () => {
  it('associates label and input and accepts typed values', async () => {
    render(<TextField label="Phone number" placeholder="+98…" />);
    const input = screen.getByLabelText('Phone number');
    await userEvent.type(input, '0912');
    expect(input).toHaveValue('0912');
  });

  it('wires error state for assistive tech', () => {
    render(<TextField label="Email" type="email" error="Enter a valid email address" />);
    const input = screen.getByLabelText(/Email/);
    expect(input).toBeInvalid();

    const error = screen.getByRole('alert');
    expect(error).toHaveTextContent('Enter a valid email address');
    expect(input.getAttribute('aria-describedby')).toBe(error.id);
  });

  it('supports required and disabled states', async () => {
    const onChange = vi.fn();
    render(
      <div>
        <TextField label="Required field" required />
        <TextField label="Locked field" disabled onChange={onChange} defaultValue="fixed" />
      </div>,
    );
    expect(screen.getByLabelText(/Required field/)).toBeRequired();
    const locked = screen.getByLabelText(/Locked field/);
    expect(locked).toBeDisabled();
    await userEvent.type(locked, 'x');
    expect(onChange).not.toHaveBeenCalled();
  });

  it('has no accessibility violations in default and error states', async () => {
    const { container } = render(
      <div>
        <TextField label="Name" hint="As printed on your ID" />
        <TextField label="Email" type="email" error="Required" />
        <TextField label="Hidden label" hideLabel />
      </div>,
    );
    expect(await axe(container)).toHaveNoViolations();
  });
});

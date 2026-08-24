import { describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { axe } from 'jest-axe';
import { Button } from '../../src/components/ui/Button';

describe('Button (GetCode / Button Variants)', () => {
  it('fires click handlers and is keyboard operable (Enter/Space are native)', async () => {
    const onClick = vi.fn();
    render(<Button onClick={onClick}>Buy now</Button>);
    const button = screen.getByRole('button', { name: 'Buy now' });

    await userEvent.click(button);
    expect(onClick).toHaveBeenCalledOnce();

    button.focus();
    await userEvent.keyboard('{Enter}');
    await userEvent.keyboard(' ');
    expect(onClick).toHaveBeenCalledTimes(3);
  });

  it('disabled buttons block interaction but stay discoverable', async () => {
    const onClick = vi.fn();
    render(<Button disabled onClick={onClick}>Unavailable</Button>);
    const button = screen.getByRole('button', { name: 'Unavailable' });
    expect(button).toBeDisabled();

    await userEvent.click(button);
    expect(onClick).not.toHaveBeenCalled();
  });

  it('renders the documented variant axes as classes for visual-regression hooks', () => {
    render(
      <Button buttonStyle="accent" size="lg" data-testid="btn">
        Accent
      </Button>,
    );
    const button = screen.getByTestId('btn');
    expect(button.className).toContain('gc-button--accent');
    expect(button.className).toContain('gc-button--lg');
  });

  it('has no accessibility violations across styles and states', async () => {
    const { container } = render(
      <div>
        <Button>Primary</Button>
        <Button buttonStyle="ghost">Ghost</Button>
        <Button buttonStyle="secondary" size="sm">
          Secondary
        </Button>
        <Button disabled>Disabled</Button>
      </div>,
    );
    expect(await axe(container)).toHaveNoViolations();
  });
});

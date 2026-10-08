import { cleanup, fireEvent, render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import PasswordGenerator from '../../src/ArcaneVault.Client/src/components/PasswordGenerator';
import { mockClipboard } from './TestDoubles/mockClipboard';
import { mockDeterministicCrypto } from './TestDoubles/mockDeterministicCrypto';

describe('PasswordGenerator', () => {
  const originalClipboard = Object.getOwnPropertyDescriptor(window.navigator, 'clipboard');

  beforeEach(mockDeterministicCrypto);

  afterEach(() => {
    cleanup();
    if (originalClipboard) {
      Object.defineProperty(window.navigator, 'clipboard', originalClipboard);
    } else {
      Reflect.deleteProperty(window.navigator, 'clipboard');
    }
  });

  it('updates the generated password when the selected length changes', () => {
    render(<PasswordGenerator />);

    const output = screen.getByLabelText('Generated password');
    const lengthSlider = screen.getByRole('slider', { name: 'Password length' });

    expect(output.textContent).toHaveLength(20);

    fireEvent.change(lengthSlider, { target: { value: '12' } });

    expect(screen.getByText('12 characters')).toBeInTheDocument();
    expect(output.textContent).toHaveLength(12);
  });

  it('keeps one character type selected and generates only that type', async () => {
    const user = userEvent.setup();
    render(<PasswordGenerator />);

    const lowercase = screen.getByRole('checkbox', { name: 'Lowercase letters' });
    await user.click(screen.getByRole('checkbox', { name: 'Uppercase letters' }));
    await user.click(screen.getByRole('checkbox', { name: 'Numbers' }));
    await user.click(screen.getByRole('checkbox', { name: 'Symbols' }));

    expect(lowercase).toBeChecked();
    expect(screen.getByLabelText('Generated password').textContent).toMatch(/^[a-z]{20}$/);

    await user.click(lowercase);

    expect(lowercase).toBeChecked();
    expect(screen.getByLabelText('Generated password').textContent).toMatch(/^[a-z]{20}$/);
  });

  it('includes a character type when it is selected again', async () => {
    const user = userEvent.setup();
    render(<PasswordGenerator />);
    const uppercase = screen.getByRole('checkbox', { name: 'Uppercase letters' });

    await user.click(uppercase);
    expect(uppercase).not.toBeChecked();

    await user.click(uppercase);

    expect(uppercase).toBeChecked();
    expect(screen.getByLabelText('Generated password').textContent).toMatch(/[A-Z]/);
  });

  it('removes ambiguous characters when the option is enabled', async () => {
    mockDeterministicCrypto(13);
    const user = userEvent.setup();
    render(<PasswordGenerator />);

    const output = screen.getByLabelText('Generated password');
    expect(output.textContent).toContain('o');

    await user.click(screen.getByRole('checkbox', { name: 'Exclude ambiguous characters' }));

    expect(output.textContent).not.toMatch(/[Il1O0o]/);
  });

  it('retries cryptographic values outside the unbiased range', () => {
    mockDeterministicCrypto([0xFFFFFFFF, 0]);
    render(<PasswordGenerator />);

    expect(screen.getByLabelText('Generated password').textContent).toHaveLength(20);
  });

  it('updates strength feedback for different password recipes', async () => {
    const user = userEvent.setup();
    render(<PasswordGenerator />);

    expect(screen.getByRole('img', { name: 'Password strength: Excellent' })).toBeInTheDocument();
    await user.click(screen.getByRole('checkbox', { name: 'Uppercase letters' }));
    await user.click(screen.getByRole('checkbox', { name: 'Numbers' }));
    await user.click(screen.getByRole('checkbox', { name: 'Symbols' }));
    expect(screen.getByRole('img', { name: 'Password strength: Strong' })).toBeInTheDocument();

    fireEvent.change(screen.getByRole('slider', { name: 'Password length' }), {
      target: { value: '12' },
    });
    expect(screen.getByRole('img', { name: 'Password strength: Fair' })).toBeInTheDocument();

    await user.click(screen.getByRole('checkbox', { name: 'Numbers' }));
    await user.click(screen.getByRole('checkbox', { name: 'Lowercase letters' }));
    expect(screen.getByRole('img', { name: 'Password strength: Weak' })).toBeInTheDocument();
  });

  it('reports successful clipboard copies', async () => {
    const user = userEvent.setup();
    const writeText = mockClipboard(async () => {});
    render(<PasswordGenerator />);
    const password = screen.getByLabelText('Generated password').textContent;

    await user.click(screen.getByRole('button', { name: 'Copy password' }));

    expect(writeText).toHaveBeenCalledWith(password);
    expect(await screen.findByText('Password copied to clipboard')).toBeInTheDocument();
  });

  it('reports when the clipboard is unavailable', async () => {
    const user = userEvent.setup();
    mockClipboard(async () => {
      throw new Error('Clipboard permission denied.');
    });
    render(<PasswordGenerator />);

    await user.click(screen.getByRole('button', { name: 'Copy password' }));

    expect(await screen.findByText('Copy unavailable. Select the password to copy it.')).toBeInTheDocument();
  });

  it('reports notification status when notifications are opened', async () => {
    const user = userEvent.setup();
    render(<PasswordGenerator />);

    await user.click(screen.getByRole('button', { name: 'Notifications' }));

    expect(screen.getByText('You are all caught up.')).toBeInTheDocument();
  });
});

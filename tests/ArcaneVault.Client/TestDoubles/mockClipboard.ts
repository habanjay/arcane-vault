import { vi } from 'vitest';

export function mockClipboard(writeText: (text: string) => Promise<void>) {
  const writeTextMock = vi.fn(writeText);
  Object.defineProperty(window.navigator, 'clipboard', {
    configurable: true,
    value: { writeText: writeTextMock },
  });
  return writeTextMock;
}

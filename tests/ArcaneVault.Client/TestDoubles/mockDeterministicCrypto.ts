import { vi } from 'vitest';

export function mockDeterministicCrypto(values: number | number[] = 0): void {
  const sequence = Array.isArray(values) ? values : [values];
  let sequenceIndex = 0;

  vi.spyOn(globalThis.crypto, 'getRandomValues').mockImplementation((values) => {
    if (values instanceof Uint32Array) {
      const value = sequence[Math.min(sequenceIndex, sequence.length - 1)] ?? 0;
      sequenceIndex += 1;
      values.fill(value);
    }

    return values;
  });
}

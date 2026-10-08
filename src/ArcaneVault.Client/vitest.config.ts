import { fileURLToPath } from 'node:url';
import { defineConfig, mergeConfig } from 'vitest/config';
import viteConfig from './vite.config';

const uiTestDirectory = fileURLToPath(new URL('../../tests/ArcaneVault.Client', import.meta.url));
export default mergeConfig(
  viteConfig,
  defineConfig({
    test: {
      environment: 'jsdom',
      include: [`${uiTestDirectory.replaceAll('\\', '/')}/**/*.test.{ts,tsx}`],
      setupFiles: [`${uiTestDirectory}/setup.ts`],
      restoreMocks: true,
      coverage: {
        include: ['src/components/PasswordGenerator.tsx'],
        thresholds: {
          branches: 80,
          functions: 80,
          lines: 80,
          statements: 80,
        },
      },
    },
    server: {
      fs: {
        allow: [uiTestDirectory],
      },
    },
    resolve: {
      alias: [
        {
          find: 'react',
          replacement: fileURLToPath(new URL('./node_modules/react', import.meta.url)),
        },
        {
          find: 'react-dom',
          replacement: fileURLToPath(new URL('./node_modules/react-dom', import.meta.url)),
        },
        {
          find: '@testing-library/react',
          replacement: fileURLToPath(import.meta.resolve('@testing-library/react')),
        },
        {
          find: '@testing-library/user-event',
          replacement: fileURLToPath(import.meta.resolve('@testing-library/user-event')),
        },
        {
          find: '@testing-library/jest-dom/vitest',
          replacement: fileURLToPath(import.meta.resolve('@testing-library/jest-dom/vitest')),
        },
        {
          find: 'vitest',
          replacement: fileURLToPath(import.meta.resolve('vitest')),
        },
      ],
    },
  }),
);

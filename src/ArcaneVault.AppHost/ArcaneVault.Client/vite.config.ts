import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import tailwindcss from '@tailwindcss/vite';

const apiTarget =
  process.env['services__arcaneVault-server__https__0'] ??
  process.env['services__arcaneVault-server__http__0'] ??
  process.env.SERVER_HTTPS ??
  process.env.SERVER_HTTP ??
  'http://localhost:5400';

// https://vite.dev/config/
export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    proxy: {
      // Proxy API calls to the app service
      '/api': {
        target: apiTarget,
        changeOrigin: true
      }
    }
  }
});

import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import path from 'path';

export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: {
      '@': path.resolve(import.meta.dirname, './src'),
    },
  },
  server: {
    port: 5173,
    proxy: {
      '/api/Auth': {
        target: 'http://127.0.0.1:5168', // User Service (local .NET CLI port)
        changeOrigin: true,
      },
      '/api': {
        target: 'http://127.0.0.1:5167', // Stream Service (local .NET CLI port)
        changeOrigin: true,
      },
    },
  },
});

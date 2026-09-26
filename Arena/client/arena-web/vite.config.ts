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
      '/ws/chat': {
        target: 'http://127.0.0.1:5169', // Chat Service WebSocket
        ws: true,
        changeOrigin: true,
      },
      '/api/chat': {
        target: 'http://127.0.0.1:5169', // Chat Service REST (moderation)
        changeOrigin: true,
      },
      '/api/Auth': {
        target: 'http://127.0.0.1:5168', // User Service
        changeOrigin: true,
      },
      '/api/tournaments': {
        target: 'http://127.0.0.1:8082', // Tournament Service
        changeOrigin: true,
      },
      '/api/teams': {
        target: 'http://127.0.0.1:8082', // Tournament Service
        changeOrigin: true,
      },
      '/uploads': {
        target: 'http://127.0.0.1:8082', // Tournament Service Static Uploads
        changeOrigin: true,
      },
      '/api': {
        target: 'http://127.0.0.1:5167', // Stream Service
        changeOrigin: true,
      },
    },
  },
});

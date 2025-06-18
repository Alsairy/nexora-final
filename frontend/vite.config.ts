import react from '@vitejs/plugin-react';
import path from 'path';
import { defineConfig, splitVendorChunkPlugin } from 'vite';

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [
    react(),
    splitVendorChunkPlugin(), // Split vendor chunks for better caching
  ],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
      '@components': path.resolve(__dirname, './src/components'),
      '@hooks': path.resolve(__dirname, './src/hooks'),
      '@utils': path.resolve(__dirname, './src/utils'),
      '@routes': path.resolve(__dirname, './src/routes'),
      '@contexts': path.resolve(__dirname, './src/contexts'),
      '@assets': path.resolve(__dirname, './src/assets'),
    },
  },
  build: {
    rollupOptions: {
      output: {
        manualChunks: {
          // Split vendor chunks to keep them under 300KB
          'vendor-react': ['react', 'react-dom', 'react-router-dom'],
          'vendor-mui': [
            '@mui/material',
            '@mui/icons-material',
            '@mui/system',
            '@mui/styles',
            '@emotion/react',
            '@emotion/styled',
          ],
          'vendor-charts': ['chart.js', 'react-chartjs-2'],
          'vendor-utils': ['axios', 'date-fns', 'lodash'],
        },
      },
    },
    chunkSizeWarningLimit: 300, // 300KB warning limit
    sourcemap: true,
    minify: 'terser',
    terserOptions: {
      compress: {
        drop_console: true,
        drop_debugger: true,
      },
    },
  },
  server: {
    port: 3000,
    strictPort: true,
    host: true,
    cors: true,
  },
  preview: {
    port: 3000,
    strictPort: true,
    host: true,
    cors: true,
  },
});


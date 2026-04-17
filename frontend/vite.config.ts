import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import mkcert from 'vite-plugin-mkcert'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react(), mkcert()],
  server: {
    host: 'localhost',
    port: 5173,
    strictPort: true,
  },
  build: {
    rollupOptions: {
      output: {
        manualChunks: {
          'vendor-react': ['react', 'react-dom', 'react-router-dom'],
          'vendor-mui': ['@mui/material', '@emotion/react', '@emotion/styled'],
          'vendor-query': ['@tanstack/react-query'],
        },
      },
    },
  },
  // Vitest merges `test` into Vite config at runtime; Vite's types omit it.
  // @ts-expect-error TS2769 — `test` is not on Vite's UserConfigExport
  test: {
    globals: true,
    environment: 'jsdom',
    setupFiles: './src/test/setup.ts',
    css: true,
    restoreMocks: true,
    clearMocks: true,
    exclude: ['tests/e2e/**', 'node_modules/**', 'dist/**'],
  },
})

import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// In development the app runs on its own port and proxies /api to the running
// API, so there is no CORS to configure. In production the build lands in the
// API's wwwroot and both are served from the same origin.
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/api': {
        target: process.env.VITE_API_URL ?? 'http://localhost:5163',
        changeOrigin: true,
      },
    },
  },
  build: {
    outDir: 'dist',
    sourcemap: true,
  },
})

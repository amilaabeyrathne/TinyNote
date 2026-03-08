import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig({
  server: {
    port: 5173,
    proxy: {
      '/api': {
        // Dev-only: used when running "npm run dev". Not used in Docker/ECS - nginx and ALB handle /api there.
        target: 'http://localhost:8082',  // Docker API port; use 5072 if running API with dotnet run
        changeOrigin: true,
        secure: false,
      },
    },
  },
  plugins: [react()],
})

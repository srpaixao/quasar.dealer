import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import path from 'path'

// https://vite.dev/config/
export default defineConfig({
  plugins: [vue()],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
  build: {
    rollupOptions: {
      output: {
        manualChunks(id) {
          if (id.includes('node_modules')) {
            if (id.includes('vuetify')) {
              return 'vendor_vuetify'
            }
            if (id.includes('vue-router') || id.includes('pinia') || id.includes('axios') || id.includes('vue')) {
              return 'vendor_vue'
            }
            return 'vendor'
          }
        },
      },
    },
  },
})

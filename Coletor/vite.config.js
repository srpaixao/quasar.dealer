import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import path from 'path'

export default defineConfig({
  plugins: [vue()],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
  build: {
    chunkSizeWarningLimit: 700,
    rollupOptions: {
      output: {
        manualChunks(id) {
          if (!id.includes('node_modules')) {
            return undefined
          }

          if (id.includes('vuetify')) {
            return 'vuetify'
          }

          if (id.includes('vue-router')) {
            return 'vue-router'
          }

          if (id.includes('pinia')) {
            return 'pinia'
          }

          if (id.includes('axios')) {
            return 'axios'
          }

          return 'vendor'
        },
      },
    },
  },
})

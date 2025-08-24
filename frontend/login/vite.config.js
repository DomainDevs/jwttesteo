import { fileURLToPath, URL } from 'node:url'

import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import vueDevTools from 'vite-plugin-vue-devtools'
import mkcert from 'vite-plugin-mkcert';

// https://vite.dev/config/
export default defineConfig({
  plugins: [ vue(), vueDevTools(), mkcert() ],
  server: {
    port: 5173,      // 🔹 puerto fijo
    strictPort: true, // ❌ si está ocupado, falla en vez de usar otro
    https: true
  },
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url))
    },
  },
})

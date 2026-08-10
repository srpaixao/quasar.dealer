<script setup>
import { useAuthStore } from '@/stores/authStore.js';
import pkg from '../../package.json';

const authStore = useAuthStore();
const version = pkg.version;
</script>

<template>
  <v-footer app color="grey-lighten-4" class="px-2 py-0 footer-container" elevation="2" style="z-index: 1001; bottom: 0;">
    <div class="footer-content">
      <div class="user-info">
        <v-icon size="x-small" color="primary">mdi-account</v-icon>
        <span>{{ authStore.user.fullName || 'Usuário' }}</span>
      </div>
      
      <div class="branch-info">
        <v-icon size="x-small" color="primary">mdi-office-building</v-icon>
        <span>{{ authStore.user.filialName || 'Filial' }}</span>
      </div>

      <div class="version-info">
        <span>v{{ version }}</span>
      </div>
    </div>
  </v-footer>
</template>

<style scoped>
.footer-container {
  max-height: 28px;
  min-height: 28px;
  border-top: 1px solid #e0e0e0;
}

.footer-content {
  display: flex;
  justify-content: space-between;
  align-items: center;
  width: 100%;
  font-size: 0.7rem;
  font-weight: 500;
  color: #555;
}

.user-info, .branch-info, .version-info {
  display: flex;
  align-items: center;
  gap: 4px;
}

.version-info {
  font-style: italic;
  color: #888;
}
</style>

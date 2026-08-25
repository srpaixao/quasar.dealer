<script setup>
import { APP_VERSION } from '@/config/version.js';

let ambiente = '';
if (import.meta.env.MODE != 'production') {
  ambiente = 'Dev'
}
</script>
<template>
  <v-app-bar color="primary" density="compact">
    <v-app-bar-title>
      <div class="title-container">
        <span>Quasar Dealer</span>
        <span class="system-info">
          <small v-if="ambiente" class="ambiente">{{ ambiente }}</small>
          <small class="version">Versão {{ APP_VERSION }}</small>
        </span>
      </div>
    </v-app-bar-title>
  </v-app-bar>
</template>

<style scoped>
.title-container {
  display: flex;
  justify-content: space-between;
  width: 100%;
}

.system-info {
  margin-left: auto;
  margin-right: 20px;
  display: flex;
  align-items: center;
  gap: 10px;
}

.title-container small {
  white-space: nowrap;
}

.title-container small.ambiente {
  font-size: 80%;
  font-style: italic;
  color: yellow;
}

.title-container small.version {
  font-size: 65%;
  font-weight: 400;
  opacity: 0.85;
}
</style>

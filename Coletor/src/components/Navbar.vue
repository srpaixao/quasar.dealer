<script setup>
import { APP_VERSION } from '@/config/version.js';

let ambiente = '';
if (import.meta.env.MODE != 'production') {
  ambiente = 'Dev'
}
const logoUrl = `${import.meta.env.BASE_URL}quasar-dealer-logo.svg`;
</script>
<template>
  <v-app-bar class="collector-appbar" density="comfortable" flat>
    <img class="collector-appbar-logo" :src="logoUrl" alt="Quasar Dealer" />
    <v-app-bar-title>
      <div class="title-container">
        <span class="collector-appbar-title">Coletor</span>
        <span class="system-info">
          <small v-if="ambiente" class="ambiente">{{ ambiente }}</small>
          <small class="online">● ONLINE</small>
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

.system-info small.online {
  color: #27d3a2;
  font-size: 65%;
  font-weight: 800;
}
</style>

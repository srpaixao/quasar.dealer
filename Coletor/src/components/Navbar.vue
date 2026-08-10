<script setup>
let ambiente = '';
if (import.meta.env.MODE != 'production') {
  ambiente = 'Dev'
}
</script>
<template>
  <v-app-bar color="primary" density="compact">
    <v-app-bar-title tabindex="-1">
      <div class="title-container">
        Quasar Dealer Nova Chevrolet <small>{{ ambiente }}</small>
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

.title-container small {
  margin-left: auto;
  margin-right: 20px;
  font-size: 80%;
  font-style: italic;
  color: yellow;
}
</style>
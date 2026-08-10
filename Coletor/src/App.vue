<script setup>
import Navbar from './components/Navbar.vue';
import BottomBar from './components/BottomBar.vue'

import { useRoute } from 'vue-router';

const route = useRoute();

</script>

<template>
  <v-app>
    <Navbar v-if="route.meta.showNavbar" />

    <BottomBar v-if="route.name !== 'Login'" />

    <v-main>
      <router-view />
    </v-main>

  </v-app>
</template>

<style scoped></style>

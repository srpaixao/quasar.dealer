<script setup>
import Navbar from './components/Navbar.vue';
// import BottomBar from './components/BottomBar.vue'

import { useRoute } from 'vue-router';

const route = useRoute();

</script>

<template>
  <v-app class="collector-app">
    <Navbar v-if="route.meta.showNavbar" />

    <v-main>
      <router-view />
    </v-main>

    <!-- <BottomBar v-if="route.meta.showBottomBar" /> -->
  </v-app>
</template>

<style scoped></style>

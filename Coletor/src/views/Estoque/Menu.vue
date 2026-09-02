<script setup>
import { ref } from 'vue';
import { logout } from '@/router';

const confirmationdialog = ref(false);

function confirmLogout() {
  logout();
}
</script>

<template>
  <v-container class="collector-home collector-submenu text-center">
    <v-row dense>
      <v-col cols="12">
        <div class="text-center">Estoque</div>
      </v-col>
    </v-row>
    <v-row justify="center">
      <v-col cols="12" md="4" sm="6">
        <v-btn block :to="{ name: 'Locacao' }" size="x-large" color="primary" variant="outlined">Consultar
          Locação</v-btn>
      </v-col>
    </v-row>
    <v-row justify="center">
      <v-col cols="12" md="4" sm="6">
        <v-btn block :to="{ name: 'Material' }" size="x-large" color="primary" variant="outlined">Consultar Item</v-btn>
      </v-col>
    </v-row>
    <v-row justify="center">
      <v-col cols="12" md="4" sm="6">
        <v-btn block :to="{ name: 'Coleta' }" size="x-large" color="primary" variant="outlined">Coletar</v-btn>
      </v-col>
    </v-row>
    <v-row justify="center">
      <v-col cols="12" md="4" sm="6">
        <v-btn block :to="{ name: 'Transferir' }" size="x-large" color="primary" variant="outlined">Transferir</v-btn>
      </v-col>
    </v-row>
    <v-row justify="center">
      <v-col cols="12" md="4" sm="6">
        <v-btn rounded="0" size="x-large" prepend-icon="" class="inactive-btn" block>Contagem</v-btn>
      </v-col>
    </v-row>
  </v-container>

  <v-bottom-navigation grow>
    <v-btn label="Home" class="active-btn" :to="{ name: 'Home' }">
      <v-icon class="default-icon">mdi-home</v-icon> <span>Home</span>
    </v-btn>
    <v-btn label="Sair" class="active-btn" @click="confirmationdialog = true">
      <v-icon class="default-icon">mdi-logout</v-icon> <span>Sair</span>
    </v-btn>
  </v-bottom-navigation>

  <!-- Dialog de confirmação -->
  <v-dialog v-model="confirmationdialog" max-width="500" persistent>
    <v-card>
      <v-card-text> Tem certeza de que deseja sair? </v-card-text>
      <v-card-actions class="mx-auto">
        <v-row justify="center" class="mb-5">
          <v-col>
            <v-btn color="green-darken-1" variant="elevated" block @click="confirmLogout">
              <v-icon left>mdi-check</v-icon> Sim </v-btn>
          </v-col>
          <v-col>
            <v-btn class="bg-red-accent-4" variant="elevated" block @click="confirmationdialog = false">
              <v-icon left>mdi-close</v-icon> Não </v-btn>
          </v-col>
        </v-row>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

<style scoped></style>

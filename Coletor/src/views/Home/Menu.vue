<script setup>
import { ref } from 'vue';
import { logout } from '@/router';

const confirmationdialog = ref(false);

function confirmLogout() {
  logout();
}
</script>

<template>
  <v-container class="collector-home text-center">
    <v-row justify="center">
      <v-col cols="12" md="4" sm="6">
        <v-btn block :to="{ name: 'Recebimento' }" size="x-large" color="primary" variant="outlined">Recebimento</v-btn>
      </v-col>
    </v-row>
    <v-row justify="center">
      <v-col cols="12" md="4" sm="6">
        <v-btn block :to="{ name: 'Estoque' }" size="x-large" color="primary" variant="outlined">Estoque</v-btn>
      </v-col>
    </v-row>
    <v-row justify="center">
      <v-col cols="12" md="4" sm="6">
        <v-btn block :to="{ name: 'Separacao' }" size="x-large" color="primary" variant="outlined">Separação</v-btn>
      </v-col>
    </v-row>
    <v-row justify="center">
      <v-col cols="12" md="4" sm="6">
        <v-btn block :to="{ name: 'Expedicao' }" size="x-large" color="primary" variant="outlined">Expedição</v-btn>
      </v-col>
    </v-row>
    <v-row justify="center">
      <v-col cols="12" md="4" sm="6">
        <v-btn rounded="0" size="x-large" prepend-icon="" class="inactive-btn" block  >Inventário</v-btn>
      </v-col>
    </v-row>
    <!-- <v-row justify="center">
      <v-col cols="12" md="4" sm="6">
        <v-btn rounded="0" size="x-large" prepend-icon="" block :to="{ name: 'Armazenagem' }"
          class="active-btn">Armazenagem</v-btn>
      </v-col>
    </v-row> -->
    <!-- <v-row justify="center">
      <v-col cols="12" md="4" sm="6">
        <v-btn rounded="0" size="x-large" prepend-icon="mdi-cog" block class="inactive-btn">Configuração</v-btn>
      </v-col>
    </v-row> -->
  </v-container>

  <v-bottom-navigation grow>
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

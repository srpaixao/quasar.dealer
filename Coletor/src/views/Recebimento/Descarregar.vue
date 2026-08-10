<script setup>
import { reactive, ref, nextTick, onMounted, watch, computed } from 'vue';
import { logout } from '@/router';
import apiService from '../../http/request.js';

import { useAuthStore } from '@/stores/authStore.js';

const authStore = useAuthStore();
const user = authStore.getUser;
const form = reactive({
  itemnr: ''
});

const areas = ref([]);
const selectedArea = ref(null);
const isVolumeDisabled = ref(true);
const total = ref(0);
const pendentes = ref(0);
const confirmados = ref(0);
const incorretos = ref(0);

let dialogMessage = '';
const dialog = ref(false);
const confirmationdialog = ref(false);
const volume = ref(null);
const itemFound = ref(false);

const loading = ref(false);

const itemInput = ref(null);
const focusItem = () => {
  setFocus(itemInput);
};

const setFocus = (field) => {
  nextTick(() => {
    if (field && field.value) {
      field.value.focus();
    }
  });
};

const consultarItem = async () => {

  if (form.itemnr.trim().length === 0) {
    return;
  }
  dialogMessage = '';
  dialog.value = false;
  //loading.value = true;
  try {
    const response = await apiService.processarVolume(form.itemnr, selectedArea.value, user.filialId, user.account);
    console.log(response.data)
    form.itemnr = '';
    focusItem();

    if (!response.data.erro) {
      total.value = response.data.total;
      pendentes.value = response.data.pendentes;
      confirmados.value = response.data.conferidos;
      incorretos.value = response.data.incorretos;

      if (response.data.finalizado) {
        dialogMessage = response.data.msg;
        dialog.value = true;
      }
    } else {
      total.value = response.data.total;
      incorretos.value = response.data.incorretos;
      if (response.data.notfound) {
        dialogMessage = response.data.msg;
        dialog.value = true;
      } else {
        dialogMessage = response.data.msg;
        dialog.value = true;
      }
    }
    //loading.value = false;
  }
  catch (error) {
    loading.value = false;
    if (error.response && error.response.data) {
      dialogMessage = error.response.data.mensagem || 'Erro ao buscar os dados do volume. Por favor, tente novamente.';
      dialog.value = true;
    }
    else {
      dialogMessage = 'Erro ao buscar os dados do volume. Por favor, tente novamente.';
      dialog.value = true;
    }
  }
  finally {
    //loading.value = false;
  }

}

const onDialogClose = (value) => {
  if (!value) {
    form.itemnr = '';
    focusItem();
  }
};

watch(dialog, (newVal) => {
  if (!newVal) {
    onDialogClose(newVal);
  }
});

watch(selectedArea, async (newVal) => {
  if (newVal) {
    try {
      const response = await apiService.contarVolume(newVal, user.filialId);
      console.log(response.data);
      total.value = response.data.filter(status => status.statusId != 3).length;;
      pendentes.value = response.data.filter(status => status.statusId === 1).length;
      confirmados.value = response.data.filter(status => status.statusId === 2).length;
      incorretos.value = response.data.filter(status => status.statusId === 3).length;

      isVolumeDisabled.value = false;
      focusItem();
    } catch (error) {
      console.error('Erro ao obter contadores:', error)
      total.value = 0;
      pendentes.value = 0;
      confirmados.value = 0;
      incorretos.value = 0;
    }
  } else {
    isVolumeDisabled.value = true;
    total.value = pendentes.value = confirmados.value = incorretos.value = 0;
  }
})


function confirmLogout() {
  logout();
}

onMounted(async () => {
  try {
    const response = await apiService.obterAreas(user.filialId);
    // console.log(response.data)
    areas.value = response.data;
  }
  catch (error) {
    loading.value = false;
    if (error.response && error.response.data) {
      dialogMessage = error.response.data.mensagem || 'Erro ao buscar a lista de áreas. Por favor, tente novamente.';
      dialog.value = true;
    }
    else {
      dialogMessage = 'Erro ao buscar a lista de áreas. Por favor, tente novamente.';
      dialog.value = true;
    }
  }
  finally {

  }

});

const statusList = [
  { status: 'Pendente', quantidade: 3 },
  { status: 'Em andamento', quantidade: 5 },
  { status: 'Concluído', quantidade: 7 },
]
</script>

<template>
  <v-container>

    <div>
      <v-row dense>
        <v-col cols="12" md="6" lg="4">
          <div class="text-center">Recebimento / Descarregar</div>
        </v-col>
      </v-row>

      <v-row dense>
        <v-col cols="12" md="6" lg="4">
          <v-select label="Área" :items="areas" v-model="selectedArea" item-title="descricao" item-value="id"
            density="comfortable" hide-details="true" outlined />
        </v-col>
      </v-row>

      <div v-if="!isVolumeDisabled">
        <v-row dense>
          <v-col cols="12" md="6" lg="4">
            <v-text-field label="Volume NR" v-model="form.itemnr" ref="itemInput" @blur="consultarItem" outlined
              density="comfortable" hide-details="true">
            </v-text-field>
          </v-col>
        </v-row>


        <v-list>
          <v-list-item class="bg-orange-lighten-1 pa-4 my-1 rounded" height="30">
            <template #default>
              <div class="d-flex justify-space-between w-100">
                <span class="font-weight-medium text-white text-h6">Pendentes</span>
                <span class="font-weight-bold text-white text-h6">{{ pendentes }}</span>
              </div>
            </template>
          </v-list-item>

          <v-list-item class="bg-green-lighten-1 pa-4 my-1 rounded" height="30">
            <template #default>
              <div class="d-flex justify-space-between w-100">
                <span class="font-weight-medium text-h6">Confirmados</span>
                <span class="font-weight-bold text-white text-h6">{{ confirmados }}</span>
              </div>
            </template>
          </v-list-item>

          <v-list-item class="bg-blue-lighten-1 pa-4 my-1 rounded" height="30">
            <template #default>
              <div class="d-flex justify-space-between w-100">
                <span class="font-weight-medium text-h6">Total</span>
                <span class="font-weight-bold text-white text-h6">{{ total }}</span>
              </div>
            </template>
          </v-list-item>
        </v-list>


        <v-list-item class="bg-red-lighten-1 pa-4 my-1 rounded mt-5" height="30">
            <template #default>
              <div class="d-flex justify-space-between w-100">
                <span class="font-weight-medium text-h6">Incorretos</span>
                <span class="font-weight-bold text-white text-h6">{{ incorretos }}</span>
              </div>
            </template>
          </v-list-item>

        <!-- <v-row dense>
          <v-col cols="12">
            <v-card color="yellow-lighten-1" class="text-center pa-1">
              <div class="text-h5">{{ pendentes }}</div>
              <div class="text-subtitle-3">PENDENTES</div>
            </v-card>
          </v-col>
        </v-row>

        <v-row dense>
          <v-col cols="12">
            <v-card color="green-lighten-1" class="text-center pa-1">
              <div class="text-h5">{{ confirmados }}</div>
              <div class="text-subtitle-3">CONFIRMADOS</div>
            </v-card>
          </v-col>
        </v-row>

        <v-row dense>
          <v-col cols="12">
            <v-card color="red-lighten-1" class="text-center pa-1">
              <div class="text-h5">{{ incorretos }}</div>
              <div class="text-subtitle-3">INCORRETOS</div>
            </v-card>
          </v-col>
        </v-row>

        <v-row dense>
          <v-col cols="12">
            <v-card color="blue-lighten-1" class="text-center pa-1">
              <div class="text-h5">{{ total }}</div>
              <div class="text-subtitle-3">TOTAL</div>
            </v-card>
          </v-col>
        </v-row> -->

      </div>


    </div>

    <v-bottom-navigation grow>
      <v-btn label="Voltar" class="active-btn" :to="{ name: 'Recebimento' }">
        <v-icon>mdi-arrow-left</v-icon> <span>Voltar</span>
      </v-btn>
      <v-btn label="Menu" class="active-btn" :to="{ name: 'Home' }">
        <v-icon>mdi-home</v-icon> <span>Home</span>
      </v-btn>
      <v-btn label="Sair" class="active-btn" @click="confirmationdialog = true">
        <v-icon>mdi-logout</v-icon> <span>Sair</span>
      </v-btn>
    </v-bottom-navigation>

    <!-- Mensagens (popup) -->
    <v-dialog v-model="dialog" max-width="500" persistent>
      <v-card>
        <v-card-text>
          {{ dialogMessage }}
        </v-card-text>
        <v-card-actions class="mx-auto">
          <v-row justify="center">
            <v-btn class="bg-red-accent-4 small-font" variant="elevated" block @click="dialog = false"><v-icon
                left>mdi-close</v-icon> Fechar </v-btn>
          </v-row>
        </v-card-actions>
      </v-card>
    </v-dialog>

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

  </v-container>
</template>

<style scoped>
.no-select {
  user-select: none !important;
  pointer-events: none !important;
}

div.v-input__details {
  display: hidden;
}

.small-font {
  font-size: 75% !important;
}

.mdi-spin:before {
  animation: mdi-spin 1s infinite linear !important;
}

:deep(.v-skeleton-loader__bone.v-skeleton-loader__text) {
  border-radius: 0px;
  margin-left: 0px;
  margin-right: 0px;
  margin-bottom: 0px;
  height: 48px;
}
</style>
<script setup>
import { reactive, ref, nextTick, onMounted, watch, computed } from 'vue';
import { logout } from '@/router';
import apiService from '../../http/request.js';

import { useAuthStore } from '@/stores/authStore.js';

const authStore = useAuthStore();
const user = authStore.getUser;
const form = reactive({
  itemnr: '',
  descricao: '',
  un: '',
  locacao: '',
  saldo: 0,
  indisponivel: 0,
  pedidoPendente: 0,
  curva: '',
  itemCritico: false
});

let dialogMessage = '';
const dialog = ref(false);
const confirmationdialog = ref(false);
const material = ref(null);
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

  form.descricao = '';
  form.locacao = '';
  form.saldo = 0;
  form.indisponivel = 0;
  form.pedidoPendente = 0;
  form.curva = '';
  form.un = '';
  form.itemCritico = false;
  itemFound.value = false;
  loading.value = true;

  try {
    const response = await apiService.consultarItem(form.itemnr);
    console.log(response.data)

    loading.value = false;
    material.value = response.data;
    form.descricao = response.data.descricao && response.data.descricao.trim() !== '' ? response.data.descricao : '-';
    form.locacao = response.data.locacao && response.data.locacao.trim() !== '' ? response.data.locacao : '-';
    form.saldo = response.data.saldo ? response.data.saldo : 0;
    form.indisponivel = response.data.indisponivel ? response.data.indisponivel : 0;
    form.pedidoPendente = response.data.pedidoPendente ? response.data.pedidoPendente : 0;
    form.curva = response.data.curva && response.data.curva.trim() !== '' ? response.data.curva : '-';
    form.un = response.data.un && response.data.un.trim() !== '' ? response.data.un : '-';
    form.itemCritico = response.data.itemCritico;
    itemFound.value = true;
  }
  catch (error) {
    loading.value = false;
    if (error.response && error.response.data) {
      dialogMessage = error.response.data.mensagem || 'Erro ao buscar os dados do material. Por favor, tente novamente.';
      dialog.value = true;
    }
    else {
      dialogMessage = 'Erro ao buscar os dados do material. Por favor, tente novamente.';
      dialog.value = true;
    }
  }
  finally {
    loading.value = false;
  }

}

// Reiniciar o formulário
function resetForm() {
  form.itemnr = '';
  form.descricao = '';
  itemFound.value = false;

  dialogMessage = '';
  dialog.value = false;

  focusItem();
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

function confirmLogout() {
  logout();
}

onMounted(() => {
  focusItem();
});

</script>

<template>
  <v-container>

    <div>

      <v-row dense>
        <v-col cols="12" md="6" lg="4">
          <div class="text-center">Estoque / Consultar Item</div>
        </v-col>
      </v-row>

      <v-row dense>
        <v-col cols="12" md="6" lg="4">
          <v-text-field label="Item Nr" v-model="form.itemnr" @input="form.itemnr = form.itemnr.toUpperCase()"
            ref="itemInput" @blur="consultarItem" outlined density="comfortable" hide-details="true">
          </v-text-field>
        </v-col>
      </v-row>

      <div v-if="loading">
        <v-row dense>
          <v-col cols="12" md="6" lg="4">
            <v-skeleton-loader type="text" width="100%"></v-skeleton-loader>
          </v-col>
        </v-row>
        <v-row dense>
          <v-col cols="12" md="6" lg="4">
            <v-skeleton-loader type="text" width="100%"></v-skeleton-loader>
          </v-col>
        </v-row>
      </div>
      <div v-else>
        <div v-show="itemFound">
          <v-row dense v-if="form.itemCritico">
            <v-col cols="12" md="6" lg="4">
              <div class="d-flex justify-center">
                <span class="d-inline-flex align-center bg-error rounded px-3 py-1 text-xs">
                  <v-icon size="14" class="me-1">mdi-lock</v-icon>
                  Item crítico
                </span>
              </div>
            </v-col>
          </v-row>
          <v-row dense>
            <v-col cols="12" md="6" lg="4">
              <v-text-field label="Descrição" v-model="form.descricao" class="no-select" density="comfortable" outlined
                readonly hide-details="true">
              </v-text-field>
            </v-col>
          </v-row>
          <v-row dense>
            <v-col cols="12" md="6" lg="4">
              <v-text-field label="Locação" v-model="form.locacao" class="no-select" density="comfortable" outlined
                readonly hide-details="true">
              </v-text-field>
            </v-col>
          </v-row>
          <v-row dense>
            <v-col cols="12" md="6" lg="4">
              <v-text-field label="Saldo" v-model="form.saldo" class="no-select" density="comfortable" outlined readonly
                hide-details="true">
              </v-text-field>
            </v-col>
          </v-row>
          <v-row dense>
            <v-col cols="12" md="6" lg="4">
              <v-text-field label="Indisponível" v-model="form.indisponivel" class="no-select" density="comfortable"
                outlined readonly hide-details="true">
              </v-text-field>
            </v-col>
          </v-row>
          <v-row dense>
            <v-col cols="12" md="6" lg="4">
              <v-text-field label="Pedido Pendente" v-model="form.pedidoPendente" class="no-select"
                density="comfortable" outlined readonly hide-details="true">
              </v-text-field>
            </v-col>
          </v-row>
          <v-row dense>
            <v-col cols="6" md="4" lg="2">
              <v-text-field label="Curva" v-model="form.curva" class="no-select" density="comfortable" outlined readonly
                hide-details="true">
              </v-text-field>
            </v-col>
            <v-col cols="6" md="4" lg="2">
              <v-text-field label="UN" v-model="form.un" class="no-select" density="comfortable" outlined readonly
                hide-details="true">
              </v-text-field>
            </v-col>
          </v-row>
        </div>
      </div>

    </div>

    <v-bottom-navigation grow>
      <v-btn label="Voltar" class="active-btn" :to="{ name: 'Estoque' }">
        <v-icon>mdi-arrow-left</v-icon> <span>Voltar</span>
      </v-btn>
      <v-btn label="Menu" class="active-btn" :to="{ name: 'Home' }">
        <v-icon>mdi-home</v-icon> <span>Home</span>
      </v-btn>
      <v-btn label="Reiniciar" class="active-btn" @click="resetForm">
        <v-icon>mdi-restart</v-icon> <span>Reiniciar</span>
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

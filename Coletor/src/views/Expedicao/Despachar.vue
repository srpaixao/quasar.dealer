<script setup>
import { reactive, ref, nextTick, onMounted, watch } from 'vue';
import { logout } from '@/router';
import apiService from '../../http/request.js';

import { useAuthStore } from '@/stores/authStore.js';

const authStore = useAuthStore();
const user = authStore.getUser;

const form = reactive({
  itemnr: '',
  descricao: '',
  locacao: '',
  quantidade: null,
  locacaoconfirmada: '',
});

let dialogMessage = '';
const dialog = ref(false);
const confirmationdialog = ref(false);
const processing = ref(false);

const loading = ref(false);

const itemInput = ref(null);
const focusItem = () => {
  setFocus(itemInput);
};
const reportDialog = ref(false);
const reportMessage = ref('');

const setFocus = (field) => {
  nextTick(() => {
    if (field && field.value) {
      field.value.focus();
    }
  });
};

// Reiniciar o formulário
function resetForm() {
  form.itemnr = '';
  form.descricao = '';
  form.locacao = '';
  form.quantidade = null;
  form.locacaoconfirmada = '';
  itemFound.value = false;
  locacaoOK.value = false;

  dialogMessage = '';
  dialog.value = false;

  reportMessage.value = '';
  reportDialog.value = false;

  focusItem();
}

const onDialogClose = (value) => {
  if (!value) {
    if (form.quantidade) {
      form.quantidade = null;
      focusQuantidade();
    } else {
      if (form.locacaoconfirmada) {
        form.locacaoconfirmada = '';
        focusLocacaoConfirmada();
      } else {
        if (form.itemnr) {
          form.itemnr = '';
          focusItem();
        }
      }
    }
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
          <div class="text-center">Expedição / Despachar</div>
        </v-col>
      </v-row>
    </div>

    <v-bottom-navigation grow>
      <v-btn label="Voltar" class="active-btn" :to="{ name: 'Expedicao' }">
        <v-icon>mdi-arrow-left</v-icon> <span>Voltar</span>
      </v-btn>
      <v-btn label="Menu" class="active-btn" :to="{ name: 'Home' }">
        <v-icon>mdi-home</v-icon> <span>Home</span>
      </v-btn>
      <!-- <v-btn label="Reiniciar" class="active-btn" @click="resetForm">
        <v-icon>mdi-restart</v-icon> <span>Reiniciar</span>
      </v-btn> -->
      <v-btn label="Sair" class="active-btn" @click="confirmationdialog = true">
        <v-icon>mdi-logout</v-icon> <span>Sair</span>
      </v-btn>
    </v-bottom-navigation>

    <!-- Form para reportar aocorrência -->
    <v-dialog v-model="reportDialog" max-width="600px">
      <v-card>
        <v-card-title class="headline text-center">Reportar Ocorrência</v-card-title>
        <v-card-text>
          <v-select label="Tipo"
            :items="['Ocorrência 1', 'Ocorrência 2', 'Ocorrência 3', 'Ocorrência 4', 'Ocorrência 5', 'Outros']"></v-select>
          <v-textarea label="Observações" v-model="reportMessage" outlined rows="5"></v-textarea>
        </v-card-text>
        <v-card-actions>
          <v-spacer></v-spacer>
          <v-btn color="secondary" text @click="submitReport">
            <v-icon left>mdi-check</v-icon> Enviar
          </v-btn>
          <v-btn color="error" text @click="closeReport">
            <v-icon left>mdi-close</v-icon> Cancelar
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

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
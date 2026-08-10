<script setup>
import { ref, reactive, nextTick, watch, onMounted } from 'vue';
import { logout } from '@/router';
import apiService from '../../http/request.js';
import api from '@/http/axios.js';
import { useAuthStore } from '@/stores/authStore.js';

const authStore = useAuthStore();
const user = authStore.getUser;

const form = reactive({
  itemnr: '',
  descricao: '',
  locacao: '',
  saldo: 0,
});

const dialog = ref(false);
const dialogMessage = ref('');
const confirmationdialog = ref(false);
const itemFound = ref(false);
const loading = ref(false);
const processing = ref(false);

const itemInput = ref(null);

const focusItem = () => {
  nextTick(() => {
    itemInput.value?.focus();
  });
};

const showMessage = (message) => {
  dialogMessage.value = message;
  dialog.value = true;
};

const clearItemData = () => {
  form.descricao = '';
  form.locacao = '';
  form.saldo = 0;
  itemFound.value = false;
};

const resetForm = () => {
  form.itemnr = '';
  clearItemData();
  loading.value = false;
  processing.value = false;
  focusItem();
};

const consultarItem = async () => {
  if (!form.itemnr.trim()) {
    showMessage('Informe o número do item para consultar.');
    return;
  }

  loading.value = true;
  clearItemData();

  try {
    const response = await apiService.consultarItem(form.itemnr.trim(), user.filialId);
    const data = response.data;

    form.descricao = data.descricao?.trim() || '-';
    form.locacao = data.locacao?.trim() || '-';
    form.saldo = data.saldo ?? 0;
    itemFound.value = true;
  } catch (error) {
    if (error.response?.status === 404) {
      showMessage('Item não cadastrado.');
    } else {
      showMessage('Erro ao buscar o item. Tente novamente.');
    }
    resetForm();
  } finally {
    loading.value = false;
  }
};

const confirmarMovimentacao = async () => {
  if (!itemFound.value || !form.itemnr.trim()) {
    return;
  }

  processing.value = true;

  try {
    const coleta = {
      itemNr: form.itemnr.trim(),
      locacaoOrigem: form.locacao,
      qtdOrigem: form.saldo,
      criadoPor: user?.account ?? null,
      FilialId: user?.filialId
    };
    await apiService.gravarColeta(coleta);
    // showMessage('Movimentação registrada com sucesso.');
    resetForm();
  } catch (error) {
    const msg = error.response?.data?.mensagem || 'Erro ao registrar a movimentação. Tente novamente.';
    showMessage(msg);
  } finally {
    processing.value = false;
  }
};

const cancelarMovimentacao = () => {
  resetForm();
};

const confirmLogout = () => {
  logout();
};

watch(dialog, (isOpen) => {
  if (!isOpen) {
    focusItem();
  }
});

onMounted(() => {
  focusItem();
});
</script>

<template>
  <v-container>

    <v-row dense>
      <v-col cols="12" md="6" lg="4">
        <div class="text-center">Estoque / Coletar</div>
      </v-col>
    </v-row>

    <v-row dense>
      <v-col cols="12" md="6" lg="4">
        <v-text-field ref="itemInput" label="Item" v-model="form.itemnr" density="comfortable" outlined
          hide-details="auto" :disabled="processing" :loading="loading" @keyup.enter.prevent="consultarItem"
          @change="consultarItem" />
      </v-col>
    </v-row>

    <v-row dense v-if="itemFound" class="mt-2">
      <v-col cols="12" md="6" lg="4">
        <v-text-field label="Descrição" v-model="form.descricao" density="comfortable" outlined readonly
          hide-details="auto" />
      </v-col>
      <v-col cols="12" md="6" lg="4">
        <v-text-field label="Locação" v-model="form.locacao" density="comfortable" outlined readonly
          hide-details="auto" />
      </v-col>
      <v-col cols="12" md="3" lg="2">
        <v-text-field label="Saldo" v-model="form.saldo" density="comfortable" outlined readonly hide-details="auto" />
      </v-col>
    </v-row>

    <v-row dense v-if="itemFound" class="mt-4">
      <v-col cols="12" sm="6" md="3" lg="2">
        <v-btn color="green-darken-1" class="w-100" :loading="processing" :disabled="processing"
          @click="confirmarMovimentacao">
          Confirmar
        </v-btn>
      </v-col>
      <v-col cols="12" sm="6" md="3" lg="2">
        <v-btn color="red-accent-4" class="w-100" :disabled="processing" @click="cancelarMovimentacao">
          Cancelar
        </v-btn>
      </v-col>
    </v-row>


    <v-bottom-navigation grow class="mt-6">
      <v-btn label="Voltar" class="active-btn" :to="{ name: 'Estoque' }">
        <v-icon>mdi-arrow-left</v-icon>
        <span>Voltar</span>
      </v-btn>
      <v-btn label="Menu" class="active-btn" :to="{ name: 'Home' }">
        <v-icon>mdi-home</v-icon>
        <span>Home</span>
      </v-btn>
      <v-btn label="Sair" class="active-btn" @click="confirmationdialog = true">
        <v-icon>mdi-logout</v-icon>
        <span>Sair</span>
      </v-btn>
    </v-bottom-navigation>

    <v-dialog v-model="dialog" max-width="400" persistent>
      <v-card>
        <v-card-text class="py-5 text-center">
          {{ dialogMessage }}
        </v-card-text>
        <v-card-actions class="justify-center pb-4">
          <v-btn color="primary" variant="elevated" @click="dialog = false">
            <v-icon left>mdi-check</v-icon>
            Fechar
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <v-dialog v-model="confirmationdialog" max-width="400" persistent>
      <v-card>
        <v-card-text class="py-5 text-center">
          Tem certeza de que deseja sair?
        </v-card-text>
        <v-card-actions class="justify-center pb-4">
          <v-btn color="green-darken-1" variant="elevated" @click="confirmLogout">
            <v-icon left>mdi-check</v-icon>
            Sim
          </v-btn>
          <v-btn color="red-accent-4" variant="elevated" @click="confirmationdialog = false">
            <v-icon left>mdi-close</v-icon>
            Não
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </v-container>
</template>

<style scoped>
.active-btn {
  text-transform: none;
}

.w-100 {
  width: 100%;
}
</style>

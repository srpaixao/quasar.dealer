<script setup>
import { ref, reactive, nextTick, watch, onMounted } from 'vue';
import { logout } from '@/router';
import apiService from '../../http/request.js';
import { useAuthStore } from '@/stores/authStore.js';

const authStore = useAuthStore();
const user = authStore.getUser;

const form = reactive({
  locacaoEspera: '',
  locacaoEsperaDescricao: '',
  itemnr: '',
  descricao: '',
  locacao: '',
  saldo: 0,
  estoqueCadastrado: false,
  movimentacaoCorreta: false,
  quantidade: null,
});

const dialog = ref(false);
const dialogMessage = ref('');
const confirmationdialog = ref(false);
const esperaValidada = ref(false);
const itemFound = ref(false);
const loading = ref(false);
const processing = ref(false);
const itensColetados = ref(0);

const esperaInput = ref(null);
const itemInput = ref(null);
const quantidadeInput = ref(null);

const setFocus = (input) => nextTick(() => input.value?.focus?.());
const focusEspera = () => setFocus(esperaInput);
const focusItem = () => setFocus(itemInput);
const focusQuantidade = () => setFocus(quantidadeInput);

const showMessage = (message) => {
  dialogMessage.value = message;
  dialog.value = true;
};

const serverMessage = (error, fallback) =>
  error.response?.data?.mensagem ||
  error.response?.data?.detail ||
  error.response?.data?.title ||
  fallback;

const clearItemData = () => {
  form.itemnr = '';
  form.descricao = '';
  form.locacao = '';
  form.saldo = 0;
  form.estoqueCadastrado = false;
  form.movimentacaoCorreta = false;
  form.quantidade = null;
  itemFound.value = false;
};

const resetItem = () => {
  clearItemData();
  loading.value = false;
  processing.value = false;
  focusItem();
};

const trocarLocacaoEspera = () => {
  clearItemData();
  form.locacaoEspera = '';
  form.locacaoEsperaDescricao = '';
  esperaValidada.value = false;
  itensColetados.value = 0;
  focusEspera();
};

const validarLocacaoEspera = async () => {
  const codigo = form.locacaoEspera?.trim();
  if (!codigo) {
    showMessage('Informe a Locação de Espera.');
    return;
  }

  loading.value = true;
  esperaValidada.value = false;
  clearItemData();

  try {
    const response = await apiService.validarLocacao(codigo);
    form.locacaoEspera = response.data.codigo?.trim() || codigo;
    form.locacaoEsperaDescricao = response.data.descricao?.trim() || '';
    esperaValidada.value = true;
    focusItem();
  } catch (error) {
    form.locacaoEsperaDescricao = '';
    showMessage(serverMessage(error, 'Locação de Espera inválida.'));
  } finally {
    loading.value = false;
  }
};

const consultarItem = async () => {
  if (!esperaValidada.value) {
    showMessage('Valide primeiro a Locação de Espera.');
    return;
  }

  if (!form.itemnr.trim()) {
    showMessage('Informe o número do item para consultar.');
    return;
  }

  const itemNr = form.itemnr.trim();
  loading.value = true;
  form.descricao = '';
  form.locacao = '';
  form.saldo = 0;
  form.quantidade = null;
  itemFound.value = false;

  try {
    const response = await apiService.consultarItem(itemNr);
    const data = response.data;

    form.itemnr = data.itemNr?.trim() || itemNr;
    form.descricao = data.descricao?.trim() || '-';
    form.locacao = data.locacao?.trim() || '';
    form.saldo = data.saldo ?? 0;
    form.estoqueCadastrado = data.estoqueCadastrado === true;
    form.movimentacaoCorreta = data.movimentacaoCorreta === true;

    if (form.estoqueCadastrado && form.movimentacaoCorreta && form.saldo <= 0) {
      showMessage('Item sem saldo disponível em uma locação de origem.');
      clearItemData();
      return;
    }

    itemFound.value = true;
    focusQuantidade();
  } catch (error) {
    showMessage(serverMessage(error, 'Erro ao buscar o item. Tente novamente.'));
    clearItemData();
  } finally {
    loading.value = false;
  }
};

const confirmarMovimentacao = async () => {
  if (!esperaValidada.value || !itemFound.value || !form.itemnr.trim()) return;

  const quantidade = Number(form.quantidade || 0);
  if (!Number.isInteger(quantidade) || quantidade <= 0) {
    showMessage('Informe uma quantidade válida.');
    focusQuantidade();
    return;
  }

  if (form.estoqueCadastrado && form.movimentacaoCorreta && quantidade !== Number(form.saldo || 0)) {
    showMessage('A quantidade coletada deve ser igual ao saldo.');
    focusQuantidade();
    return;
  }

  processing.value = true;

  try {
    await apiService.gravarColeta({
      itemNr: form.itemnr.trim(),
      locacaoOrigem: form.locacao,
      locacaoEspera: form.locacaoEspera,
      qtdOrigem: quantidade,
      criadoPor: user?.account ?? null,
    });

    itensColetados.value += 1;
    resetItem();
  } catch (error) {
    showMessage(serverMessage(error, 'Erro ao registrar a coleta. Tente novamente.'));
  } finally {
    processing.value = false;
  }
};

const confirmarSaida = () => logout();

watch(dialog, (isOpen) => {
  if (!isOpen) {
    if (!esperaValidada.value) focusEspera();
    else if (!itemFound.value) focusItem();
    else focusQuantidade();
  }
});

onMounted(focusEspera);
</script>

<template>
  <v-container>
    <v-row dense>
      <v-col cols="12" md="8" lg="6">
        <div class="text-center text-h6 mb-2">Estoque / Coletar</div>
      </v-col>
    </v-row>

    <v-row dense>
      <v-col cols="12" md="6" lg="4">
        <v-text-field ref="esperaInput" label="Locação de Espera" v-model="form.locacaoEspera"
          density="comfortable" outlined hide-details="auto" :readonly="esperaValidada"
          :disabled="processing" :loading="loading && !esperaValidada"
          @input="form.locacaoEspera = form.locacaoEspera.toUpperCase()"
          @keyup.enter.prevent="validarLocacaoEspera" @change="validarLocacaoEspera" />
      </v-col>
      <v-col v-if="esperaValidada" cols="12" md="3" lg="2" class="d-flex align-center">
        <v-btn variant="outlined" color="primary" block :disabled="processing" @click="trocarLocacaoEspera">
          Trocar locação
        </v-btn>
      </v-col>
    </v-row>

    <v-row dense v-if="esperaValidada">
      <v-col cols="12" md="8" lg="6">
        <v-alert type="info" variant="tonal" density="compact" class="mb-2">
          <strong>Espera: {{ form.locacaoEspera }}</strong>
          <span v-if="form.locacaoEsperaDescricao"> — {{ form.locacaoEsperaDescricao }}</span>
          <span class="ml-2">Itens coletados nesta operação: {{ itensColetados }}</span>
        </v-alert>
      </v-col>
    </v-row>

    <v-row dense v-if="esperaValidada">
      <v-col cols="12" md="6" lg="4">
        <v-text-field ref="itemInput" label="Item" v-model="form.itemnr" density="comfortable" outlined
          hide-details="auto" :disabled="processing" :loading="loading"
          @keyup.enter.prevent="consultarItem" @change="consultarItem" />
      </v-col>
    </v-row>

    <v-row dense v-if="itemFound" class="mt-2">
      <v-col cols="12" md="6" lg="4">
        <v-text-field label="Descrição" v-model="form.descricao" density="comfortable" outlined readonly
          hide-details="auto" />
      </v-col>
      <v-col cols="12" md="4" lg="3">
        <v-text-field label="Locação de Origem" :model-value="form.locacao || 'Item ainda não cadastrado no estoque'"
          density="comfortable" outlined readonly
          hide-details="auto" />
      </v-col>
      <v-col cols="6" md="2" lg="1">
        <v-text-field label="Saldo" :model-value="form.estoqueCadastrado ? form.saldo : '-'" density="comfortable" outlined readonly
          hide-details="auto" />
      </v-col>
      <v-col cols="6" md="3" lg="2">
        <v-text-field ref="quantidadeInput" label="Quantidade" v-model="form.quantidade" type="number"
          min="1" :max="form.estoqueCadastrado && form.movimentacaoCorreta ? form.saldo : undefined" step="1" density="comfortable" outlined hide-details="auto"
          :disabled="processing" @keyup.enter.prevent="confirmarMovimentacao" />
      </v-col>
    </v-row>

    <v-row dense v-if="itemFound" class="mt-4">
      <v-col cols="12" sm="6" md="3" lg="2">
        <v-btn color="green-darken-1" class="w-100" :loading="processing"
          :disabled="processing || !form.quantidade" @click="confirmarMovimentacao">
          Confirmar coleta
        </v-btn>
      </v-col>
      <v-col cols="12" sm="6" md="3" lg="2">
        <v-btn color="red-accent-4" class="w-100" :disabled="processing" @click="resetItem">
          Cancelar item
        </v-btn>
      </v-col>
    </v-row>

    <v-bottom-navigation grow class="mt-6">
      <v-btn label="Voltar" class="active-btn" :to="{ name: 'Estoque' }">
        <v-icon>mdi-arrow-left</v-icon><span>Voltar</span>
      </v-btn>
      <v-btn label="Menu" class="active-btn" :to="{ name: 'Home' }">
        <v-icon>mdi-home</v-icon><span>Home</span>
      </v-btn>
      <v-btn label="Sair" class="active-btn" @click="confirmationdialog = true">
        <v-icon>mdi-logout</v-icon><span>Sair</span>
      </v-btn>
    </v-bottom-navigation>

    <v-dialog v-model="dialog" max-width="400" persistent>
      <v-card>
        <v-card-text class="py-5 text-center">{{ dialogMessage }}</v-card-text>
        <v-card-actions class="justify-center pb-4">
          <v-btn color="primary" variant="elevated" @click="dialog = false">
            <v-icon left>mdi-check</v-icon>Fechar
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <v-dialog v-model="confirmationdialog" max-width="400" persistent>
      <v-card>
        <v-card-text class="py-5 text-center">Tem certeza de que deseja sair?</v-card-text>
        <v-card-actions class="justify-center pb-4">
          <v-btn color="green-darken-1" variant="elevated" @click="confirmarSaida">
            <v-icon left>mdi-check</v-icon>Sim
          </v-btn>
          <v-btn color="red-accent-4" variant="elevated" @click="confirmationdialog = false">
            <v-icon left>mdi-close</v-icon>Não
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </v-container>
</template>

<style scoped>
.active-btn { text-transform: none; }
.w-100 { width: 100%; }
</style>

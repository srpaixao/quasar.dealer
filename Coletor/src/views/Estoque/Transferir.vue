<script setup>
import { reactive, ref, nextTick, onMounted, watch } from 'vue';
import { logout } from '@/router';
import apiService from '../../http/request.js';
import { useAuthStore } from '@/stores/authStore.js';
import { useClienteApiStore } from '@/stores/clienteApiStore.js';

const authStore = useAuthStore();
const clienteApiStore = useClienteApiStore();
const user = authStore.getUser;

const form = reactive({
  locacaoEspera: '',
  locacaoDestinoConf: '',
  qtdDestino: null,
});

const itens = ref([]);
const movimentacaoSelecionada = ref(null);
const esperaValidada = ref(false);
const movimentacaoCorreta = ref(false);
const destinoValidado = ref(false);
const loading = ref(false);
const processing = ref(false);
const dialog = ref(false);
const dialogMessage = ref('');
const confirmationdialog = ref(false);

const esperaInput = ref(null);
const destinoInput = ref(null);
const quantidadeInput = ref(null);

const setFocus = (input) => nextTick(() => input.value?.focus?.());
const focusEspera = () => setFocus(esperaInput);
const focusDestino = () => setFocus(destinoInput);
const focusQuantidade = () => setFocus(quantidadeInput);

const normalizarCodigo = (valor) => (valor || '')
  .toString()
  .replaceAll('.', '')
  .replaceAll(' ', '')
  .trim()
  .toUpperCase();

const serverMessage = (error, fallback) =>
  error.response?.data?.mensagem ||
  error.response?.data?.detail ||
  error.response?.data?.title ||
  fallback;

const showMessage = (message) => {
  dialogMessage.value = message;
  dialog.value = true;
};

const limparSelecao = () => {
  movimentacaoSelecionada.value = null;
  form.locacaoDestinoConf = '';
  form.qtdDestino = null;
  destinoValidado.value = false;
};

const carregarLocacaoEspera = async () => {
  const codigo = form.locacaoEspera?.trim();
  if (!codigo) {
    showMessage('Informe a Locação de Espera.');
    return;
  }

  loading.value = true;
  limparSelecao();

  try {
    const response = await apiService.consultarMovimentacoesLocacaoEspera(codigo);
    form.locacaoEspera = response.data.locacaoEspera?.trim() || codigo;
    movimentacaoCorreta.value = response.data.movimentacaoCorreta === true;
    itens.value = (response.data.itens || []).sort((a, b) => {
      const destinoA = a.locacaoDestino?.trim() || '\uffff';
      const destinoB = b.locacaoDestino?.trim() || '\uffff';
      return destinoA.localeCompare(destinoB, 'pt-BR', { numeric: true }) ||
        (a.itemNr || '').localeCompare(b.itemNr || '', 'pt-BR', { numeric: true });
    });
    esperaValidada.value = true;
  } catch (error) {
    itens.value = [];
    movimentacaoCorreta.value = false;
    esperaValidada.value = false;
    showMessage(serverMessage(error, 'Não foi possível consultar a Locação de Espera.'));
  } finally {
    loading.value = false;
  }
};

const trocarLocacaoEspera = () => {
  limparSelecao();
  itens.value = [];
  movimentacaoCorreta.value = false;
  form.locacaoEspera = '';
  esperaValidada.value = false;
  focusEspera();
};

const selecionarMovimentacao = (item) => {
  if (!item.locacaoDestino?.trim()) {
    limparSelecao();
    showMessage('Locação final não definida para este item.');
    return;
  }

  movimentacaoSelecionada.value = item;
  form.locacaoDestinoConf = '';
  form.qtdDestino = null;
  destinoValidado.value = false;

  focusDestino();
};

const validarDestino = () => {
  if (!movimentacaoSelecionada.value) return;

  const destinoLido = normalizarCodigo(form.locacaoDestinoConf);
  const destinoEsperado = normalizarCodigo(movimentacaoSelecionada.value.locacaoDestino);

  if (!destinoLido) return;

  if (destinoLido !== destinoEsperado) {
    showMessage('Locação final incorreta. Verifique e tente novamente.');
    form.locacaoDestinoConf = '';
    destinoValidado.value = false;
    focusDestino();
    return;
  }

  destinoValidado.value = true;
  focusQuantidade();
};

const confirmarMovimentacao = async () => {
  const movimento = movimentacaoSelecionada.value;
  if (!movimento || !destinoValidado.value) return;

  const quantidade = Number(form.qtdDestino || 0);
  const quantidadeDisponivel = Number(movimento.quantidade || 0);

  if (!Number.isInteger(quantidade) || quantidade <= 0) {
    showMessage('Informe uma quantidade válida.');
    focusQuantidade();
    return;
  }

  if (quantidade > quantidadeDisponivel) {
    showMessage('A quantidade não pode ser maior que o saldo na Locação de Espera.');
    focusQuantidade();
    return;
  }

  if (movimentacaoCorreta.value && quantidade !== quantidadeDisponivel) {
    showMessage('A quantidade transferida deve ser igual à quantidade coletada.');
    focusQuantidade();
    return;
  }

  processing.value = true;

  const auditoria = {
    id: movimento.id,
    itemNr: movimento.itemNr,
    locacaoEspera: form.locacaoEspera,
    destinoMov: form.locacaoDestinoConf.trim(),
    qtdeMov: quantidade,
    usuarioMov: user?.account ?? null,
    dataHoraMov: new Date().toISOString(),
  };

  try {
    const baseDms = clienteApiStore.getBaseApi;
    await apiService.finalizarMovimentacao({
      id: movimento.id,
      locacaoDestino: form.locacaoDestinoConf.trim(),
      qtdDestino: quantidade,
      finalizadoPor: user?.account ?? null,
      urlDMS: baseDms ? `${baseDms}/registrar-movimento` : '',
      payload: JSON.stringify(auditoria),
    });

    limparSelecao();
    await carregarLocacaoEspera();
  } catch (error) {
    showMessage(serverMessage(error, 'Erro ao registrar a transferência. Tente novamente.'));
  } finally {
    processing.value = false;
  }
};

const confirmarSaida = () => logout();

watch(dialog, (isOpen) => {
  if (!isOpen) {
    if (!esperaValidada.value) focusEspera();
    else if (movimentacaoSelecionada.value && !destinoValidado.value) focusDestino();
    else if (destinoValidado.value) focusQuantidade();
  }
});

onMounted(focusEspera);
</script>

<template>
  <v-container>
    <v-row dense>
      <v-col cols="12" md="8" lg="6">
        <div class="text-center text-h6 mb-2">Estoque / Transferir</div>
      </v-col>
    </v-row>

    <v-row dense>
      <v-col cols="12" md="6" lg="4">
        <v-text-field ref="esperaInput" label="Locação de Espera" v-model="form.locacaoEspera"
          density="comfortable" outlined hide-details="auto" :readonly="esperaValidada"
          :disabled="processing" :loading="loading"
          @input="form.locacaoEspera = form.locacaoEspera.toUpperCase()"
          @keyup.enter.prevent="carregarLocacaoEspera" @change="carregarLocacaoEspera" />
      </v-col>
      <v-col v-if="esperaValidada" cols="12" md="3" lg="2" class="d-flex align-center">
        <v-btn variant="outlined" color="primary" block :disabled="processing" @click="trocarLocacaoEspera">
          Trocar locação
        </v-btn>
      </v-col>
    </v-row>

    <v-card v-if="movimentacaoSelecionada" variant="tonal" color="primary" class="mb-4 transfer-card">
      <v-card-title class="text-subtitle-1 font-weight-bold">
        Transferir {{ movimentacaoSelecionada.itemNr }}
      </v-card-title>
      <v-card-text>
        <v-row dense>
          <v-col cols="12" md="4">
            <v-text-field label="Locação Final" :model-value="movimentacaoSelecionada.locacaoDestino"
              density="comfortable" outlined readonly hide-details="auto" />
          </v-col>
          <v-col cols="12" md="4">
            <v-text-field ref="destinoInput" label="Confirmar Locação Final" v-model="form.locacaoDestinoConf"
              density="comfortable" outlined hide-details="auto" :disabled="processing || destinoValidado"
              @input="form.locacaoDestinoConf = form.locacaoDestinoConf.toUpperCase()"
              @keyup.enter.prevent="validarDestino" @change="validarDestino" />
          </v-col>
          <v-col cols="12" md="2" v-if="destinoValidado">
            <v-text-field ref="quantidadeInput" type="number" min="1" :max="movimentacaoSelecionada.quantidade"
              step="1" label="Quantidade" v-model="form.qtdDestino" density="comfortable" outlined
              hide-details="auto" :disabled="processing" @keyup.enter.prevent="confirmarMovimentacao" />
          </v-col>
          <v-col cols="12" md="2" v-if="destinoValidado" class="d-flex align-center">
            <v-btn color="green-darken-1" block :loading="processing" :disabled="processing || !form.qtdDestino"
              @click="confirmarMovimentacao">
              Confirmar
            </v-btn>
          </v-col>
        </v-row>
      </v-card-text>
    </v-card>

    <v-row dense v-if="esperaValidada" class="mt-2">
      <v-col cols="12" md="10" lg="8">
        <v-alert v-if="itens.length === 0" type="success" variant="tonal" density="compact">
          Não há itens pendentes nesta Locação de Espera.
        </v-alert>

        <v-card v-for="item in itens" :key="item.id" variant="outlined" class="mb-2 pending-card"
          :color="movimentacaoSelecionada?.id === item.id ? 'blue-lighten-5' : undefined">
          <v-card-text class="py-3">
            <v-row dense align="center">
              <v-col cols="12" sm="3">
                <div class="text-caption text-medium-emphasis">Item</div>
                <div class="font-weight-bold">{{ item.itemNr }}</div>
              </v-col>
              <v-col cols="12" sm="4">
                <div class="text-caption text-medium-emphasis">Descrição</div>
                <div>{{ item.descricao }}</div>
              </v-col>
              <v-col cols="4" sm="2">
                <div class="text-caption text-medium-emphasis">Qtde</div>
                <div class="font-weight-bold">{{ item.quantidade }}</div>
              </v-col>
              <v-col cols="4" sm="2">
                <div class="text-caption text-medium-emphasis">Locação</div>
                <div class="font-weight-bold">{{ item.locacaoDestino || 'Não definida' }}</div>
              </v-col>
              <v-col cols="4" sm="1" class="text-right">
                <v-btn icon="mdi-arrow-right" color="primary" size="small"
                  :disabled="processing || !item.locacaoDestino" @click="selecionarMovimentacao(item)" />
              </v-col>
            </v-row>
          </v-card-text>
        </v-card>
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
.pending-card { border-radius: 12px; }
.transfer-card { max-width: 1100px; }
</style>

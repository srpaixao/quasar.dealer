<script setup>
import { computed, nextTick, onMounted, reactive, ref } from 'vue';
import { logout } from '@/router';
import apiService from '../../http/request.js';
import { useAuthStore } from '@/stores/authStore.js';

const authStore = useAuthStore();
const user = authStore.getUser;

const form = reactive({ volume: '' });
const itens = ref([]);
const volumeCarregado = ref('');
const loading = ref(false);
const processingId = ref(null);
const volumeInput = ref(null);

const messageDialog = ref(false);
const messageText = ref('');
const divergenceDialog = ref(false);
const pendingItem = ref(null);
const logoutDialog = ref(false);

const resumo = computed(() => ({
  total: itens.value.length,
  conferidos: itens.value.filter(item => item.conferido).length,
  pendentes: itens.value.filter(item => !item.conferido).length,
}));

const toNumber = value => {
  if (value === null || value === undefined || value === '') return null;
  const parsed = Number(String(value).replace(',', '.'));
  return Number.isFinite(parsed) ? parsed : null;
};

const diferenca = item => {
  const quantidade = toNumber(item.qtdDigitada);
  return quantidade === null ? null : quantidade - Number(item.quantidade);
};

const formatarQuantidade = value => {
  if (value === null || value === undefined) return '-';
  return Number(value).toLocaleString('pt-BR', { minimumFractionDigits: 0, maximumFractionDigits: 3 });
};

const formatarDiferenca = value => {
  if (value === null) return '-';
  const texto = formatarQuantidade(value);
  return value > 0 ? `+${texto}` : texto;
};

const podeEditar = item => !item.conferido
  || String(item.usuarioConferencia || '').toLowerCase() === String(user.account || '').toLowerCase();

const focusVolume = () => nextTick(() => volumeInput.value?.focus());
const focusQuantidade = item => nextTick(() => document.getElementById(`qtd-conferida-${item.id}`)?.focus());

const mostrarMensagem = mensagem => {
  messageText.value = mensagem;
  messageDialog.value = true;
};

const mensagemErro = (error, fallback) => error?.response?.data?.mensagem
  || error?.response?.data?.detail
  || error?.message
  || fallback;

const limpar = () => {
  form.volume = '';
  volumeCarregado.value = '';
  itens.value = [];
  pendingItem.value = null;
  divergenceDialog.value = false;
  focusVolume();
};

const carregarVolume = async () => {
  const volume = form.volume.trim();
  if (!volume || loading.value) return;

  loading.value = true;
  itens.value = [];
  try {
    const response = await apiService.obterConferenciaVolume(volume);
    volumeCarregado.value = response.data.volume;
    itens.value = response.data.itens.map(item => ({
      ...item,
      qtdDigitada: item.qtdConferida ?? null,
      marcarConferido: Boolean(item.conferido),
    }));

    const primeiroPendente = itens.value.find(item => !item.conferido);
    if (primeiroPendente) focusQuantidade(primeiroPendente);
  } catch (error) {
    volumeCarregado.value = '';
    mostrarMensagem(mensagemErro(error, 'Erro ao localizar o volume.'));
  } finally {
    loading.value = false;
  }
};

const solicitarConfirmacao = item => {
  const quantidade = toNumber(item.qtdDigitada);
  if (quantidade === null) {
    mostrarMensagem('Informe a quantidade conferida.');
    focusQuantidade(item);
    return;
  }
  if (quantidade < 0) {
    mostrarMensagem('A quantidade conferida não pode ser negativa.');
    focusQuantidade(item);
    return;
  }
  if (!item.marcarConferido) {
    mostrarMensagem('Marque o campo Conferido antes de finalizar.');
    return;
  }

  if (diferenca(item) !== 0) {
    pendingItem.value = item;
    divergenceDialog.value = true;
    return;
  }

  salvar(item, false);
};

const salvar = async (item, confirmarDivergencia) => {
  if (!item || processingId.value !== null) return;

  processingId.value = item.id;
  try {
    const response = await apiService.confirmarConferenciaItem(volumeCarregado.value, item.id, {
      qtdConferida: toNumber(item.qtdDigitada),
      conferido: true,
      confirmarDivergencia,
      modificadoEmEsperado: item.modificadoEm,
    });

    const atualizado = response.data;
    Object.assign(item, atualizado, {
      notaFiscal: atualizado.notaFiscal || item.notaFiscal,
      qtdDigitada: atualizado.qtdConferida,
      marcarConferido: atualizado.conferido,
    });
    divergenceDialog.value = false;
    pendingItem.value = null;

    const proximo = itens.value.find(candidato => !candidato.conferido);
    if (proximo) {
      focusQuantidade(proximo);
    } else {
      mostrarMensagem('Conferência do volume finalizada.');
    }
  } catch (error) {
    divergenceDialog.value = false;
    pendingItem.value = null;
    mostrarMensagem(mensagemErro(error, 'Não foi possível finalizar a conferência.'));
    if (error?.response?.status === 409) {
      await carregarVolume();
    } else {
      focusQuantidade(item);
    }
  } finally {
    processingId.value = null;
  }
};

const cancelarDivergencia = () => {
  const item = pendingItem.value;
  divergenceDialog.value = false;
  pendingItem.value = null;
  if (item) focusQuantidade(item);
};

onMounted(focusVolume);
</script>

<template>
  <v-container class="conference-screen">
    <div class="text-center screen-title mb-2">Recebimento / Conferência de Volume</div>

    <v-row dense>
      <v-col cols="12">
        <v-text-field
          ref="volumeInput"
          v-model="form.volume"
          label="Volume"
          density="comfortable"
          hide-details
          :disabled="loading"
          @keyup.enter="carregarVolume"
        />
      </v-col>
    </v-row>

    <v-row dense class="mt-2">
      <v-col cols="8">
        <v-btn color="primary" block :loading="loading" @click="carregarVolume">
          <v-icon>mdi-magnify</v-icon>&nbsp;Localizar
        </v-btn>
      </v-col>
      <v-col cols="4">
        <v-btn variant="outlined" block @click="limpar">Limpar</v-btn>
      </v-col>
    </v-row>

    <v-card v-if="volumeCarregado" variant="tonal" class="my-3">
      <v-card-text class="py-2">
        <div><strong>Volume:</strong> {{ volumeCarregado }}</div>
        <div class="d-flex justify-space-between mt-1">
          <span>Total: {{ resumo.total }}</span>
          <span>Conferidos: {{ resumo.conferidos }}</span>
          <span>Pendentes: {{ resumo.pendentes }}</span>
        </div>
      </v-card-text>
    </v-card>

    <v-card v-for="item in itens" :key="item.id" class="mb-3" :color="item.conferido ? 'green-lighten-5' : undefined">
      <v-card-title class="text-subtitle-1 py-2">
        Item {{ item.item }}
      </v-card-title>
      <v-card-subtitle>NF {{ item.notaFiscal }}<span v-if="item.pedido"> · Pedido {{ item.pedido }}</span></v-card-subtitle>
      <v-card-text class="pb-2">
        <v-alert
          v-if="item.itemCritico"
          type="warning"
          variant="tonal"
          density="compact"
          class="mb-3 critical-item-alert"
          icon="mdi-alert"
        >
          <strong>ITEM CRÍTICO:</strong>
          {{ item.observacaoItemCritico || 'Item crítico sem observação cadastrada.' }}
        </v-alert>

        <v-row dense>
          <v-col cols="4">
            <div class="quantity-label">Qtde NF</div>
            <div class="quantity-value">{{ formatarQuantidade(item.quantidade) }}</div>
          </v-col>
          <v-col cols="4">
            <div class="quantity-label">Diferença</div>
            <div class="quantity-value" :class="{ 'text-error': diferenca(item) !== 0 && diferenca(item) !== null }">
              {{ formatarDiferenca(diferenca(item)) }}
            </div>
          </v-col>
          <v-col cols="4">
            <div class="quantity-label">Qtde Armazenada</div>
            <div class="quantity-value">{{ formatarQuantidade(item.qtdArmazenada) }}</div>
          </v-col>
        </v-row>

        <v-text-field
          :id="`qtd-conferida-${item.id}`"
          v-model="item.qtdDigitada"
          label="Qtde Conferida"
          type="number"
          min="0"
          step="0.001"
          density="comfortable"
          class="mt-3"
          :disabled="!podeEditar(item) || processingId === item.id"
        />

        <v-checkbox
          v-model="item.marcarConferido"
          label="Conferido"
          density="compact"
          hide-details
          :disabled="!podeEditar(item) || processingId === item.id"
        />

        <div class="text-caption mt-1">
          Situação: <strong>{{ item.situacao }}</strong>
          <span v-if="item.usuarioConferencia"> · {{ item.usuarioConferencia }}</span>
        </div>
      </v-card-text>
      <v-card-actions>
        <v-btn
          color="green-darken-1"
          variant="elevated"
          block
          :loading="processingId === item.id"
          :disabled="!podeEditar(item) || processingId !== null"
          @click="solicitarConfirmacao(item)"
        >
          <v-icon>mdi-check</v-icon>&nbsp;Confirmar
        </v-btn>
      </v-card-actions>
    </v-card>

    <v-bottom-navigation grow>
      <v-btn :to="{ name: 'Recebimento' }"><v-icon>mdi-arrow-left</v-icon><span>Voltar</span></v-btn>
      <v-btn :to="{ name: 'Home' }"><v-icon>mdi-home</v-icon><span>Home</span></v-btn>
      <v-btn @click="limpar"><v-icon>mdi-restart</v-icon><span>Reiniciar</span></v-btn>
      <v-btn @click="logoutDialog = true"><v-icon>mdi-logout</v-icon><span>Sair</span></v-btn>
    </v-bottom-navigation>

    <v-dialog v-model="divergenceDialog" max-width="440" persistent>
      <v-card v-if="pendingItem">
        <v-card-title class="text-subtitle-1">
          Confirma a quantidade conferida a {{ diferenca(pendingItem) < 0 ? 'menor' : 'maior' }}?
        </v-card-title>
        <v-card-text>
          <div>Quantidade da NF: {{ formatarQuantidade(pendingItem.quantidade) }}</div>
          <div>Quantidade conferida: {{ formatarQuantidade(toNumber(pendingItem.qtdDigitada)) }}</div>
          <div>Diferença: {{ formatarDiferenca(diferenca(pendingItem)) }}</div>
          <div class="mt-3">Deseja finalizar a conferência?</div>
        </v-card-text>
        <v-card-actions class="flex-column">
          <v-btn color="green-darken-1" variant="elevated" block @click="salvar(pendingItem, true)">Sim, finalizar</v-btn>
          <v-btn color="red-accent-4" variant="elevated" block class="mt-2" @click="cancelarDivergencia">Não, corrigir</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <v-dialog v-model="messageDialog" max-width="420" persistent>
      <v-card>
        <v-card-text class="py-5 text-center">{{ messageText }}</v-card-text>
        <v-card-actions><v-btn color="primary" variant="elevated" block @click="messageDialog = false">Fechar</v-btn></v-card-actions>
      </v-card>
    </v-dialog>

    <v-dialog v-model="logoutDialog" max-width="420" persistent>
      <v-card>
        <v-card-text class="py-5 text-center">Tem certeza de que deseja sair?</v-card-text>
        <v-card-actions>
          <v-btn color="green-darken-1" variant="elevated" @click="logout">Sim</v-btn>
          <v-btn color="red-accent-4" variant="elevated" @click="logoutDialog = false">Não</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </v-container>
</template>

<style scoped>
.conference-screen { max-width: 480px; padding-bottom: 72px; }
.screen-title { font-size: 1rem; font-weight: 700; }
.quantity-label { color: rgba(0, 0, 0, 0.6); font-size: 0.7rem; }
.quantity-value { font-size: 1rem; font-weight: 700; }
</style>

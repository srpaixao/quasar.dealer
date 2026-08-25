<script setup>
import { computed, nextTick, onMounted, ref, watch } from 'vue';
import { logout } from '@/router';
import { onBeforeRouteLeave, useRouter } from 'vue-router';
import apiService from '../../http/request.js';

const router = useRouter();

const confirmationdialog = ref(false);
const dialog = ref(false);
const dialogMessage = ref('');
const zeroDialog = ref(false);
const loadingRomaneios = ref(false);
const startingConference = ref(false);
const loadingSnapshot = ref(false);
const confirmingItem = ref(false);
const releasingConference = ref(false);

const romaneios = ref([]);
const selectedRomaneioId = ref(null);
const currentRomaneio = ref(null);
const currentItem = ref(null);
const itens = ref([]);
const conferenceFinished = ref(false);
const finishMessage = ref('');
const quantidadeInformada = ref(null);

const romaneioInput = ref(null);
const quantidadeInput = ref(null);

const showMessage = (message) => {
  dialogMessage.value = message;
  dialog.value = true;
};

const setFocus = (elRef) => {
  nextTick(() => {
    elRef?.value?.focus?.();
  });
};

const focusRomaneio = () => setFocus(romaneioInput);
const focusQuantidade = () => setFocus(quantidadeInput);

const romaneioOptions = computed(() =>
  romaneios.value.map((romaneio) => ({
    ...romaneio,
    displayName: romaneio.romaneioNr
  }))
);

const hasConferenceContext = computed(() => !!currentRomaneio.value?.romaneioId);

const headerTitle = computed(() => {
  if (!currentRomaneio.value?.romaneioNr) {
    return 'Expedicao / Conferir Separacao';
  }

  return `Expedicao / Conferir Separacao - ${currentRomaneio.value.romaneioNr}`;
});

const extractErrorMessage = (error, fallback) => {
  return error?.response?.data?.mensagem
    || error?.response?.data?.message
    || error?.response?.data?.title
    || error?.response?.data?.Title
    || fallback;
};

const clearQuantidade = () => {
  quantidadeInformada.value = null;
};

const resetConferenceState = ({ keepSelection = true } = {}) => {
  currentRomaneio.value = null;
  currentItem.value = null;
  itens.value = [];
  conferenceFinished.value = false;
  finishMessage.value = '';
  clearQuantidade();

  if (!keepSelection) {
    selectedRomaneioId.value = null;
  }
};

const canReleaseConference = () => {
  return !!currentRomaneio.value?.romaneioId && !conferenceFinished.value;
};

const releaseConference = async ({ silent = false } = {}) => {
  if (!canReleaseConference()) {
    return true;
  }

  if (releasingConference.value) {
    return true;
  }

  releasingConference.value = true;

  try {
    await apiService.liberarConferenciaSeparacao(currentRomaneio.value.romaneioId);
    return true;
  } catch (error) {
    console.error(error);
    if (!silent) {
      showMessage(extractErrorMessage(error, 'Falha ao liberar a conferencia de separacao.'));
    }
    return false;
  } finally {
    releasingConference.value = false;
  }
};

const loadRomaneios = async () => {
  loadingRomaneios.value = true;
  try {
    const response = await apiService.obterRomaneiosConferenciaSeparacao();
    romaneios.value = Array.isArray(response.data) ? response.data : [];

    if (selectedRomaneioId.value && !romaneios.value.some((romaneio) => String(romaneio.id) === String(selectedRomaneioId.value))) {
      selectedRomaneioId.value = null;
    }
  } catch (error) {
    console.error(error);
    showMessage('Falha ao carregar os romaneios disponiveis.');
  } finally {
    loadingRomaneios.value = false;
  }
};

const handleRomaneioSelection = async (value) => {
  selectedRomaneioId.value = value;

  if (!value || startingConference.value || hasConferenceContext.value) {
    return;
  }

  await iniciarConferencia();
};

const applySnapshot = async (payload) => {
  itens.value = Array.isArray(payload?.itens) ? payload.itens : [];
  currentItem.value = payload?.itemAtual || null;

  if (payload?.finalizado || !payload?.itemAtual) {
    currentItem.value = null;
    conferenceFinished.value = true;
    finishMessage.value = payload?.mensagem || 'Conferencia finalizada com sucesso.';
    clearQuantidade();
    await loadRomaneios();
    return;
  }

  conferenceFinished.value = false;
  finishMessage.value = '';
  clearQuantidade();
  focusQuantidade();
};

const loadSnapshot = async () => {
  if (!currentRomaneio.value?.romaneioId) {
    return;
  }

  loadingSnapshot.value = true;
  try {
    const response = await apiService.obterItensConferenciaSeparacao(currentRomaneio.value.romaneioId);
    await applySnapshot(response.data || {});
  } catch (error) {
    console.error(error);
    showMessage(extractErrorMessage(error, 'Falha ao carregar os itens do romaneio.'));
  } finally {
    loadingSnapshot.value = false;
  }
};

const iniciarConferencia = async () => {
  if (!selectedRomaneioId.value) {
    showMessage('Selecione o romaneio para iniciar a conferencia.');
    focusRomaneio();
    return;
  }

  startingConference.value = true;
  resetConferenceState({ keepSelection: true });

  try {
    const response = await apiService.iniciarConferenciaSeparacao(Number(selectedRomaneioId.value));
    currentRomaneio.value = response.data || null;
    await loadSnapshot();
  } catch (error) {
    console.error(error);
    showMessage(extractErrorMessage(error, 'Falha ao iniciar a conferencia do romaneio.'));
  } finally {
    startingConference.value = false;
  }
};

const confirmarQuantidade = async (quantidade) => {
  if (!currentRomaneio.value?.romaneioId || !currentItem.value) {
    return;
  }

  confirmingItem.value = true;

  try {
    const response = await apiService.confirmarItemConferenciaSeparacao(currentRomaneio.value.romaneioId, {
      quantidadeInformada: quantidade
    });

    await applySnapshot(response.data || {});
  } catch (error) {
    console.error(error);
    showMessage(extractErrorMessage(error, 'Falha ao confirmar a quantidade do item.'));
  } finally {
    confirmingItem.value = false;
  }
};

const submitQuantidade = async () => {
  if (!currentItem.value) {
    return;
  }

  const quantidade = Number(quantidadeInformada.value ?? 0);
  if (Number.isNaN(quantidade) || quantidade < 0) {
    showMessage('Informe uma quantidade valida.');
    focusQuantidade();
    return;
  }

  if (quantidade === 0) {
    zeroDialog.value = true;
    return;
  }

  await confirmarQuantidade(quantidade);
};

const confirmarZero = async () => {
  zeroDialog.value = false;
  await confirmarQuantidade(0);
};

const buscarNovoRomaneio = async () => {
  conferenceFinished.value = false;
  finishMessage.value = '';
  await iniciarConferencia();
};

const voltar = async () => {
  if (hasConferenceContext.value) {
    const released = await releaseConference();
    if (!released) {
      return;
    }

    resetConferenceState({ keepSelection: true });
    await loadRomaneios();
    focusRomaneio();
    return;
  }

  if (conferenceFinished.value) {
    resetConferenceState({ keepSelection: true });
    focusRomaneio();
    return;
  }

  router.push({ name: 'Expedicao' });
};

async function confirmLogout() {
  confirmationdialog.value = false;

  if (hasConferenceContext.value) {
    const released = await releaseConference();
    if (!released) {
      confirmationdialog.value = true;
      return;
    }

    resetConferenceState({ keepSelection: true });
  }

  logout();
}

onBeforeRouteLeave(async () => {
  if (releasingConference.value) {
    return true;
  }

  if (!hasConferenceContext.value) {
    return true;
  }

  const released = await releaseConference({ silent: true });
  if (released) {
    resetConferenceState({ keepSelection: true });
    await loadRomaneios();
  }

  return released;
});

watch(dialog, (open, wasOpen) => {
  if (!open && wasOpen) {
    if (currentItem.value) {
      focusQuantidade();
      return;
    }

    if (!hasConferenceContext.value) {
      focusRomaneio();
    }
  }
});

onMounted(async () => {
  await loadRomaneios();
  focusRomaneio();
});
</script>

<template>
  <v-container class="conference-screen py-2">
    <v-row dense>
      <v-col cols="12">
        <div class="text-center screen-title">{{ headerTitle }}</div>
      </v-col>
    </v-row>

    <template v-if="!hasConferenceContext && !conferenceFinished">
      <v-row dense class="mt-1">
        <v-col cols="12">
          <v-autocomplete
            ref="romaneioInput"
            label="Romaneio Nr"
            :items="romaneioOptions"
            item-title="displayName"
            item-value="id"
            v-model="selectedRomaneioId"
            :loading="loadingRomaneios"
            density="compact"
            hide-no-data
            hide-details
            no-data-text="Nenhum romaneio encontrado"
            @update:model-value="handleRomaneioSelection"
          />
        </v-col>
      </v-row>
    </template>

    <template v-if="currentItem">
      <v-row dense class="mt-1">
        <v-col cols="4">
          <v-alert color="indigo-darken-2" variant="flat" density="compact" class="info-box">
            <div class="text-caption font-weight-bold">Zona</div>
            <div class="text-h6 font-weight-bold">{{ currentItem.zona || '-' }}</div>
          </v-alert>
        </v-col>
        <v-col cols="8">
          <v-alert color="blue-grey-darken-2" variant="flat" density="compact" class="info-box">
            <div class="text-caption font-weight-bold">Item Nr</div>
            <div class="text-h6 font-weight-bold">{{ currentItem.itemNr }}</div>
          </v-alert>
        </v-col>
      </v-row>

      <v-row dense class="mt-1">
        <v-col cols="12">
          <v-textarea
            label="Descricao"
            :model-value="currentItem.descricao"
            rows="2"
            auto-grow="false"
            density="compact"
            readonly
            hide-details
          />
        </v-col>
      </v-row>

      <v-row dense class="mt-1">
        <v-col cols="4">
          <v-alert color="blue-lighten-1" variant="flat" density="compact" class="small-box">
            <div class="text-caption font-weight-bold">Pedido</div>
            <div class="text-h6 font-weight-bold">{{ currentItem.quantidadePedido }}</div>
          </v-alert>
        </v-col>
        <v-col cols="4">
          <v-alert color="green-lighten-1" variant="flat" density="compact" class="small-box">
            <div class="text-caption font-weight-bold">Conferida</div>
            <div class="text-h6 font-weight-bold">{{ currentItem.quantidadeConferida }}</div>
          </v-alert>
        </v-col>
        <v-col cols="4">
          <v-alert color="amber-darken-2" variant="flat" density="compact" class="small-box">
            <div class="text-caption font-weight-bold">Faltante</div>
            <div class="text-h6 font-weight-bold">{{ currentItem.quantidadeFaltante }}</div>
          </v-alert>
        </v-col>
      </v-row>

      <v-row dense class="mt-1">
        <v-col cols="12">
          <v-text-field
            ref="quantidadeInput"
            label="Quantidade conferida"
            type="number"
            min="0"
            step="1"
            v-model="quantidadeInformada"
            density="compact"
            hide-details
            :disabled="confirmingItem || loadingSnapshot"
            @keyup.enter.prevent="submitQuantidade"
          />
        </v-col>
      </v-row>

      <v-row dense class="mt-2">
        <v-col cols="12">
          <v-btn
            block
            color="green-darken-1"
            variant="elevated"
            :loading="confirmingItem"
            :disabled="confirmingItem || loadingSnapshot"
            @click="submitQuantidade"
          >
            Confirmar
          </v-btn>
        </v-col>
      </v-row>

      <v-row dense class="mt-2">
        <v-col cols="12">
          <v-table density="compact" class="items-table">
            <thead>
              <tr>
                <th class="text-left">Zona</th>
                <th class="text-left">Item</th>
                <th class="text-right">Ped</th>
                <th class="text-right">Conf</th>
                <th class="text-right">Falta</th>
              </tr>
            </thead>
            <tbody>
              <tr
                v-for="item in itens"
                :key="`${item.zonaId}-${item.itemNr}-${item.descricao}`"
                :class="{ 'row-current': item.atual, 'row-search': item.emBusca }"
              >
                <td>{{ item.zona || '-' }}</td>
                <td>{{ item.itemNr }}</td>
                <td class="text-right">{{ item.quantidadePedido }}</td>
                <td class="text-right">{{ item.quantidadeConferida }}</td>
                <td class="text-right">{{ item.quantidadeFaltante }}</td>
              </tr>
            </tbody>
          </v-table>
        </v-col>
      </v-row>
    </template>

    <template v-if="conferenceFinished">
      <v-row dense class="mt-2">
        <v-col cols="12">
          <v-alert type="success" variant="tonal" density="compact">
            {{ finishMessage || 'Conferencia finalizada com sucesso.' }}
          </v-alert>
        </v-col>
      </v-row>

      <v-row dense class="mt-2">
        <v-col cols="12">
          <v-btn
            block
            color="green-darken-1"
            variant="elevated"
            :loading="startingConference"
            :disabled="startingConference || !selectedRomaneioId"
            @click="buscarNovoRomaneio"
          >
            Buscar novo romaneio
          </v-btn>
        </v-col>
        <v-col cols="12" class="mt-1">
          <v-btn
            block
            color="primary"
            variant="outlined"
            @click="resetConferenceState({ keepSelection: false })"
          >
            Trocar romaneio
          </v-btn>
        </v-col>
      </v-row>
    </template>

    <v-bottom-navigation grow class="mt-2 compact-nav">
      <v-btn label="Voltar" class="active-btn" @click="voltar">
        <v-icon>mdi-arrow-left</v-icon>
        <span>Voltar</span>
      </v-btn>

      <v-btn label="Menu" class="active-btn" :to="{ name: 'Home' }">
        <v-icon>mdi-home</v-icon>
        <span>Menu</span>
      </v-btn>

      <v-btn label="Sair" class="active-btn" @click="confirmationdialog = true">
        <v-icon>mdi-logout</v-icon>
        <span>Sair</span>
      </v-btn>
    </v-bottom-navigation>

    <v-dialog v-model="dialog" max-width="420" persistent>
      <v-card>
        <v-card-text class="py-5 text-center">
          {{ dialogMessage }}
        </v-card-text>
        <v-card-actions class="justify-center pb-4">
          <v-btn color="primary" variant="elevated" @click="dialog = false">
            Fechar
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <v-dialog v-model="zeroDialog" max-width="420" persistent>
      <v-card>
        <v-card-text class="py-5 text-center">
          Confirma quantidade zero para este item?
        </v-card-text>
        <v-card-actions class="justify-center pb-4">
          <v-btn color="green-darken-1" variant="elevated" @click="confirmarZero">
            Sim
          </v-btn>
          <v-btn color="red-accent-4" variant="elevated" @click="zeroDialog = false">
            Nao
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <v-dialog v-model="confirmationdialog" max-width="420" persistent>
      <v-card>
        <v-card-text class="py-5 text-center">
          Tem certeza de que deseja sair?
        </v-card-text>
        <v-card-actions class="justify-center pb-4">
          <v-btn color="green-darken-1" variant="elevated" @click="confirmLogout">
            Sim
          </v-btn>
          <v-btn color="red-accent-4" variant="elevated" @click="confirmationdialog = false">
            Nao
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </v-container>
</template>

<style scoped>
.conference-screen {
  max-width: 420px;
}

.screen-title {
  font-size: 1rem;
  font-weight: 700;
}

.active-btn {
  text-transform: none;
}

.active-btn :deep(span) {
  font-size: 0.75rem;
}

.info-box,
.small-box {
  color: white;
  min-height: 72px;
}

.items-table :deep(th),
.items-table :deep(td) {
  padding: 4px 6px !important;
  font-size: 12px;
}

.items-table :deep(.v-table__wrapper) {
  max-height: 220px;
  overflow-y: auto;
}

.row-current {
  background: #e3f2fd;
}

.row-search {
  background: #fff3e0;
}

.compact-nav {
  min-height: 56px;
}
</style>

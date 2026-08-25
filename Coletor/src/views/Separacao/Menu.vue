<script setup>
import { computed, nextTick, onMounted, ref, watch } from 'vue';
import { logout } from '@/router';
import { onBeforeRouteLeave, useRouter } from 'vue-router';
import apiService from '../../http/request.js';

const router = useRouter();

const confirmationdialog = ref(false);
const dialog = ref(false);
const dialogMessage = ref('');
const loadingZones = ref(false);
const assumingTask = ref(false);
const loadingLine = ref(false);
const confirmingLine = ref(false);
const skippingLine = ref(false);
const releasingTask = ref(false);

const zones = ref([]);
const selectedZoneId = ref(null);

const currentTask = ref(null);
const currentLine = ref(null);
const taskFinished = ref(false);
const finishMessage = ref('');

const locationConfirmation = ref('');
const quantityConfirmation = ref(null);

const zoneInput = ref(null);
const locationInput = ref(null);
const quantityInput = ref(null);

const showMessage = (message) => {
  dialogMessage.value = message;
  dialog.value = true;
};

const setFocus = (elRef) => {
  nextTick(() => {
    elRef?.value?.focus?.();
  });
};

const focusZone = () => setFocus(zoneInput);
const focusLocation = () => setFocus(locationInput);
const focusQuantity = () => setFocus(quantityInput);

const zoneOptions = computed(() =>
  zones.value.map((zone) => ({
    ...zone,
    displayName: zone.tarefasPendentes > 0
      ? `${zone.nome} - ${zone.descricao || ''} (${zone.tarefasPendentes})`
      : `${zone.nome} - ${zone.descricao || ''}`.trim().replace(/\s-\s$/, '')
  }))
);

const hasTaskContext = computed(() => !!currentTask.value);
const selectedZoneHeader = computed(() => {
  const zone = zones.value.find((item) => String(item.id) === String(selectedZoneId.value));
  if (!zone) {
    return 'Separação';
  }

  const descricao = (zone.descricao || '').trim();
  return descricao ? `Separação - ${descricao}` : 'Separação';
});

const loadZones = async () => {
  loadingZones.value = true;
  try {
    const response = await apiService.obterZonasSeparacao();
    zones.value = Array.isArray(response.data) ? response.data : [];

    if (selectedZoneId.value && !zones.value.some((zone) => String(zone.id) === String(selectedZoneId.value))) {
      selectedZoneId.value = null;
    }
  } catch (error) {
    console.error(error);
    showMessage('Falha ao carregar as zonas disponíveis.');
  } finally {
    loadingZones.value = false;
  }
};

const clearLineInputs = () => {
  locationConfirmation.value = '';
  quantityConfirmation.value = null;
};

const resetTaskState = ({ keepZone = true } = {}) => {
  currentTask.value = null;
  currentLine.value = null;
  taskFinished.value = false;
  finishMessage.value = '';
  clearLineInputs();

  if (!keepZone) {
    selectedZoneId.value = null;
  }
};

const extractErrorMessage = (error, fallback) => {
  return error?.response?.data?.mensagem
    || error?.response?.data?.message
    || error?.response?.data?.title
    || error?.response?.data?.Title
    || fallback;
};

const canReleaseTask = () => {
  return !!currentTask.value?.tarefaNr && !taskFinished.value;
};

const releaseCurrentTask = async ({ silent = false } = {}) => {
  if (!canReleaseTask()) {
    return true;
  }

  if (releasingTask.value) {
    return true;
  }

  releasingTask.value = true;

  try {
    await apiService.liberarTarefaSeparacao(currentTask.value.tarefaNr);
    return true;
  } catch (error) {
    console.error(error);
    if (!silent) {
      showMessage(extractErrorMessage(error, 'Falha ao liberar a tarefa em separação.'));
    }
    return false;
  } finally {
    releasingTask.value = false;
  }
};

const loadCurrentLine = async () => {
  if (!currentTask.value?.tarefaNr) {
    return;
  }

  loadingLine.value = true;
  try {
    const response = await apiService.obterLinhaAtualSeparacao(currentTask.value.tarefaNr);
    const payload = response.data || {};

    if (payload.finalizada || !payload.linha) {
      currentLine.value = null;
      taskFinished.value = true;
      finishMessage.value = payload.mensagem || 'Separação finalizada com sucesso.';
      await loadZones();
      return;
    }

    currentLine.value = payload.linha;
    taskFinished.value = false;
    finishMessage.value = '';
    clearLineInputs();
    focusLocation();
  } catch (error) {
    console.error(error);
    showMessage(extractErrorMessage(error, 'Falha ao carregar a linha atual da tarefa.'));
  } finally {
    loadingLine.value = false;
  }
};

const iniciarSeparacao = async () => {
  if (!selectedZoneId.value) {
    showMessage('Selecione a zona para iniciar a separação.');
    focusZone();
    return;
  }

  assumingTask.value = true;
  resetTaskState({ keepZone: true });

  try {
    const response = await apiService.assumirTarefaSeparacao(Number(selectedZoneId.value));
    currentTask.value = response.data || null;
    await loadCurrentLine();
  } catch (error) {
    console.error(error);
    showMessage(extractErrorMessage(error, 'Nenhuma tarefa disponível para a zona selecionada.'));
  } finally {
    assumingTask.value = false;
  }
};

const normalizeCode = (value) => (value || '').toString().replaceAll('.', '').replaceAll(' ', '').trim().toUpperCase();

const validarLocacao = () => {
  if (!currentLine.value) {
    return;
  }

  const informado = normalizeCode(locationConfirmation.value);
  if (!informado) {
    return;
  }

  const esperado = normalizeCode(currentLine.value.locacao);
  if (informado !== esperado) {
    showMessage('Locação informada diferente da locação da tarefa.');
    locationConfirmation.value = '';
    focusLocation();
    return;
  }

  focusQuantity();
};

const confirmarLinha = async () => {
  if (!currentTask.value?.tarefaNr || !currentLine.value) {
    return;
  }

  const locacaoInformada = locationConfirmation.value?.trim();
  const quantidadeInformada = Number(quantityConfirmation.value || 0);

  if (!locacaoInformada) {
    showMessage('Informe a locação para confirmação.');
    focusLocation();
    return;
  }

  if (!quantidadeInformada || quantidadeInformada <= 0) {
    showMessage('Informe uma quantidade válida.');
    focusQuantity();
    return;
  }

  confirmingLine.value = true;

  try {
    const response = await apiService.confirmarLinhaSeparacao(currentTask.value.tarefaNr, {
      locacaoInformada,
      quantidadeInformada
    });

    const payload = response.data || {};
    if (payload.finalizada) {
      currentLine.value = null;
      taskFinished.value = true;
      finishMessage.value = payload.mensagem || 'Separação finalizada com sucesso.';
      clearLineInputs();
      await loadZones();
      return;
    }

    currentLine.value = payload.proximaLinha || null;
    clearLineInputs();
    await loadZones();
    focusLocation();
  } catch (error) {
    console.error(error);
    showMessage(extractErrorMessage(error, 'Falha ao confirmar a linha da tarefa.'));
  } finally {
    confirmingLine.value = false;
  }
};

const passbyLinha = async () => {
  if (!currentTask.value?.tarefaNr || !currentLine.value) {
    return;
  }

  skippingLine.value = true;

  try {
    const response = await apiService.passbyLinhaSeparacao(currentTask.value.tarefaNr);
    const payload = response.data || {};

    if (payload.finalizada) {
      currentLine.value = null;
      taskFinished.value = true;
      finishMessage.value = payload.mensagem || 'Separação finalizada com sucesso.';
      clearLineInputs();
      await loadZones();
      return;
    }

    currentLine.value = payload.proximaLinha || null;
    clearLineInputs();
    focusLocation();
  } catch (error) {
    console.error(error);
    showMessage(extractErrorMessage(error, 'Falha ao fazer passby da linha.'));
  } finally {
    skippingLine.value = false;
  }
};

const buscarNovaTarefa = async () => {
  taskFinished.value = false;
  finishMessage.value = '';
  await iniciarSeparacao();
};

const voltar = async () => {
  if (hasTaskContext.value) {
    const released = await releaseCurrentTask();
    if (!released) {
      return;
    }
    resetTaskState({ keepZone: true });
    await loadZones();
    focusZone();
    return;
  }

  if (taskFinished.value) {
    resetTaskState({ keepZone: true });
    focusZone();
    return;
  }

  router.push({ name: 'Home' });
};

async function confirmLogout() {
  confirmationdialog.value = false;

  if (hasTaskContext.value) {
    const released = await releaseCurrentTask();
    if (!released) {
      confirmationdialog.value = true;
      return;
    }

    resetTaskState({ keepZone: true });
  }

  logout();
}

onBeforeRouteLeave(async () => {
  if (releasingTask.value) {
    return true;
  }

  if (!hasTaskContext.value) {
    return true;
  }

  const released = await releaseCurrentTask({ silent: true });
  if (released) {
    resetTaskState({ keepZone: true });
    await loadZones();
  }

  return released;
});

watch(dialog, (open, wasOpen) => {
  if (!open && wasOpen) {
    if (currentLine.value) {
      if (!locationConfirmation.value) {
        focusLocation();
      } else {
        focusQuantity();
      }
      return;
    }

    if (!hasTaskContext.value) {
      focusZone();
    }
  }
});

onMounted(async () => {
  await loadZones();
  focusZone();
});
</script>

<template>
  <v-container class="separacao-screen py-2">
    <v-row dense>
      <v-col cols="12">
        <div class="text-center screen-title">{{ selectedZoneHeader }}</div>
      </v-col>
    </v-row>

    <template v-if="!hasTaskContext && !taskFinished">
      <v-row dense>
        <v-col cols="12">
          <v-autocomplete
            ref="zoneInput"
            label="Zona"
            :items="zoneOptions"
            item-title="displayName"
            item-value="id"
            v-model="selectedZoneId"
            :loading="loadingZones"
            density="compact"
            hide-no-data
            hide-details
            no-data-text="Nenhuma zona encontrada"
            @keyup.enter.prevent="iniciarSeparacao"
          />
        </v-col>
      </v-row>

      <v-row dense class="mt-2">
        <v-col cols="12">
          <v-btn
            block
            color="green-darken-1"
            variant="elevated"
            :loading="assumingTask"
            :disabled="assumingTask || !selectedZoneId"
            @click="iniciarSeparacao"
          >
            Iniciar separação
          </v-btn>
        </v-col>
      </v-row>
    </template>

    <template v-if="currentLine">
      <v-row dense class="mt-1">
        <v-col cols="8">
          <v-alert color="red-darken-2" variant="flat" density="compact" class="locacao-highlight">
            <div class="text-caption font-weight-bold info-label">Locação</div>
            <div class="text-h6 font-weight-bold info-value">{{ currentLine.locacao }}</div>
          </v-alert>
        </v-col>
        <v-col cols="4">
          <v-alert color="amber-darken-2" variant="flat" density="compact" class="quantidade-highlight">
            <div class="text-caption font-weight-bold info-label">Qtde</div>
            <div class="text-h5 font-weight-bold info-value">{{ currentLine.quantidadePendente }}</div>
          </v-alert>
        </v-col>
      </v-row>

      <v-row dense class="mt-1">
        <v-col cols="12">
          <v-text-field label="Item Nr" :model-value="currentLine.itemNr" density="compact" readonly hide-details />
        </v-col>
      </v-row>

      <v-row dense class="mt-1">
        <v-col cols="12">
          <v-textarea
            label="Descrição"
            :model-value="currentLine.descricao"
            rows="2"
            auto-grow="false"
            density="compact"
            readonly
            hide-details
          />
        </v-col>
      </v-row>

      <v-row dense class="mt-1">
        <v-col cols="12">
          <v-text-field
            ref="locationInput"
            label="Confirmar locação"
            v-model="locationConfirmation"
            density="compact"
            :disabled="confirmingLine || loadingLine || skippingLine"
            hide-details
            @keyup.enter.prevent="validarLocacao"
            @change="validarLocacao"
          />
        </v-col>
      </v-row>

      <v-row dense class="mt-1">
        <v-col cols="12">
          <v-text-field
            ref="quantityInput"
            label="Quantidade separada"
            type="number"
            min="1"
            step="1"
            v-model="quantityConfirmation"
            density="compact"
            :disabled="confirmingLine || loadingLine || skippingLine"
            hide-details
            @keyup.enter.prevent="confirmarLinha"
          />
        </v-col>
      </v-row>

      <v-row dense class="mt-2">
        <v-col cols="12">
          <v-btn
            block
            color="green-darken-1"
            variant="elevated"
            :loading="confirmingLine"
            :disabled="confirmingLine || loadingLine || skippingLine"
            @click="confirmarLinha"
          >
            Confirmar
          </v-btn>
        </v-col>
      </v-row>
    </template>

    <template v-if="taskFinished">
      <v-row dense class="mt-2">
        <v-col cols="12">
          <v-alert type="success" variant="tonal" density="compact">
            {{ finishMessage || 'Separação finalizada com sucesso.' }}
          </v-alert>
        </v-col>
      </v-row>

      <v-row dense class="mt-2">
        <v-col cols="12">
          <v-btn
            block
            color="green-darken-1"
            variant="elevated"
            :loading="assumingTask"
            :disabled="assumingTask || !selectedZoneId"
            @click="buscarNovaTarefa"
          >
            Buscar nova tarefa
          </v-btn>
        </v-col>
        <v-col cols="12" class="mt-1">
          <v-btn
            block
            color="primary"
            variant="outlined"
            @click="resetTaskState({ keepZone: false })"
          >
            Trocar zona
          </v-btn>
        </v-col>
      </v-row>
    </template>

    <v-bottom-navigation grow class="mt-2 compact-nav">
      <v-btn label="Voltar" class="active-btn" @click="voltar">
        <v-icon>mdi-arrow-left</v-icon>
        <span>Voltar</span>
      </v-btn>

      <v-btn
        v-if="currentLine"
        label="Passby"
        class="active-btn"
        :disabled="skippingLine || confirmingLine || loadingLine"
        @click="passbyLinha"
      >
        <v-icon>mdi-skip-next</v-icon>
        <span>Passby</span>
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
            Não
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </v-container>
</template>

<style scoped>
.separacao-screen {
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

.locacao-highlight {
  border-width: 2px;
  color: white;
  min-height: 72px;
}

.quantidade-highlight {
  border-width: 2px;
  color: white;
  min-height: 72px;
}

.info-label {
  line-height: 1.1;
}

.info-value {
  line-height: 1.1;
}

.compact-nav {
  min-height: 56px;
}
</style>

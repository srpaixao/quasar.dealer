<script setup>
import { reactive, ref, nextTick, onMounted, computed, watch } from 'vue';
import { logout } from '@/router';
import apiService from '../../http/request.js';
import { useAuthStore } from '@/stores/authStore.js';
import { formatDateTimeBr } from '@/utils/date.js';

const authStore = useAuthStore();
const user = authStore.getUser;

// Dialogs
const confirmationdialog = ref(false);
const dialog = ref(false);
const dialogMessage = ref('');

const setDialog = (msg) => {
  dialogMessage.value = msg;
  dialog.value = true;
};

// Estado de formulário
const selectedTransportadoraId = ref('');
const transportadoras = ref([]);
const veiculo = ref('');
const responsavel = ref('');
const volume = ref('');

const volumesResumo = reactive({
  pendentes: 0,
  lidos: 0, // confirmados
  total: 0
});

const formLocked = ref(false);
const lastTransportadoraId = ref('');
const veiculoConfirmado = ref(false);
const responsavelConfirmado = ref(false);

const selectedTransportadora = computed(() => {
  return transportadoras.value.find(
    t => String(t.id) === String(selectedTransportadoraId.value)
  ) || null;
});


// Exibição condicional de campos
const showResumo = computed(() => !!selectedTransportadoraId.value);

const showVeiculoField = computed(
  () => !!selectedTransportadoraId.value || formLocked.value
);
const showResponsavelField = computed(
  () => formLocked.value || veiculoConfirmado.value
);
const showVolumeField = computed(
  () => formLocked.value || responsavelConfirmado.value
);

// Refs de input
const transportadoraInput = ref(null);
const veiculoInput = ref(null);
const responsavelInput = ref(null);
const volumeInput = ref(null);

// Modal de volumes
const volumesModal = reactive({
  open: false,
  titulo: '',
  lista: [],
  loading: false
});

const lidosModal = reactive({
  open: false,
  titulo: '',
  lista: [],
  loading: false
});

const volumesFilter = ref('');
const lidosFilter = ref('');
const pendentesScrollTop = ref(null);
const pendentesScrollBody = ref(null);
const pendentesTableRef = ref(null);
const lidosScrollTop = ref(null);
const lidosScrollBody = ref(null);
const lidosTableRef = ref(null);
const pendentesScrollWidth = ref(0);
const lidosScrollWidth = ref(0);

const filteredVolumes = computed(() => {
  const term = String(volumesFilter.value ?? '').trim().toLowerCase();
  if (!term) return volumesModal.lista;

  return volumesModal.lista.filter((item) => {
    const haystack = [
      item.Numero,
      item.Controle,
      item.QtdVolumes,
      item.NomeCliente,
      item.Cidade,
      item.Estado
    ]
      .map((value) => String(value ?? '').toLowerCase())
      .join(' ');

    return haystack.includes(term);
  });
});

const filteredLidos = computed(() => {
  const term = String(lidosFilter.value ?? '').trim().toLowerCase();
  if (!term) return lidosModal.lista;

  return lidosModal.lista.filter((item) => {
    const haystack = [
      item.NotaFiscalNr,
      item.VolumeNr,
      item.Veiculo,
      item.Responsavel,
      item.NomeCliente,
      item.Cidade,
      item.Estado,
      item.CriadoEm,
      item.CriadoPor
    ]
      .map((value) => String(value ?? '').toLowerCase())
      .join(' ');

    return haystack.includes(term);
  });
});

const attachedScroll = new WeakSet();

const resolveScrollEl = (tableRefOrInstance) => {
  const instance = tableRefOrInstance?.value ?? tableRefOrInstance;
  return instance?.$el?.querySelector?.('.v-table__wrapper') || null;
};

const syncScroll = (sourceRef, tableRefOrInstance) => {
  const source = sourceRef?.value ?? sourceRef;
  const target = resolveScrollEl(tableRefOrInstance);
  if (!source || !target) return;
  const left = source.scrollLeft;
  if (target.scrollLeft === left) return;
  target.scrollLeft = left;
};

const attachBodyScroll = (scrollEl, topRef) => {
  if (!scrollEl || attachedScroll.has(scrollEl)) return;
  scrollEl.addEventListener('scroll', () => {
    const topEl = topRef.value;
    if (!topEl) return;
    if (topEl.scrollLeft !== scrollEl.scrollLeft) {
      topEl.scrollLeft = scrollEl.scrollLeft;
    }
  });
  attachedScroll.add(scrollEl);
};

const updatePendentesScroll = () => {
  nextTick(() => {
    const scrollEl = resolveScrollEl(pendentesTableRef);
    pendentesScrollWidth.value = scrollEl?.scrollWidth ?? 0;
    if (pendentesScrollTop.value && scrollEl) {
      pendentesScrollTop.value.scrollLeft = scrollEl.scrollLeft;
    }
    attachBodyScroll(scrollEl, pendentesScrollTop);
  });
};

const updateLidosScroll = () => {
  nextTick(() => {
    const scrollEl = resolveScrollEl(lidosTableRef);
    lidosScrollWidth.value = scrollEl?.scrollWidth ?? 0;
    if (lidosScrollTop.value && scrollEl) {
      lidosScrollTop.value.scrollLeft = scrollEl.scrollLeft;
    }
    attachBodyScroll(scrollEl, lidosScrollTop);
  });
};

const normalizePendentesItem = (item) => ({
  Numero: item?.Numero ?? item?.numero ?? '',
  Controle: item?.Controle ?? item?.controle ?? '',
  QtdVolumes: item?.QtdVolumes ?? item?.qtdVolumes ?? '',
  NomeCliente: item?.NomeCliente ?? item?.nomeCliente ?? '',
  Cidade: item?.Cidade ?? item?.cidade ?? '',
  Estado: item?.Estado ?? item?.estado ?? ''
});

const normalizeLidosItem = (item) => ({
  NotaFiscalNr: item?.NotaFiscalNr ?? item?.notaFiscalNr ?? '',
  VolumeNr: item?.VolumeNr ?? item?.volumeNr ?? '',
  Veiculo: item?.Veiculo ?? item?.veiculo ?? '',
  Responsavel: item?.Responsavel ?? item?.responsavel ?? '',
  NomeCliente: item?.NomeCliente ?? item?.nomeCliente ?? '',
  Cidade: item?.Cidade ?? item?.cidade ?? '',
  Estado: item?.Estado ?? item?.estado ?? '',
  CriadoEm: item?.CriadoEm ?? item?.criadoEm ?? '',
  CriadoPor: item?.CriadoPor ?? item?.criadoPor ?? ''
});

async function abrirPendentesModal() {
  if (!selectedTransportadoraId.value) {
    setDialog('Selecione a transportadora.');
    focusTransportadora();
    return;
  }

  volumesModal.open = true;
  volumesModal.titulo = `Pendentes (${volumesResumo.pendentes})`;
  volumesModal.lista = [];
  volumesModal.loading = true;
  volumesFilter.value = '';

  try {
    const { data } = await apiService.obterPendentesExpedicao(
      selectedTransportadoraId.value,
      user.filialId
    );
    const lista = Array.isArray(data)
      ? data
      : (data?.lista ?? data?.volumes ?? []);
    volumesModal.lista = lista.map(normalizePendentesItem);
  } catch (e) {
    console.error(e);
    volumesModal.lista = [];
    setDialog('Não foi possível obter a lista de volumes pendentes.');
  } finally {
    volumesModal.loading = false;
    updatePendentesScroll();
  }
}

async function abrirLidosModal() {
  if (!selectedTransportadoraId.value) {
    setDialog('Selecione a transportadora.');
    focusTransportadora();
    return;
  }

  lidosModal.open = true;
  lidosModal.titulo = `Conferidos (${volumesResumo.lidos})`;
  lidosModal.lista = [];
  lidosModal.loading = true;
  lidosFilter.value = '';

  try {
    const { data } = await apiService.obterLidosExpedicao(
      selectedTransportadoraId.value,
      user.filialId
    );
    const lista = Array.isArray(data)
      ? data
      : (data?.lista ?? data?.volumes ?? []);
    lidosModal.lista = lista.map(normalizeLidosItem);
  } catch (e) {
    console.error(e);
    lidosModal.lista = [];
    setDialog('Não foi possível obter a lista de volumes conferidos.');
  } finally {
    lidosModal.loading = false;
    updateLidosScroll();
  }
}

// Helpers de foco
const focusElement = (elRef) => {
  nextTick(() => {
    elRef?.value?.focus?.();
  });
};

const focusTransportadora = () => focusElement(transportadoraInput);
const focusVeiculo = () => focusElement(veiculoInput);
const focusResponsavel = () => focusElement(responsavelInput);
const focusVolume = () => {
  if (!showVolumeField.value) return;
  focusElement(volumeInput);
};

// Funções auxiliares
function confirmLogout() {
  logout();
}

function resetVolumeField({ focus = true } = {}) {
  volume.value = '';
  if (focus) focusVolume();
}

function parseVolume(raw) {
  const cleaned = String(raw || '').replace(/\D/g, '');
  if (cleaned.length < 12) return null;

  const notaFiscal = cleaned.slice(0, 9);
  const volumeNr = cleaned.slice(9);

  if (!/^[0-9]{9}$/.test(notaFiscal) || !/^[0-9]+$/.test(volumeNr)) return null;

  return { notaFiscal, volumeNr };
}

const normalizeVolumeNr = (valor) => {
  const str = String(valor || '').trim();
  const normalized = str.replace(/^0+/, '');
  return normalized === '' ? '0' : normalized;
};

// API – resumo por transportadora
async function GetVolumesPorTransportadora(transportadoraId) {
  volumesResumo.pendentes = 0;
  volumesResumo.lidos = 0;
  volumesResumo.total = 0;

  if (!transportadoraId) return;

  try {
    const { data } = await apiService.getResumoExpedicao(transportadoraId, user.filialId);
    volumesResumo.total = Number(data?.total ?? 0);
    volumesResumo.pendentes = Number(data?.pendentes ?? 0);
    volumesResumo.lidos = Number(
      data?.lidos ?? data?.confirmados ?? 0 // cobre nome diferente
    );
  } catch (e) {
    console.error(e);
    setDialog('Não foi possível obter o histórico de volumes da transportadora.');
  }
}

// Eventos de formulário
function handleTransportadoraChange(value) {
  if (formLocked.value) {
    nextTick(() => {
      selectedTransportadoraId.value = lastTransportadoraId.value;
    });
    return;
  }

  lastTransportadoraId.value = value || '';
  veiculo.value = '';
  responsavel.value = '';
  veiculoConfirmado.value = false;
  responsavelConfirmado.value = false;
  volumesModal.open = false;
  volumesModal.titulo = '';
  volumesModal.lista = [];
  volumesFilter.value = '';
  lidosModal.open = false;
  lidosModal.titulo = '';
  lidosModal.lista = [];
  lidosFilter.value = '';
  resetVolumeField({ focus: false });

  GetVolumesPorTransportadora(value);

  if (value) {
    focusVeiculo();
  } else {
    focusTransportadora();
  }
}

function confirmVeiculo(event) {
  if (formLocked.value) return;

  const valor = veiculo.value.trim();
  if (!valor) return;

  if (!veiculoConfirmado.value) {
    veiculoConfirmado.value = true;

    if (event?.type === 'keydown' || event?.type === 'keyup') {
      event.preventDefault?.();
    }

    nextTick(() => focusResponsavel());
  }
}

function confirmResponsavel(event) {
  if (formLocked.value) return;

  const valor = responsavel.value.trim();
  if (!valor) return;

  if (!responsavelConfirmado.value) {
    responsavelConfirmado.value = true;

    if (event?.type === 'keydown' || event?.type === 'keyup') {
      event.preventDefault?.();
    }

    nextTick(() => focusVolume());
  }
}

// Processamento de volume
async function processVolume() {

  if (!selectedTransportadoraId.value) {
    setDialog('Selecione a transportadora.');
    focusTransportadora();
    return;
  }

  const raw = (volume.value || '').trim();
  if (!raw) return;

  let parsed;
  //if (selectedTransportadora.value?.EmitirEtiqueta) {
  parsed = parseVolume(raw);
  if (!parsed) {
    setDialog('Volume incorreto!');
    resetVolumeField();
    return;
  }
  //} 
  // else {
  //   parsed = {
  //     notaFiscal: raw,
  //     volumeNr: '001'
  //   };
  // }

  const veiculoAtual = veiculo.value.trim();
  if (!veiculoAtual) {
    setDialog('Informe a identificação do veículo.');
    focusVeiculo();
    return;
  }

  const responsavelAtual = responsavel.value.trim();
  if (!responsavelAtual) {
    setDialog('Informe o responsável.');
    focusResponsavel();
    return;
  }

  const { notaFiscal, volumeNr } = parsed;

  try {
    const response = await apiService.obterDocumentoExpedicao(
      notaFiscal,
      selectedTransportadoraId.value
    );

    const doc = response.data || [];

    if (
      !doc ||
      String(doc.numero) !== String(notaFiscal) ||
      String(doc.transportadoraId) !== String(selectedTransportadoraId.value)
    ) {
      setDialog(notaFiscal + '<br><br>Volume incorreto!');
      resetVolumeField();
      return;
    }

    const qtdVolumesNF = Number(doc.qtdVolumes ?? doc.QtdVolumes ?? 0);
    if (!qtdVolumesNF) {
      setDialog(
        notaFiscal + '<br><br>Não foram encontrados volumes para esta nota fiscal.'
      );
      resetVolumeField();
      return;
    }

    const t = transportadoras.value.find(
      (x) => String(x.id) === String(selectedTransportadoraId.value)
    );

    const { data } = await apiService.getHistoricoVolumes(notaFiscal, user.filialId);
    const volumes = Array.isArray(data?.volumes) ? data.volumes : [];
    const volumesRegistrados = volumes.map((v) => String(v));

    const duplicado = volumesRegistrados.some(
      (v) => normalizeVolumeNr(v) === normalizeVolumeNr(volumeNr)
    );

    if (duplicado) {
      setDialog(
        notaFiscal + '/' + volumeNr + '<br><br>Volume já registrado para esta NF.'
      );
      resetVolumeField();
      return;
    }

    if (volumesRegistrados.length >= qtdVolumesNF) {
      setDialog(
        notaFiscal +
        '/' +
        volumeNr +
        '<br><br>Todos os volumes da nota fiscal ' +
        notaFiscal +
        ' já foram conferidos!'
      );
      resetVolumeField();
      return;
    }

    const payload = {
      notaFiscalNr: notaFiscal,
      volumeNr: volumeNr,
      transportadoraId: selectedTransportadoraId.value,
      transportadoraNome: t?.nome || '',
      criadopor: user?.account,
      veiculo: veiculoAtual,
      responsavel: responsavelAtual
    };

    await apiService.postHistoricoDespacho(payload);
    await GetVolumesPorTransportadora(selectedTransportadoraId.value);

    if (!formLocked.value) {
      formLocked.value = true;
      lastTransportadoraId.value = selectedTransportadoraId.value || '';
    }

    resetVolumeField();
  } catch (err) {
    if (err?.response?.status === 404) {
      setDialog('Volume incorreto!');
    } else {
      setDialog('Falha ao processar volume.');
      console.log(err);
    }
    resetVolumeField();
  }
}

function handleSubmit() {
  processVolume();
}

// function resetForm() {
//   formLocked.value = false;
//   lastTransportadoraId.value = '';
//   selectedTransportadoraId.value = '';
//   veiculo.value = '';
//   responsavel.value = '';
//   veiculoConfirmado.value = false;
//   responsavelConfirmado.value = false;
//   resetVolumeField({ focus: false });

//   GetVolumesPorTransportadora(null);
//   focusTransportadora();
// }

// Watchers
watch(filteredVolumes, () => {
  if (volumesModal.open) updatePendentesScroll();
});

watch(filteredLidos, () => {
  if (lidosModal.open) updateLidosScroll();
});

watch(
  () => volumesModal.open,
  (open) => {
    if (open) updatePendentesScroll();
  }
);

watch(
  () => lidosModal.open,
  (open) => {
    if (open) updateLidosScroll();
  }
);

watch(veiculo, () => {
  if (formLocked.value) return;

  if (veiculoConfirmado.value) {
    veiculoConfirmado.value = false;
    responsavelConfirmado.value = false;
    responsavel.value = '';
    resetVolumeField({ focus: false });
  }
});

watch(responsavel, () => {
  if (formLocked.value) return;

  if (responsavelConfirmado.value) {
    responsavelConfirmado.value = false;
    resetVolumeField({ focus: false });
  }
});

// Lifecycle
onMounted(async () => {
  try {
    const response = await apiService.obterTransportadorasExpedicao(user.filialId);
    transportadoras.value = response.data || [];
  } catch {
    setDialog('Falha ao carregar transportadoras.');
  }

  focusTransportadora();
});
</script>

<template>
  <v-container>
    <v-row dense>
      <v-col cols="12" md="6" lg="4">
        <div class="text-center">Expedição / Conferir Volume</div>
      </v-col>
    </v-row>

    <v-row dense>
      <v-col cols="12" md="6" lg="4">
        <v-autocomplete ref="transportadoraInput" label="Transportadora" :items="transportadoras" item-title="nome"
          item-value="id" v-model="selectedTransportadoraId" :disabled="formLocked" :clearable="!formLocked"
          @update:modelValue="handleTransportadoraChange" prepend-inner-icon="mdi-magnify" hide-no-data hide-details
          no-data-text="Nenhuma transportadora encontrada" />
      </v-col>
    </v-row>

    <v-row dense v-if="showResumo">
      <v-col cols="12">
        <v-list>
          <v-list-item class="bg-orange-lighten-1 my-1 rounded cursor-pointer" height="20"
            @click="abrirPendentesModal">
            <template #default>
              <div class="d-flex justify-space-between w-100">
                <span class="font-weight-medium text-white">Pendentes</span>
                <span class="font-weight-bold text-white">
                  {{ volumesResumo.pendentes }}
                </span>
              </div>
            </template>
          </v-list-item>

          <v-list-item class="bg-green-lighten-1 my-1 rounded cursor-pointer" height="20"
            @click="abrirLidosModal">
            <template #default>
              <div class="d-flex justify-space-between w-100">
                <span class="font-weight-medium">Conferidos</span>
                <span class="font-weight-bold text-white">
                  {{ volumesResumo.lidos }}
                </span>
              </div>
            </template>
          </v-list-item>

          <v-list-item class="bg-blue-lighten-1 my-1 rounded" height="20">
            <template #default>
              <div class="d-flex justify-space-between w-100">
                <span class="font-weight-medium">Total</span>
                <span class="font-weight-bold text-white">
                  {{ volumesResumo.total }}
                </span>
              </div>
            </template>
          </v-list-item>
        </v-list>
      </v-col>
    </v-row>


    <v-row dense v-if="showVeiculoField">
      <v-col cols="12" md="6" lg="4">
        <v-text-field ref="veiculoInput" label="Veiculo" v-model="veiculo" :disabled="formLocked"
          @keyup.enter.prevent="confirmVeiculo($event)" @keydown.tab="confirmVeiculo($event)" />
      </v-col>
    </v-row>

    <v-row dense v-if="showResponsavelField">
      <v-col cols="12" md="6" lg="4">
        <v-text-field ref="responsavelInput" label="Responsavel" v-model="responsavel" :disabled="formLocked"
          @keyup.enter.prevent="confirmResponsavel($event)" @keydown.tab="confirmResponsavel($event)" />
      </v-col>
    </v-row>

    <v-row dense v-if="showVolumeField">
      <v-col cols="12" md="6" lg="4">
        <v-text-field ref="volumeInput" label="Volume Nr" v-model="volume" @keyup.enter.prevent="handleSubmit"
          @blur="handleSubmit" placeholder="Ex.: 123456789001" />
      </v-col>
    </v-row>


    <v-dialog v-model="volumesModal.open" max-width="420">
      <v-card>
        <v-card-title class="text-h6 modal-title-tight">
          {{ volumesModal.titulo }}
        </v-card-title>
        <v-card-text class="modal-card-text-tight">
          <v-text-field
            v-if="volumesModal.lista.length"
            v-model="volumesFilter"
            density="compact"
            variant="outlined"
            hide-details
            clearable
            placeholder="Filtrar..."
            prepend-inner-icon="mdi-magnify"
            class="mb-2"
          />
          <div v-if="volumesModal.loading">Carregando...</div>
          <div v-else-if="!filteredVolumes.length">Nenhum volume encontrado.</div>
          <div v-else>
            <div ref="pendentesScrollTop" class="table-scroll-top"
              @scroll="syncScroll(pendentesScrollTop, pendentesTableRef)">
              <div :style="{ width: `${pendentesScrollWidth}px` }" class="table-scroll-spacer"></div>
            </div>
            <div ref="pendentesScrollBody" class="pendentes-table-wrapper">
              <v-table ref="pendentesTableRef" density="compact" class="pendentes-table pendentes-table--pendentes">
              <thead>
                <tr>
                  <th class="text-left">NF</th>
                  <th class="text-left">Ctrl</th>
                  <th class="text-right">Vol</th>
                  <th class="text-left">Cliente</th>
                  <th class="text-left">Cidade</th>
                  <th class="text-left">UF</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="(item, index) in filteredVolumes" :key="`${item.Numero}-${item.Controle}-${index}`">
                  <td class="text-no-wrap">{{ item.Numero }}</td>
                  <td class="text-no-wrap">{{ item.Controle }}</td>
                  <td class="text-right">{{ item.QtdVolumes }}</td>
                  <td class="cell-truncate">{{ item.NomeCliente }}</td>
                  <td class="cell-truncate">{{ item.Cidade }}</td>
                  <td class="text-no-wrap">{{ item.Estado }}</td>
                </tr>
              </tbody>
              </v-table>
            </div>
          </div>
        </v-card-text>
        <v-card-actions>
          <v-btn block color="primary" variant="elevated" @click="volumesModal.open = false">
            Fechar
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <v-dialog v-model="lidosModal.open" max-width="540">
      <v-card>
        <v-card-title class="text-h6 modal-title-tight">
          {{ lidosModal.titulo }}
        </v-card-title>
        <v-card-text class="modal-card-text-tight">
          <v-text-field v-if="lidosModal.lista.length" v-model="lidosFilter" density="compact" variant="outlined"
            hide-details clearable placeholder="Filtrar..." prepend-inner-icon="mdi-magnify" class="mb-2" />
          <div v-if="lidosModal.loading">Carregando...</div>
          <div v-else-if="!filteredLidos.length">Nenhum volume encontrado.</div>
          <div v-else>
            <div ref="lidosScrollTop" class="table-scroll-top"
              @scroll="syncScroll(lidosScrollTop, lidosTableRef)">
              <div :style="{ width: `${lidosScrollWidth}px` }" class="table-scroll-spacer"></div>
            </div>
            <div ref="lidosScrollBody" class="pendentes-table-wrapper">
              <v-table ref="lidosTableRef" density="compact" class="pendentes-table pendentes-table--lidos">
              <thead>
                <tr>
                  <th class="text-left">NF</th>
                  <th class="text-left">Vol</th>
                  <th class="text-left">Veículo</th>
                  <th class="text-left">Resp.</th>
                  <th class="text-left">Cliente</th>
                  <th class="text-left">Cidade</th>
                  <th class="text-left">UF</th>
                  <th class="text-left">Data</th>
                  <th class="text-left">Usuário</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="(item, index) in filteredLidos"
                  :key="`${item.NotaFiscalNr}-${item.VolumeNr}-${index}`">
                  <td class="text-no-wrap">{{ item.NotaFiscalNr }}</td>
                  <td class="text-no-wrap">{{ item.VolumeNr }}</td>
                  <td class="cell-truncate">{{ item.Veiculo }}</td>
                  <td class="cell-truncate">{{ item.Responsavel }}</td>
                  <td class="cell-truncate">{{ item.NomeCliente }}</td>
                  <td class="cell-truncate">{{ item.Cidade }}</td>
                  <td class="text-no-wrap">{{ item.Estado }}</td>
                  <td class="text-no-wrap">{{ formatDateTimeBr(item.CriadoEm) }}</td>
                  <td class="cell-truncate">{{ item.CriadoPor }}</td>
                </tr>
              </tbody>
              </v-table>
            </div>
          </div>
        </v-card-text>
        <v-card-actions>
          <v-btn block color="primary" variant="elevated" @click="lidosModal.open = false">
            Fechar
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <v-bottom-navigation grow>
      <v-btn label="Voltar" class="active-btn" :to="{ name: 'Expedicao' }">
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

    <!-- Mensagens (popup) -->
    <v-dialog v-model="dialog" max-width="500" persistent>
      <v-card>
        <v-card-text v-html="dialogMessage"></v-card-text>
        <v-card-actions class="mx-auto">
          <v-row justify="center">
            <v-btn class="bg-red-accent-4 small-font" variant="elevated" block @click="dialog = false">
              <v-icon left>mdi-close</v-icon>
              Fechar
            </v-btn>
          </v-row>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <!-- Dialog de confirmação -->
    <v-dialog v-model="confirmationdialog" max-width="500" persistent>
      <v-card>
        <v-card-text>Tem certeza de que deseja sair?</v-card-text>
        <v-card-actions class="mx-auto">
          <v-row justify="center" class="mb-5">
            <v-col>
              <v-btn color="green-darken-1" variant="elevated" block @click="confirmLogout">
                <v-icon left>mdi-check</v-icon>
                Sim
              </v-btn>
            </v-col>
            <v-col>
              <v-btn class="bg-red-accent-4" variant="elevated" block @click="confirmationdialog = false">
                <v-icon left>mdi-close</v-icon>
                Não
              </v-btn>
            </v-col>
          </v-row>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </v-container>
</template>

<style scoped>
.small-font {
  font-size: 75% !important;
}

.cursor-pointer {
  cursor: pointer;
}

.pendentes-table-wrapper {
  max-height: 260px;
  overflow-x: hidden;
  overflow-y: auto;
}

.table-scroll-top {
  overflow-x: scroll;
  overflow-y: hidden;
  height: 18px;
  margin-bottom: 4px;
}

.table-scroll-spacer {
  height: 1px;
}

.table-scroll-top::-webkit-scrollbar,
.pendentes-table :deep(.v-table__wrapper)::-webkit-scrollbar {
  height: 14px;
}

.table-scroll-top::-webkit-scrollbar-thumb,
.pendentes-table :deep(.v-table__wrapper)::-webkit-scrollbar-thumb {
  background: #9e9e9e;
  border-radius: 8px;
}

.table-scroll-top::-webkit-scrollbar-track,
.pendentes-table :deep(.v-table__wrapper)::-webkit-scrollbar-track {
  background: #e0e0e0;
  border-radius: 8px;
}

.pendentes-table :deep(.v-table__wrapper)::-webkit-scrollbar-thumb,
.pendentes-table :deep(.v-table__wrapper)::-webkit-scrollbar-track {
  background: transparent;
  border-radius: 0;
}

.pendentes-table :deep(.v-table__wrapper)::-webkit-scrollbar {
  height: 8px;
}

.modal-title-tight {
  padding-bottom: 4px !important;
}

.modal-card-text-tight {
  padding-top: 8px !important;
}

.pendentes-table :deep(th),
.pendentes-table :deep(td) {
  padding: 4px 6px !important;
  font-size: 12px;
  line-height: 1.1;
}

.pendentes-table :deep(.v-table__wrapper) {
  overflow-x: auto;
}

.pendentes-table :deep(table) {
  min-width: 100%;
  width: max-content;
  table-layout: fixed;
}

.pendentes-table--pendentes :deep(th:nth-child(1)),
.pendentes-table--pendentes :deep(td:nth-child(1)) {
  width: 88px;
}

.pendentes-table--pendentes :deep(th:nth-child(2)),
.pendentes-table--pendentes :deep(td:nth-child(2)) {
  width: 80px;
}

.pendentes-table--pendentes :deep(th:nth-child(3)),
.pendentes-table--pendentes :deep(td:nth-child(3)) {
  width: 48px;
}

.pendentes-table--pendentes :deep(th:nth-child(4)),
.pendentes-table--pendentes :deep(td:nth-child(4)) {
  width: 150px;
}

.pendentes-table--pendentes :deep(th:nth-child(5)),
.pendentes-table--pendentes :deep(td:nth-child(5)) {
  width: 96px;
}

.pendentes-table--pendentes :deep(th:nth-child(6)),
.pendentes-table--pendentes :deep(td:nth-child(6)) {
  width: 40px;
}

.pendentes-table--lidos :deep(th:nth-child(1)),
.pendentes-table--lidos :deep(td:nth-child(1)) {
  width: 88px;
}

.pendentes-table--lidos :deep(th:nth-child(2)),
.pendentes-table--lidos :deep(td:nth-child(2)) {
  width: 52px;
}

.pendentes-table--lidos :deep(th:nth-child(3)),
.pendentes-table--lidos :deep(td:nth-child(3)) {
  width: 96px;
}

.pendentes-table--lidos :deep(th:nth-child(4)),
.pendentes-table--lidos :deep(td:nth-child(4)) {
  width: 96px;
}

.pendentes-table--lidos :deep(th:nth-child(5)),
.pendentes-table--lidos :deep(td:nth-child(5)) {
  width: 150px;
}

.pendentes-table--lidos :deep(th:nth-child(6)),
.pendentes-table--lidos :deep(td:nth-child(6)) {
  width: 96px;
}

.pendentes-table--lidos :deep(th:nth-child(7)),
.pendentes-table--lidos :deep(td:nth-child(7)) {
  width: 40px;
}

.pendentes-table--lidos :deep(th:nth-child(8)),
.pendentes-table--lidos :deep(td:nth-child(8)) {
  width: 140px;
}

.pendentes-table--lidos :deep(th:nth-child(9)),
.pendentes-table--lidos :deep(td:nth-child(9)) {
  width: 120px;
  padding-left: 10px !important;
}

.pendentes-table :deep(th) {
  font-weight: 600;
}

.cell-truncate {
  max-width: 120px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
</style>

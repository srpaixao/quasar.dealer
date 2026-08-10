<script setup>
import { reactive, ref, nextTick, onMounted, watch } from 'vue';
import { logout } from '@/router';
import { useRouter } from 'vue-router';
import apiService from '../../http/request.js';

import { useAuthStore } from '@/stores/authStore.js';
import { useClienteApiStore } from '@/stores/clienteApiStore.js';

const authStore = useAuthStore();
const clienteApiStore = useClienteApiStore();
const user = authStore.getUser;
const router = useRouter();

const form = reactive({
  itemnr: '',
  descricao: '',
  locacaoOrigem: '',
  locacaoDestino: '',
  qtdOrigem: 0,
  locacaoDestinoConf: '',
  qtdDestino: null,
});

const dialog = ref(false);
const dialogMessage = ref('');
const resetDialogOnClose = ref(false);
const confirmationdialog = ref(false);
const loading = ref(false);
const processing = ref(false);

const itemFound = ref(false);
const destinoValidado = ref(false);
const movimentacaoId = ref(null);

const itemInput = ref(null);
const destinoInput = ref(null);
const quantidadeInput = ref(null);

const setFocus = (elRef) => {
  nextTick(() => {
    elRef?.value?.focus?.();
  });
};

const focusItem = () => setFocus(itemInput);
const focusDestino = () => setFocus(destinoInput);
const focusQuantidade = () => setFocus(quantidadeInput);

const showMessage = (message, options = {}) => {
  const resetOnClose = typeof options === 'boolean' ? options : !!options.resetOnClose;
  resetDialogOnClose.value = resetOnClose;
  dialogMessage.value = message;
  dialog.value = true;
};

const clearItemData = () => {
  form.descricao = '';
  form.locacaoOrigem = '';
  form.locacaoDestino = '';
  form.qtdOrigem = 0;
  itemFound.value = false;
  movimentacaoId.value = null;
};

const resetForm = () => {
  form.itemnr = '';
  clearItemData();
  form.locacaoDestinoConf = '';
  form.qtdDestino = null;
  destinoValidado.value = false;
  loading.value = false;
  processing.value = false;
  movimentacaoId.value = null;
  focusItem();
};

const normalizarCodigo = (valor) => (valor || '').toString().replaceAll('.', '').replaceAll(' ', '').trim().toUpperCase();

const consultarMovimentacao = async () => {
  const item = form.itemnr?.trim();
  if (!item) {
    showMessage('Informe o número do item.');
    return;
  }

  loading.value = true;
  clearItemData();
  destinoValidado.value = false;
  form.locacaoDestinoConf = '';
  form.qtdDestino = null;

  try {
    const resp = await apiService.consultarMovimentacao(item, user.filialId);
    const data = resp.data;
    console.log(data)
    form.descricao = data.descricao?.trim() || '-';
    form.locacaoOrigem = data.locacaoOrigem?.trim() || '';
    form.locacaoDestino = data.locacaoDestino?.trim() || '';
    form.qtdOrigem = data.qtdOrigem ?? 0;
    movimentacaoId.value = data.id ?? null;
    itemFound.value = true;
    nextTick(focusDestino);
  } catch (err) {
    const status = err.response?.status;

    const serverMsg =
      err.response?.data?.mensagem ||
      err.response?.data?.message ||
      err.response?.data?.Title ||
      err.response?.data?.title ||
      err.response?.data?.Detail ||
      err.response?.data?.detail ||
      null;

    if (status === 404) {
      showMessage(serverMsg || 'Item não encontrado.', { resetOnClose: true });
    } else if (status) {
      showMessage(serverMsg || `Erro ${status}. Tente novamente.`, { resetOnClose: true });
    } else {
      showMessage('Erro de comunicação. Verifique sua conexão.', { resetOnClose: true });
    }

  } finally {
    loading.value = false;
  }
};

const validarDestino = () => {
  const destinoLido = normalizarCodigo(form.locacaoDestinoConf);
  const destinoEsperado = normalizarCodigo(form.locacaoDestino);

  if (!destinoLido) return;

  if (destinoEsperado && destinoLido !== destinoEsperado) {
    showMessage('Locação incorreta. Verifique e tente novamente.');
    form.locacaoDestinoConf = '';
    destinoValidado.value = false;
    nextTick(focusDestino);
    return;
  }

  // Destino ok
  destinoValidado.value = true;
  nextTick(focusQuantidade);
};

const confirmarMovimentacao = async () => {
  if (!itemFound.value || !destinoValidado.value) return;
  if (!movimentacaoId.value) {
    showMessage('Identificador da movimentação indisponível. Consulte o item novamente.');
    return;
  }
  const qtde = Number(form.qtdDestino || 0);
  if (!qtde || qtde <= 0) {
    showMessage('Informe uma quantidade válida.');
    nextTick(focusQuantidade);
    return;
  }

  processing.value = true;

  const payload = {
    id: movimentacaoId.value,
    itemNr: form.itemnr.trim(),
    destinoMov: form.locacaoDestinoConf?.trim() || form.locacaoDestino?.trim() || '',
    qtdeMov: qtde,
    usuarioMov: user?.account ?? null,
    dataHoraMov: new Date().toISOString(),
  };
  console.log(payload)

  try {
    // Enviar dados para o DMS
    // const dmsPayload = {
    //   id_coletor: "QUASAR",
    //   // data_coleta: formatDateToIsoOffset(payload.dataHoraMov),
    //   data_coleta: "2025-12-03T18:20:00-03:00",
    //   operador: "BRANCO",
    //   cnpj: "72855505000300",
    //   codigo_produto: payload.itemNr,
    //   prateleira: payload.destinoMov
    // };
    // console.log('Chamar DMS SERCON => Payload:', dmsPayload)
    // const dmsResponse = await apiService.enviarDMS(dmsPayload);
    // console.log('DMS SERCON Response =>', dmsResponse)

    // if (dmsResponse?.status !== 200) {
    //   const status = dmsResponse?.status ?? 'desconhecido';
    //   const serverMsg =
    //     dmsResponse?.data?.mensagem ||
    //     dmsResponse?.data?.message ||
    //     dmsResponse?.data?.Title ||
    //     dmsResponse?.data?.title ||
    //     dmsResponse?.data?.Detail ||
    //     dmsResponse?.data?.detail ||
    //     null;

    //   showMessage(serverMsg || `Falha ao enviar dados para o DMS (status ${status}).`);
    //   return;
    // }

    console.log("Finalizar movimentação!",payload)

    // Finalizar movimentação
    const quasarPayload = {
      id: payload.id,
      LocacaoDestino: payload.destinoMov,
      QtdDestino: payload.qtdeMov,
      FinalizadoPor: payload.usuarioMov,
      UrlDMS: clienteApiStore.getBaseApi + '/registrar-movimento' || '',
      Payload: JSON.stringify(payload),
      //Response: JSON.stringify(dmsResponse?.data ?? dmsResponse),
      FilialId: user?.filialId
    };
    console.log('Finalizar Payload', quasarPayload)
    await apiService.finalizarMovimentacao(quasarPayload);
    console.log('OK!')
    resetForm();

  } catch (error) {
    const msg = error.response?.data?.mensagem || 'Erro ao registrar a movimentação. Tente novamente.';
    showMessage(msg);
  } finally {
    //const msg = error.response?.data?.mensagem || 'Transferência efetuada!';
    processing.value = false;
  }
  //showMessage(msg);
};

const cancelar = () => {
  router.push({ name: 'Estoque' });
};

const confirmLogout = () => logout();

watch(dialog, (isOpen, wasOpen) => {
  if (!isOpen && wasOpen) {
    if (resetDialogOnClose.value) {
      resetDialogOnClose.value = false;
      resetForm();
      return;
    }

    // Foco adequado apÃ³s fechar mensagem
    if (!itemFound.value) focusItem();
    else if (!destinoValidado.value) focusDestino();
    else focusQuantidade();
  }
});

onMounted(() => {
  focusItem();
});

// Placeholders para o modal de ocorrÃªncia (mantido do layout)
const reportDialog = ref(false);
const reportMessage = ref('');
const submitReport = () => { reportDialog.value = false; reportMessage.value = ''; };
const closeReport = () => { reportDialog.value = false; };

function formatDateToIsoOffset(dateString) {
  const date = new Date(dateString.replace(' ', 'T')); // cria Date válido
  const offsetMinutes = -3 * 60; // -03:00 em minutos
  const offsetHours = String(Math.floor(offsetMinutes / 60)).padStart(2, '0');
  const offsetSign = offsetMinutes < 0 ? '-' : '+';

  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  const hours = String(date.getHours()).padStart(2, '0');
  const minutes = String(date.getMinutes()).padStart(2, '0');
  const seconds = String(date.getSeconds()).padStart(2, '0');

  return `${year}-${month}-${day}T${hours}:${minutes}:${seconds}${offsetSign}${offsetHours}:00`;
}


</script>

<template>
  <v-container>
    <v-row dense>
      <v-col cols="12" md="6" lg="4">
        <div class="text-center">Estoque / Transferir</div>
      </v-col>
    </v-row>

    <v-row dense>
      <v-col cols="12" md="6" lg="4">
        <v-text-field ref="itemInput" label="Item" v-model="form.itemnr" density="comfortable" outlined
          hide-details="auto" :disabled="processing" :loading="loading" @keyup.enter.prevent="consultarMovimentacao"
          @change="consultarMovimentacao" />
      </v-col>
    </v-row>

    <v-row dense v-if="itemFound" class="mt-2">
      <v-col cols="12" md="6" lg="4">
        <v-text-field label="DescriÃ§Ã£o" v-model="form.descricao" density="comfortable" outlined readonly
          hide-details="auto" />
      </v-col>
      <!-- <v-col cols="12" md="6" lg="4">
        <v-text-field label="Locação Origem" v-model="form.locacaoOrigem" density="comfortable" outlined readonly
          hide-details="auto" />
      </v-col>
      <v-col cols="12" md="6" lg="4">
        <v-text-field label="Qtd Origem" v-model="form.qtdOrigem" density="comfortable" outlined readonly
          hide-details="auto" />
      </v-col> -->
      <v-col cols="12" md="6" lg="4">
        <v-text-field label="Locação Destino" v-model="form.locacaoDestino" density="comfortable" outlined readonly
          hide-details="auto" />
      </v-col>
    </v-row>

    <v-row dense v-if="itemFound" class="mt-2">
      <v-col cols="12" md="6" lg="4">
        <v-text-field ref="destinoInput" label="Confirmar Locação" v-model="form.locacaoDestinoConf"
          density="comfortable" outlined hide-details="auto" :disabled="processing || destinoValidado"
          @keyup.enter.prevent="validarDestino" @change="validarDestino" />
      </v-col>
    </v-row>

    <v-row dense class="mt-2" v-if="destinoValidado">
      <v-col cols="12" md="3" lg="2">
        <v-text-field ref="quantidadeInput" type="number" min="1" step="1" label="Quantidade" v-model="form.qtdDestino"
          density="comfortable" outlined hide-details="auto" :disabled="processing"
          @keyup.enter.prevent="confirmarMovimentacao" />
      </v-col>
    </v-row>

    <v-row dense class="mt-4" v-if="destinoValidado">
      <v-col cols="12" sm="6" md="3" lg="2">
        <v-btn color="green-darken-1" class="w-100" :loading="processing" :disabled="processing || !form.qtdDestino"
          @click="confirmarMovimentacao">
          Confirmar
        </v-btn>
      </v-col>
      <!-- <v-col cols="12" sm="6" md="3" lg="2">
        <v-btn color="red-accent-4" class="w-100" :disabled="processing" @click="cancelar">
          Cancelar
        </v-btn>
      </v-col> -->
    </v-row>


    <v-bottom-navigation grow class="mt-6">
      <v-btn label="Voltar" class="active-btn" :to="{ name: 'Estoque' }">
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

    <!-- Dialog de confirmaÃ§Ã£o -->
    <v-dialog v-model="confirmationdialog" max-width="400" persistent>
      <v-card>
        <v-card-text class="py-5 text-center"> Tem certeza de que deseja sair? </v-card-text>
        <v-card-actions class="justify-center pb-4">
          <v-btn color="green-darken-1" variant="elevated" @click="confirmLogout">
            <v-icon left>mdi-check</v-icon> Sim
          </v-btn>
          <v-btn color="red-accent-4" variant="elevated" @click="confirmationdialog = false">
            <v-icon left>mdi-close</v-icon> NÃ£o
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <!-- Form para reportar aocorrÃªncia -->
    <v-dialog v-model="reportDialog" max-width="600px">
      <v-card>
        <v-card-title class="headline text-center">Reportar OcorrÃªncia</v-card-title>
        <v-card-text>
          <v-select label="Tipo"
            :items="['OcorrÃªncia 1', 'OcorrÃªncia 2', 'OcorrÃªncia 3', 'OcorrÃªncia 4', 'OcorrÃªncia 5', 'Outros']"></v-select>
          <v-textarea label="ObservaÃ§Ãµes" v-model="reportMessage" outlined rows="5"></v-textarea>
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

.active-btn {
  text-transform: none;
}

.w-100 {
  width: 100%;
}
</style>

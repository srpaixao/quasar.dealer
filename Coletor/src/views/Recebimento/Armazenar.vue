<script setup>
import { reactive, ref, nextTick, onMounted, watch } from 'vue';
import { logout } from '@/router';
import apiService from '../../http/request.js';

import { useAuthStore } from '@/stores/authStore.js';

const authStore = useAuthStore();
const user = authStore.getUser;
// console.log(user)
// console.log(user.value)
// console.log('account', user.account)
// console.log('fullName', user.fullName)
// console.log('email', user.email)

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
const material = ref(null);
const itemFound = ref(false);
const locacaoOK = ref(false);

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

const validarMaterial = async () => {

  if (form.itemnr.trim().length === 0) {
    return;
  }

  form.descricao = '';
  form.locacao = '';
  itemFound.value = false;
  loading.value = true;

  try {
    const response = await apiService.validarMaterial(form.itemnr);
    if (response.data.locacao === '') {
      loading.value = false;
      dialogMessage = "Não existe locação cadastrada para este item";
      dialog.value = true;
    } else {
      loading.value = false;
      material.value = response.data;
      form.descricao = response.data.descricao;
      form.locacao = material.value.locacaoFormatada;
      itemFound.value = true;
      focusLocacaoConfirmada();
    }
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

const validarLocacao = async () => {

  if (form.locacaoconfirmada.trim().length === 0) {
    return;
  }

  //console.log('Locação:', form.locacao.toUpperCase())

  locacaoOK.value = false;

  var locform = form.locacaoconfirmada.replace(/[\s.]/g, '');
  //console.log('Locação formatada:', locform.toUpperCase())

  if (form.locacao.toUpperCase() == locform.toUpperCase()) {
    locacaoOK.value = true;
    focusQuantidade();
  } else {

    // Gravar histórico
    const historico = {
      ItemNr: form.itemnr,
      descricao: form.descricao,
      Locacao: material.value.locacao,
      LocacaoConfirmada: form.locacaoconfirmada,
      Quantidade: form.quantidade ?? 0,
      Erro: true,
      Mensagem: "Locação incorreta",
      Usuario: user.account,
      FilialId: user.filialId
    };
    const response = await apiService.gravarHistorico(historico);
    //console.log(response.data);

    locacaoOK.value = false;
    dialogMessage = 'A locação informada está incorreta';
    dialog.value = true;
  }
}

const armazenarMaterial = async () => {

  // Quantidade informada tem que ser numérico e maior que 0
  if (!form.quantidade || isNaN(parseInt(form.quantidade, 10)) || parseInt(form.quantidade, 10) < 1) {
    form.quantidade = null;
    focusQuantidade();
    return;
  }

  processing.value = true;

  // Validar a quantidade informada
  try {
    const response = await apiService.validarQuantidade(form.itemnr);
    if (parseInt(form.quantidade, 10) > parseInt(response.data.quantidade, 10)) {
      const excesso = parseInt(form.quantidade, 10) - parseInt(response.data.quantidade, 10);
      dialogMessage = `${excesso} peça(s) do item ${form.itemnr} deve(m) retornar ao recebimento!`;
      dialog.value = true;

      // Gravar historico caso a quantidade esteja incorreta
      try {
        const historico = {
          ItemNr: form.itemnr,
          descricao: form.descricao,
          Locacao: material.value.locacao,
          LocacaoConfirmada: form.locacaoconfirmada,
          Quantidade: form.quantidade ?? 0,
          Erro: true,
          Mensagem: `Excesso na armazenagem (${excesso} peças)`,
          Usuario: user.account,
          FilialId: user.filialId
        };
        await apiService.gravarHistorico(historico);
      } catch (error) {
        console.log(error)
      }

      processing.value = false;
      return;
    }
  } catch (error) {
    dialogMessage = error.message || 'Erro ao validar quantidade';
    dialog.value = true;
    processing.value = true;
    return;
  }

  // Armazenar material
  try {
    await apiService.armazenarMaterial(form.itemnr, parseInt(form.quantidade, 10), user.account);

    try {
      // Gravar histórico
      const historico = {
        ItemNr: form.itemnr,
        descricao: form.descricao,
        Locacao: material.value.locacao,
        LocacaoConfirmada: form.locacaoconfirmada,
        Quantidade: form.quantidade ?? 0,
        Erro: false,
        Mensagem: "Item armazenado",
        Usuario: user.account,
        FilialId: user.filialId
      };
      await apiService.gravarHistorico(historico);
    } catch (error) {
      console.log(error)
    }

    resetForm();

  } catch (error) {
    dialogMessage = error.message || 'Erro ao armazenar material';
    dialog.value = true;
    processing.value = false;
    return;
  }
}

const locacaoconfirmadaInput = ref(null);
const focusLocacaoConfirmada = () => {
  setFocus(locacaoconfirmadaInput);
};

const quantidadeInput = ref(null);
const focusQuantidade = () => {
  setFocus(quantidadeInput);
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

  processing.value = false;

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

// watch(reportDialog, (newVal) => {
//   console.log(newVal)
//   if (!newVal) {
//     onDialogClose(newVal);
//   }
// });

// const openReportModal = () => { resetForm(); 
//   reportMessage.value = '';
//   reportDialog.value = true;
//  };

// Fechar formulário de ocorrência
// function closeReport() {
//   console.log('cancelado');
//   reportDialog.value = false;
// }

// Submeter problema reportado
// function submitReport() {
//   console.log('Problema reportado:', reportMessage.value);
//   resetForm();
// }

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
          <div class="text-center">Recebimento / Armazenar</div>
        </v-col>
      </v-row>

      <v-row dense>
        <v-col cols="12" md="6" lg="4">
          <v-text-field label="Item" v-model="form.itemnr" ref="itemInput" @blur="validarMaterial" outlined
            density="comfortable" hide-details="true">
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
              <v-text-field label="Confirmar Locação" v-model="form.locacaoconfirmada" @blur="validarLocacao"
                @input="form.locacaoconfirmada = form.locacaoconfirmada.toUpperCase()" ref="locacaoconfirmadaInput"
                outlined density="comfortable" hide-details="true">
              </v-text-field>
            </v-col>
          </v-row>
        </div>
      </div>

      <div>
        <div v-show="locacaoOK">
          <v-row dense>
            <v-col cols="12" md="6" lg="4">
              <v-text-field label="Quantidade" v-model="form.quantidade" ref="quantidadeInput" outlined
                density="comfortable" type="number">
              </v-text-field>
            </v-col>
          </v-row>

          <v-row dense>
            <v-col cols="12" md="6" lg="4" class="text-center">
              <v-btn color="green-darken-1" variant="elevated" block @click="armazenarMaterial">

                <template v-if="processing">
                  <v-row class="d-flex align-center">
                    <v-icon color="white" size="18" class="mr-2 mdi-spin">mdi-loading</v-icon>
                    <span class="authenticating-text">Processando...</span>
                  </v-row>
                </template>
                <template v-else>
                  <v-icon>mdi-check</v-icon>&nbsp;Confirmar
                </template>
              </v-btn>
            </v-col>
          </v-row>
        </div>
      </div>

    </div>

    <v-bottom-navigation grow>
      <v-btn label="Voltar" class="active-btn" :to="{ name: 'Recebimento' }">
        <v-icon>mdi-arrow-left</v-icon> <span>Voltar</span>
      </v-btn>
      <v-btn label="Menu" class="active-btn" :to="{ name: 'Home' }">
        <v-icon>mdi-home</v-icon> <span>Home</span>
      </v-btn>
      <v-btn label="Reiniciar" class="active-btn" @click="resetForm">
        <v-icon>mdi-restart</v-icon> <span>Reiniciar</span>
      </v-btn>
      <!-- <v-btn label="Ocorrência" class="alert-btn" @click="openReportModal">
        <v-icon>mdi-alert</v-icon> <span>Ocorrência</span>
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

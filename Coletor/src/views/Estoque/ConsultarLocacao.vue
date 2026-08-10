<script setup>
import { reactive, ref, nextTick, onMounted, watch, computed } from 'vue';
import { logout } from '@/router';
import apiService from '../../http/request.js';

import { useAuthStore } from '@/stores/authStore.js';

const authStore = useAuthStore();
const user = authStore.getUser;
const form = reactive({
  itemnr: '',
  tipo: '',
  curva: '',
  estrategia: '',
  area: '',
  equipamento: '',
  descricao: '',
  observacoes: '',
  bloqueado: false
});

let dialogMessage = '';
const dialog = ref(false);
const confirmationdialog = ref(false);
const locacao = ref(null);
const itemFound = ref(false);

const loading = ref(false);

const itemInput = ref(null);
const focusItem = () => {
  setFocus(itemInput);
};

const itens = ref([]);
const qtdeItens = computed(() => itens.value.length);

// Controle de visualização
const mostrarLista = ref(true)           // Mostra div com localização e botão
const itemIndex = ref(null)              // null = nenhum item sendo exibido

const itemAtual = computed(() => {
  return itemIndex.value !== null ? itens.value[itemIndex.value] : null
})

// Ações
const mostrarItem = (index) => {
  itemIndex.value = index
  mostrarLista.value = false
}

const proximoItem = () => {
  if (itemIndex.value < itens.value.length - 1) {
    itemIndex.value++
  } else {
    voltarParaLista()
  }
}

const itemAnterior = () => {
  if (itemIndex.value > 0) {
    itemIndex.value--
  } else {
    voltarParaLista()
  }
}

const voltarParaLista = () => {
  itemIndex.value = null
  mostrarLista.value = true
}


// const reportDialog = ref(false);
// const reportMessage = ref('');

const setFocus = (field) => {
  nextTick(() => {
    if (field && field.value) {
      field.value.focus();
    }
  });
};

const consultarItem = async () => {

  if (form.itemnr.trim().length === 0) {
    return;
  }

  form.descricao = '';
  form.curva = '';
  form.estrategia = '';
  form.area = '';
  form.equipamento = '';
  form.observacoes = '';
  form.tipo = '';
  form.bloqueado = false;

  itemFound.value = false;
  loading.value = true;

  try {
    const response = await apiService.consultarLocacao(form.itemnr, user.filialId);
    console.log(response.data)

    loading.value = false;
    locacao.value = response.data;
    form.descricao = response.data.descricao && response.data.descricao.trim() !== '' ? response.data.descricao : '-';
    form.tipo = response.data.tipo && response.data.tipo.trim() !== '' ? response.data.tipo : '-';
    form.curva = response.data.curva && response.data.curva.trim() !== '' ? response.data.curva : '-';
    form.estrategia = response.data.estrategia && response.data.estrategia.trim() !== '' ? response.data.estrategia : '-';
    form.area = response.data.area && response.data.area.trim() !== '' ? response.data.area : '-';
    form.equipamento = response.data.equipamento && response.data.equipamento.trim() !== '' ? response.data.equipamento : '-';
    form.observacoes = response.data.observacoes && response.data.observacoes.trim() !== '' ? response.data.observacoes : '-';
    form.bloqueado = response.data.bloqueado;
    itens.value = response.data.itens || [];
    itemFound.value = true;
    mostrarLista.value = true;
  }
  catch (error) {
    loading.value = false;
    if (error.response && error.response.data) {
      dialogMessage = error.response.data.mensagem || 'Erro ao buscar os dados da locação. Por favor, tente novamente.';
      dialog.value = true;
    }
    else {
      dialogMessage = 'Erro ao buscar os dados da locação. Por favor, tente novamente.';
      dialog.value = true;
    }
  }
  finally {
    loading.value = false;
  }

}

// Reiniciar o formulário
function resetForm() {
  form.itemnr = '';
  form.descricao = '';
  form.tipo = '';
  form.curva = '';
  form.observacoes = '';
  itemFound.value = false;
  dialogMessage = '';
  dialog.value = false;
  mostrarLista.value = false;
  focusItem();
}

const onDialogClose = (value) => {
  if (!value) {
    form.itemnr = '';
    focusItem();
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
          <div class="text-center">Estoque / Consultar Locação</div>
        </v-col>
      </v-row>

      <!-- <v-row dense>
        <v-col cols="12" md="6" lg="4">
          <v-table density="compact">
            <thead>
              <tr>
                <th class="text-center">Locação</th>
                <th class="text-center">STK 1</th>
                <th class="text-center">STK 2</th>
                <th class="text-center">Saldo</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="row in items" :key="row.item + row.locacao">
                <td class="text-center">{{ row.locacao }}</td>
                <td class="text-center">{{ row.stk1 }}</td>
                <td class="text-center">{{ row.stk2 }}</td>
                <td class="text-center">{{ row.saldo }}</td>
              </tr>
              <tr class="font-weight-bold">
                <td colspan="3" class="text-right">Saldo Total</td>
                <td class="text-center">{{ saldoTotal }}</td>
              </tr>

            </tbody>
          </v-table>
        </v-col>
      </v-row> -->

      <v-row dense>
        <v-col cols="12" md="6" lg="4">
          <v-text-field label="Locação" v-model="form.itemnr" @input="form.itemnr = form.itemnr.toUpperCase()" ref="itemInput" @blur="consultarItem" outlined
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
          <v-row dense v-if="form.bloqueado">
            <v-col cols="12" md="6" lg="4">
              <div class="d-flex justify-center">
                <span class="d-inline-flex align-center bg-error rounded px-3 py-1 text-xs">
                  <v-icon size="14" class="me-1">mdi-lock</v-icon>
                  Locação bloqueada
                </span>
              </div>
            </v-col>
          </v-row>
          <div v-if="mostrarLista">
            <v-row dense>
              <v-col cols="12" md="6" lg="4">
                <v-text-field label="Descrição" v-model="form.descricao" class="no-select" density="comfortable"
                  outlined readonly hide-details="true">
                </v-text-field>
              </v-col>
            </v-row>
            <v-row dense>
              <v-col cols="6" md="4" lg="2">
                <v-text-field label="Tipo" v-model="form.tipo" class="no-select" density="comfortable" outlined readonly
                  hide-details="true">
                </v-text-field>
              </v-col>
              <v-col cols="6" md="4" lg="2">
                <v-text-field label="Curva" v-model="form.curva" class="no-select" density="comfortable" outlined
                  readonly hide-details="true">
                </v-text-field>
              </v-col>
            </v-row>
            <v-row dense>
              <v-col cols="12" md="6" lg="4">
                <v-text-field label="Estratégia" v-model="form.estrategia" class="no-select" density="comfortable"
                  outlined readonly hide-details="true">
                </v-text-field>
              </v-col>
            </v-row>
            <v-row dense>
              <v-col cols="12" md="6" lg="4">
                <v-text-field label="Área" v-model="form.area" class="no-select" density="comfortable" outlined readonly
                  hide-details="true">
                </v-text-field>
              </v-col>
            </v-row>
            <v-row dense>
              <v-col cols="12" md="6" lg="4">
                <v-text-field label="Equipamento" v-model="form.equipamento" class="no-select" density="comfortable"
                  outlined readonly hide-details="true">
                </v-text-field>
              </v-col>
            </v-row>
            <v-row dense>
              <v-col cols="12" md="6" lg="4">
                <v-text-field label="Observações" v-model="form.observacoes" class="no-select" density="comfortable"
                  outlined readonly hide-details="true">
                </v-text-field>
              </v-col>
            </v-row>
            <v-row dense>
              <v-col cols="12" md="6" lg="4">
                <div class="center-button">
                  <v-btn @click="mostrarItem(0)" color="primary" variant="outlined">
                    <!-- Itens: {{ qtdeItens }} -->
                    Exibir Itens ({{ qtdeItens }})
                  </v-btn>
                </div>
              </v-col>
            </v-row>
          </div>
          <div v-else-if="itemAtual">
            <v-row dense>
              <v-col cols="12" md="6" lg="4">
                <v-card class="mt-1">
                  <v-card-title class="text-center text-subtitle-1 py-1">Item {{ itemIndex + 1 }} de {{
                    qtdeItens}}</v-card-title>
                  <v-card-text class="pa-1">
                    <v-text-field label="Item Nr" :model-value="itemAtual.itemNr" class="mb-2 no-select"
                      density="comfortable" outlined readonly hide-details />

                    <v-text-field label="Descrição" :model-value="itemAtual.descricao" class="mb-2 no-select"
                      density="comfortable" outlined readonly hide-details />

                    <v-text-field label="Saldo" :model-value="itemAtual.saldo" class="mb-2 no-select"
                      density="comfortable" outlined readonly hide-details />

                    <v-text-field label="Indisponível" :model-value="itemAtual.indisponivel" class="mb-2 no-select"
                      density="comfortable" outlined readonly hide-details />

                    <v-text-field label="Pedido Pendente" :model-value="itemAtual.pedidoPendente" class="mb-2 no-select"
                      density="comfortable" outlined readonly hide-details />

                    <v-row dense>
                      <v-col cols="6">
                        <v-text-field label="Curva" :model-value="itemAtual.curva" density="comfortable" outlined
                          readonly hide-details />
                      </v-col>
                      <v-col cols="6">
                        <v-text-field label="UN" :model-value="itemAtual.un && itemAtual.un.trim() !== '' ? itemAtual.un : '-'" density="comfortable" outlined readonly
                          hide-details />
                      </v-col>
                    </v-row>

                  </v-card-text>

                  <div class="d-flex justify-space-between mx-4 my-4">
                    <v-btn v-if="itemIndex > 0" @click="itemAnterior" size="small" color="primary" variant="outlined">
                      Anterior
                    </v-btn>

                    <v-spacer />

                    <v-btn v-if="itemIndex < qtdeItens - 1" @click="proximoItem" size="small" color="primary"
                      variant="outlined">
                      Próximo
                    </v-btn>
                  </div>

                </v-card>
              </v-col>
            </v-row>

            <v-row dense>
              <v-col cols="12" md="6" lg="4">
                <div class="center-button">
                  <v-btn @click="voltarParaLista" color="primary" variant="outlined">
                    Exibir dados da locação
                  </v-btn>
                </div>
              </v-col>
            </v-row>
          </div>
        </div>
      </div>

    </div>

    <v-bottom-navigation grow>
      <v-btn label="Voltar" class="active-btn" :to="{ name: 'Estoque' }">
        <v-icon>mdi-arrow-left</v-icon> <span>Voltar</span>
      </v-btn>
      <v-btn label="Menu" class="active-btn" :to="{ name: 'Home' }">
        <v-icon>mdi-home</v-icon> <span>Home</span>
      </v-btn>
      <v-btn label="Reiniciar" class="active-btn" @click="resetForm">
        <v-icon>mdi-restart</v-icon> <span>Reiniciar</span>
      </v-btn>
      <v-btn label="Sair" class="active-btn" @click="confirmationdialog = true">
        <v-icon>mdi-logout</v-icon> <span>Sair</span>
      </v-btn>
    </v-bottom-navigation>

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

.center-button {
  display: flex;
  justify-content: center;
  align-items: center;
  padding-top: 15px;
}
</style>

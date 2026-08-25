<script setup>

let ambiente = '';
if (import.meta.env.MODE != 'production') {
    ambiente = 'Ambiente: Dev'
}

import { ref, computed } from 'vue';
import apiService from '../../http/request.js';
import { APP_VERSION } from '@/config/version.js';

import { stateSession } from '../../router/index.js';

const errorMessage = computed(() => stateSession.errorMessage);
const dialogVisible = computed(() => errorMessage.value !== '');
const closeDialog = () => {
    stateSession.errorMessage = '';
};

import { useRouter } from 'vue-router';
const router = useRouter();

import { useAuthStore } from '@/stores/authStore.js';
import { useClienteApiStore } from '@/stores/clienteApiStore.js';
const authStore = useAuthStore();
const clienteApiStore = useClienteApiStore();

const form = ref(null);
const formValid = ref(false);
const apiError = ref('');
const processing = ref(false);

const username = ref('');
const usernameErrors = ref([]);
const usernameRules = [
    v => !!v || 'Username is required',
    // v => (v && v.length <= 20) || 'Username must be less than 20 characters'
];

const password = ref('');
const passwordErrors = ref([]);
const passwordRules = [
    v => !!v || 'Password is required',
    // v => (v && v.length >= 6) || 'Password must be at least 6 characters'
];

const clearError = (field) => {
    if (field === 'username') {
        usernameErrors.value = [];
    } else if (field === 'password') {
        passwordErrors.value = [];
    }
    apiError.value = '';
};

const submitLogin = async () => {
    apiError.value = '';

    const validation = await form.value.validate();
    if (validation.valid) {
        try {
            processing.value = true;
            const response = await apiService.login(username.value, password.value);
            console.log(response.data);

            authStore.setUser({
                account: response.data.useraccount,
                fullName: response.data.username,
                email: response.data.email,
                filialId: response.data.filialId
            });

            sessionStorage.setItem('quasarJWT', response.data.token);

            // clienteApiStore.setApiToken("e6c75bb6cb3c793b9033461df3a835ee");

            // try {
            //     const clienteResponse = await apiService.obterClienteApi();
            //     clienteApiStore.setConfig(clienteResponse.data);

            //     const baseApiUrl = clienteApiStore.getBaseApi;
            //     if (baseApiUrl) {
            //         try {
            //             const clienteAuthResponse = await apiService.autenticarClienteApi(baseApiUrl);
            //             const tokenApi = clienteAuthResponse?.data?.token
            //                 ?? clienteAuthResponse?.data?.Token
            //                 ?? clienteAuthResponse?.data?.accessToken
            //                 ?? null;

            //             if (tokenApi) {
            //                 clienteApiStore.setApiToken(tokenApi);
            //             } else {
            //                 console.warn('Token da API do cliente não encontrado na resposta.');
            //                 clienteApiStore.setApiToken(null);
            //             }
            //         } catch (clienteAuthError) {
            //             console.error('Falha ao autenticar na API do cliente.', clienteAuthError);
            //             clienteApiStore.setApiToken(null);
            //         }
            //     } else {
            //         console.warn('Base API do cliente não configurada.');
            //         clienteApiStore.setApiToken(null);
            //     }
            // } catch (clienteError) {
            //     const status = clienteError?.response?.status;
            //     if (status === 404) {
            //         console.warn('Configuracoes do cliente nao encontradas.');
            //         clienteApiStore.clearConfig();
            //     } else {
            //         console.error('Falha ao obter configuracoes do cliente.', clienteError);
            //         alert('Nao foi possivel carregar configuracoes do cliente');
            //     }
            // }

            console.log('successfully login! Yeah!!!!')
            router.push('/');

        } catch (error) {
            if (error.response && error.response.data) {
                apiError.value = error.response.data.mensagem || 'Login falhou. Por favor, tente novamente.';
            } else {
                apiError.value = 'Login falhou. Por favor, tente novamente.';
            }
            console.log(apiError.value)
        }
        finally {
            processing.value = false;
        }
    } else {
        usernameErrors.value = usernameRules.map(rule => rule(username.value)).filter(message => message !== true);
        passwordErrors.value = passwordRules.map(rule => rule(password.value)).filter(message => message !== true);
    }
};

</script>

<template>
    <v-container class="d-flex align-center justify-center" style="height: 100vh;">
        <v-row justify="center">
            <v-col cols="12" sm="6" md="4">
                <v-form ref="form" v-model='formValid' class="mx-auto pa-4" lazy-validation>
                    <v-card class="mx-auto">
                        <v-container>
                            <v-row justify="center">
                                <v-col>
                                    <h2 class="text-center">
                                        Quasar Dealer
                                        <div>
                                            <small class="ambiente">{{ ambiente }}</small>
                                        </div>
                                    </h2>
                                    <div class="version text-center">Versão {{ APP_VERSION }}</div>
                                </v-col>
                            </v-row>
                            <v-row>
                                <v-col>
                                    <v-text-field v-model="username" label="Usuário" prepend-inner-icon="mdi-account"
                                        variant="outlined" density="comfortable" :rules="usernameRules"
                                        :error-messages="usernameErrors" @focus="clearError('username')"
                                        autocomplete="username"></v-text-field>
                                </v-col>
                            </v-row>
                            <v-row>
                                <v-col>
                                    <v-text-field v-model="password" label="Senha" prepend-inner-icon="mdi-lock"
                                        type="password" variant="outlined" density="comfortable" :rules="passwordRules"
                                        :error-messages="passwordErrors" @focus="clearError('password')"
                                        autocomplete="current-password"></v-text-field>
                                </v-col>
                            </v-row>
                            <v-row>
                                <v-col class="text-center">
                                    <v-btn color="green-darken-1" variant="elevated" block @click="submitLogin">
                                        <template v-if="processing">
                                            <v-row class="d-flex align-center">
                                                <v-icon color="white" size="18"
                                                    class="mr-2 mdi-spin">mdi-loading</v-icon>
                                                <span class="authenticating-text">Autenticando...</span>
                                            </v-row>
                                        </template>

                                        <template v-else>
                                            <v-icon>mdi-login</v-icon>&nbsp;Login
                                        </template>
                                    </v-btn>
                                </v-col>
                            </v-row>

                            <v-row v-if="apiError" dense>
                                <v-col class="text-center">
                                    <div class="api-error">{{ apiError }}</div>
                                </v-col>
                            </v-row>
                        </v-container>
                    </v-card>
                </v-form>
                <v-dialog v-model="dialogVisible" max-width="500">
                    <v-card>
                        <v-card-text class="text-center">
                            {{ errorMessage }}
                        </v-card-text>
                        <v-card-actions class="mx-auto">
                            <v-row justify="center">
                                <v-btn class="bg-red-accent-4" variant="elevated" block @click="closeDialog"> Fechar
                                </v-btn>
                            </v-row>
                        </v-card-actions>
                    </v-card>
                </v-dialog>
            </v-col>
        </v-row>
    </v-container>
</template>

<style scoped>
h2 {
    color: #1867c0;
}

.api-error {
    color: #B00020;
    font-size: 0.9rem;
    margin-top: 10px;
    text-align: center;
}

small.ambiente {
    font-size: 60%;
    font-style: italic;
    color: tomato;
}

.version {
    margin-top: 2px;
    color: #64748b;
    font-size: 0.75rem;
}

.mdi-spin:before {
    animation: mdi-spin 1s infinite linear !important;
}
</style>

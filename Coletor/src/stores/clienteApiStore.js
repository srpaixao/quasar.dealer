import { defineStore } from 'pinia';

export const useClienteApiStore = defineStore('clienteApi', {
  state: () => ({
    baseApi: null,
    userApi: null,
    apiToken: null,
  }),
  actions: {
    setConfig(data) {
      const baseApi = data?.baseApi ?? data?.BaseApi ?? null;
      const userApi = data?.userApi ?? data?.UserApi ?? null;
      this.baseApi = baseApi;
      this.userApi = userApi;
    },
    setApiToken(token) {
      this.apiToken = token ?? null;
    },
    clearConfig() {
      this.baseApi = null;
      this.userApi = null;
      this.apiToken = null;
    },
  },
  getters: {
    getBaseApi: (state) => state.baseApi,
    getUserApi: (state) => state.userApi,
    getApiToken: (state) => state.apiToken,
  },
  persist: {
    enabled: true,
  },
});

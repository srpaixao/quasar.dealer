// stores/auth.js
import { defineStore } from 'pinia';

export const useAuthStore = defineStore('auth', {
  state: () => ({
    user: {
      account: null,
      fullName: null,
      email: null,
      filialId: null,
      filialName: null,
    },
  }),
  actions: {
    setUser(data) {
      this.user = data;
    },
    clearUser() {
      this.user = {
        account: null,
        fullName: null,
        email: null,
        filialId: null,
        filialName: null,
      };
    },
  },
  getters: {
    getUser: (state) => state.user
  },
  persist: {
     enabled: true, // Habilita a persistência 
  },
});

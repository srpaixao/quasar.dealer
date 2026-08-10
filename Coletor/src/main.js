import { createApp } from 'vue'
import { createPinia } from 'pinia';
import piniaPluginPersistedstate from 'pinia-plugin-persistedstate'

import router from './router';

// Vuetify
import 'vuetify/styles'
import { createVuetify } from 'vuetify'
import * as components from 'vuetify/components'
import * as directives from 'vuetify/directives'

// Global styles
import './style.css'

// Material Design icons
import '@mdi/font/css/materialdesignicons.min.css'

console.log('Ambiente atual:', import.meta.env.MODE);
console.log('Base URL da API:', import.meta.env.VITE_API_BASE_URL);

// Components
import App from './App.vue'

const vuetify = createVuetify({
    components,
    directives,
    defaults: {
      VBottomNavigation: {
        app: true,
      }
    }
  })

const app = createApp(App);
const pinia = createPinia();
pinia.use(piniaPluginPersistedstate);

app.use(pinia);
app.use(router);
app.use(vuetify);

app.mount('#app');



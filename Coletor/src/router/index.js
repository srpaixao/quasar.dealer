import { createRouter, createWebHistory } from 'vue-router';
import { reactive } from 'vue';
import { useClienteApiStore } from '@/stores/clienteApiStore.js';

import Login from '../views/Auth/Login.vue';
import Home from '../views/Home/Menu.vue';
import Recebimento from '../views/Recebimento/Menu.vue';
import Descarregar from '../views/Recebimento/Descarregar.vue';
import Conferir from '../views/Recebimento/Conferir.vue';
import Armazenar from '../views/Recebimento/Armazenar.vue';
import Estoque from '../views/Estoque/Menu.vue';
import Material from '../views/Estoque/ConsultarItem.vue';
import Locacao from '../views/Estoque/ConsultarLocacao.vue';
import Contar from '../views/Estoque/Contar.vue';
import Coletar from '../views/Estoque/Coletar.vue';
import Transferir from '../views/Estoque/Transferir.vue';
import Separacao from '../views/Separacao/Menu.vue';
import Expedicao from '../views/Expedicao/Menu.vue';
import Despachar from '../views/Expedicao/Despachar.vue';
import ConferirVolume from '../views/Expedicao/ConferirVolume.vue';
import ConferirSeparacao from '../views/Expedicao/ConferirSeparacao.vue';

export const stateSession = reactive({ errorMessage: '' });

const routes = [
  { path: '/login', name: 'Login', component: Login, meta: { requiresAuth: false, showNavbar: false, showBottomBar: false } },
  { path: '/', name: 'Home', component: Home, meta: { requiresAuth: true, showNavbar: true } },

  { path: '/recebimento', name: 'Recebimento', component: Recebimento, meta: { requiresAuth: true, showNavbar: true } },
  { path: '/descarga', name: 'Descarga', component: Descarregar, meta: { requiresAuth: true, showNavbar: true } },
  { path: '/conferencia', name: 'Conferencia', component: Conferir, meta: { requiresAuth: true, showNavbar: true } },
  { path: '/armazenagem', name: 'Armazenar', component: Armazenar, meta: { requiresAuth: true, showNavbar: true } },

  { path: '/estoque', name: 'Estoque', component: Estoque, meta: { requiresAuth: true, showNavbar: true } },
  { path: '/material', name: 'Material', component: Material, meta: { requiresAuth: true, showNavbar: true } },
  { path: '/locacao', name: 'Locacao', component: Locacao, meta: { requiresAuth: true, showNavbar: true } },
  { path: '/contar', name: 'Contar', component: Contar, meta: { requiresAuth: true, showNavbar: true } },
  { path: '/coletar', name: 'Coleta', component: Coletar, meta: { requiresAuth: true, showNavbar: true } },
  { path: '/transferir', name: 'Transferir', component: Transferir, meta: { requiresAuth: true, showNavbar: true } },

  { path: '/separacao', name: 'Separacao', component: Separacao, meta: { requiresAuth: true, showNavbar: true } },

  { path: '/expedicao', name: 'Expedicao', component: Expedicao, meta: { requiresAuth: true, showNavbar: true } },
  { path: '/despachar', name: 'Despachar', component: Despachar, meta: { requiresAuth: true, showNavbar: true } },
  { path: '/expedicao/conferir-volume', name: 'ConferirVolume', component: ConferirVolume, meta: { requiresAuth: true, showNavbar: true } },
  { path: '/expedicao/conferir-separacao', name: 'ConferirSeparacao', component: ConferirSeparacao, meta: { requiresAuth: true, showNavbar: true } },
];

export const logout = () => {
  const clienteApiStore = useClienteApiStore();
  clienteApiStore.clearConfig();

  const token = sessionStorage.getItem('quasarJWT');
  if (token) {
    sessionStorage.removeItem('quasarJWT');
    stateSession.errorMessage = '';
  } else {
    stateSession.errorMessage = 'Sua sessao foi desconectada! Faca login novamente.';
  }
  router.push('/login');
};

const router = createRouter({
  history: createWebHistory(),
  routes,
});

router.beforeEach((to, from, next) => {
  const token = sessionStorage.getItem('quasarJWT');
  if (to.meta.requiresAuth) {
    if (!token) {
      if (to.name != 'Home') {
        stateSession.errorMessage = 'Sua sessao foi desconectada! Faca login novamente.';
      }
      next('/login');
    } else {
      stateSession.errorMessage = '';
      next();
    }
  } else {
    next();
  }
});

export default router;

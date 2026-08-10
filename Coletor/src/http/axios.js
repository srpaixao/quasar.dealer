import axios from 'axios';
import { logout } from '@/router';
import { useClienteApiStore } from '@/stores/clienteApiStore.js';

// import { useAuthStore } from '@/stores/authStore.js';
// const authStore = useAuthStore();

const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL, // Base URL da API
  headers: {
    'Content-Type': 'application/json'
  },
  //withCredentials: true // Importante para enviar cookies
});

// Intercepta requisições para adicionar o token JWT 
api.interceptors.request.use(config => {
  const appToken = sessionStorage.getItem('quasarJWT');

  let authHeader = appToken ? `Bearer ${appToken}` : null;

  try {
    const clienteApiStore = useClienteApiStore();
    const baseApi = clienteApiStore.getBaseApi;
    const apiToken = clienteApiStore.getApiToken;

    if (baseApi && apiToken) {
      const normalizedBase = baseApi.endsWith('/') ? baseApi.slice(0, -1) : baseApi;

      const requestUrl = (() => {
        const rawUrl = config.url || '';

        if (/^https?:\/\//i.test(rawUrl)) {
          return rawUrl;
        }

        if (config.baseURL) {
          try {
            return new URL(rawUrl, config.baseURL).toString();
          } catch {
            return null;
          }
        }

        return null;
      })();

      if (requestUrl) {
        const normalizedRequest = requestUrl.endsWith('/') ? requestUrl.slice(0, -1) : requestUrl;

        if (normalizedRequest.startsWith(normalizedBase)) {
          authHeader = `Bearer ${apiToken}`;
        }
      }
    }
  } catch (storeError) {
    console.warn('Não foi possível acessar a store clienteApi no interceptor.', storeError);
  }

  if (authHeader) {
    config.headers = config.headers || {};
    config.headers.Authorization = authHeader;
  }

  // const authStore = useAuthStore();
  // const token = authStore.getToken;
  // console.log('token =>', token);
  // if (token) {
  //   config.headers.Authorization = `Bearer ${token}`;
  // }

  return config;
}, error => {
  return Promise.reject(error);
});

// Interceptor de resposta para tratamento de erros
api.interceptors.response.use(
  response => response,
  error => {
    if (error.response && error.response.status === 404) {
      // Ignorar o erro 404 para tratamento no componente 
      return Promise.reject(error);
    }

    if (error.response) {
      switch (error.response.status) {
        case 400:
          console.error('Erro 400: Requisição inválida', error.response.data);
          break;
        case 401:
          console.error('Erro 401: Não autorizado', error.response.data);
          logout();
          break;
        case 500:
          console.error('Erro 500: Erro no servidor', error.response.data);
          break;
        default:
          console.error(`Erro ${error.response.status}: ${error.response.data}`);
      }
    } else if (error.request) {
      // A requisição foi feita mas nenhuma resposta foi recebida
      console.error('Erro: Nenhuma resposta recebida', error.request);
    } else {
      // Algo aconteceu ao configurar a requisição que acionou um erro
      console.error('Erro ao configurar a requisição', error.message);
    }
    return Promise.reject(error);
  }
);

export default api;

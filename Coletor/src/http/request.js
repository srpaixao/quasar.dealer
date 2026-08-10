import axios from 'axios';
import api from './axios';
import { useClienteApiStore } from '@/stores/clienteApiStore.js';

export default {

  // Auth
  login(usuario, senha, filialId) {
    return api.post('/auth/login', {
      usuario,
      senha,
      filialId
    });
  },
  getCookie() {
    return api.get('/auth/check-cookie');
  },
  removeCookie() {
    return api.post('/auth/remove-cookie');
  },

  // Empresas
  obterEmpresas() {
    return api.get('/empresas');
  },

  //Armazenagem
  validarMaterial(itemnr, filialId) {
    return api.get(`/armazenagem/validarmaterial/${itemnr}`, { params: { filialId } });
  },
  validarQuantidade(itemnr, filialId) {
    return api.get(`/armazenagem/validarquantidade/${itemnr}`, { params: { filialId } });
  },
  armazenarMaterial(itemnr, quantidade, usuario, filialId) {
    return api.post('/armazenagem/atualizarItemNotaFiscal', {
      itemnr,
      quantidade,
      usuario,
      filialId
    });
  },
  gravarHistorico(historico) {
    return api.post('/armazenagem/gravarHistorico', historico, {
      headers: {
        'Content-Type': 'application/json'
      }
    })
  },

  // Estoque
  consultarItem(itemnr, filialId) {
    return api.get(`/estoque/consultaritem/${itemnr}`, { params: { filialId } });
  },
  consultarLocacao(itemnr, filialId) {
    return api.get(`/estoque/consultarlocacao/${itemnr}`, { params: { filialId } });
  },
  gravarMovimentacao(movimentacao) {
    return api.post('/estoque/movimentacao', movimentacao, {
      headers: {
        'Content-Type': 'application/json'
      }
    });
  },
  finalizarMovimentacao(movimentacao) {
    const id = movimentacao?.id ?? movimentacao?.Id;
    if (!id) {
      throw new Error('Movimentacao sem identificador nao pode ser finalizada.');
    }

    return api.put(`/estoque/movimentacao/${id}`, movimentacao, {
      headers: {
        'Content-Type': 'application/json'
      }
    });
  },
  consultarMovimentacao(itemnr, filialId) {
    return api.get(`/estoque/consultarmovimentacao/${itemnr}`, { params: { filialId } });
  },
  gravarColeta(coleta) {
    console.log('gravarColeta', coleta)
    return api.post('/estoque/movimentacao', coleta, {
      headers: {
        'Content-Type': 'application/json'
      }
    })
  },
  enviarDMS(movimento) {
    //const clienteApiStore = useClienteApiStore();
    //const urlDMS = clienteApiStore.getBaseApi + '/registrar-movimento'; // fake DMS endpoint
    //const urlDMS = clienteApiStore.getBaseApi;

    const urlDMS = 'http://200.232.27.59:1025/coletor/coletor_prateleira_api.php'
    const token = 'e6c75bb6cb3c793b9033461df3a835ee'; // token para teste fornecido pela SERCON

    console.log('payload', movimento);
    console.log('api', urlDMS);
    console.log('token', token);
    return api.post(urlDMS, movimento, {
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${token}`
      }
    });
  },
  obterClienteApi() {
    return api.get('/config/cliente-api');
  },
  autenticarClienteApi(baseUrl) {
    if (!baseUrl) {
      return Promise.reject(new Error('Base API do cliente não configurada.'));
    }

    const sanitizedBaseUrl = baseUrl.endsWith('/') ? baseUrl.slice(0, -1) : baseUrl;
    return axios.post(`${sanitizedBaseUrl}/auth`, {
      usuario: 'myApiUser',
    }, {
      headers: {
        'Content-Type': 'application/json'
      }
    });
  },

  // Expedição

  obterTransportadorasExpedicao(filialId) {
    return api.get(`/expedicao/transportadoras`, { params: { filialId } });
  },

  getResumoExpedicao(transportadoraId, filialId) {
    console.log('trasnportadoraId', transportadoraId);
    return api.get(`/expedicao/volumes/resumo/${transportadoraId}`, { params: { filialId } });
  },

  obterPendentesExpedicao(transportadoraId, filialId) {
    return api.get(`/expedicao/volumes/pendentes/${transportadoraId}`, { params: { filialId } });
  },

  obterLidosExpedicao(transportadoraId, filialId) {
    return api.get(`/expedicao/volumes/lidos/${transportadoraId}`, { params: { filialId } });
  },

  obterDocumentoExpedicao(notaFiscal, transportadoraId) {
    return api.get(`/expedicao/doc`, { params: { numero: notaFiscal, transportadoraId } });
  },

  getHistoricoVolumes(notaFiscalNr) {
    return api.get('/expedicao/historico/volumes', { params: { notaFiscalNr } });
  },

  postHistoricoDespacho(payload) {
    return api.post('/expedicao/historico', payload, {
      headers: { 'Content-Type': 'application/json' }
    });
  },










  // Recebimento
  obterAreas(filialId) {
    return api.get(`/areas`, { params: { filialId } });
  },

  contarVolume(areaId, filialId) {
    const statusId = 0;
    return api.get(`/recebimento/volumeresumo/${statusId}/${areaId}`, { params: { filialId } });
  },
  processarVolume(volume, area, filialId, usuario) {
    return api.post('/recebimento/volumeupdate', {
      volume,
      area,
      filialId,
      usuario
    });
  },
};

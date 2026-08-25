import axios from 'axios';
import api from './axios';
import { useClienteApiStore } from '@/stores/clienteApiStore.js';

export default {

  // Auth
  login(usuario, senha) {
    return api.post('/auth/login', {
      usuario,
      senha
    });
  },
  getCookie() {
    return api.get('/auth/check-cookie');
  },
  removeCookie() {
    return api.post('/auth/remove-cookie');
  },

  //Armazenagem
  validarMaterial(itemnr) {
    return api.get(`/armazenagem/validarmaterial/${itemnr}`);
  },
  validarQuantidade(itemnr) {
    return api.get(`/armazenagem/validarquantidade/${itemnr}`);
  },
  armazenarMaterial(itemnr, quantidade, usuario) {
    return api.post('/armazenagem/atualizarItemNotaFiscal', {
      itemnr,
      quantidade,
      usuario
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
  consultarItem(itemnr) {
    return api.get(`/estoque/consultaritem/${itemnr}`);
  },
  consultarLocacao(itemnr) {
    return api.get(`/estoque/consultarlocacao/${itemnr}`);
  },
  validarLocacao(codigo) {
    return api.get(`/estoque/validarlocacao/${encodeURIComponent(codigo)}`);
  },
  consultarMovimentacoesLocacaoEspera(codigo) {
    return api.get(`/estoque/locacao-espera/${encodeURIComponent(codigo)}/movimentacoes`);
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
  consultarMovimentacao(itemnr) {
    return api.get(`/estoque/consultarmovimentacao/${itemnr}`);
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

  // Separação
  obterZonasSeparacao() {
    return api.get('/separacao/zonas');
  },
  assumirTarefaSeparacao(zonaId) {
    return api.post('/separacao/assumir-tarefa', { zonaId });
  },
  obterLinhaAtualSeparacao(tarefaNr) {
    return api.get(`/separacao/tarefas/${encodeURIComponent(tarefaNr)}/linha-atual`);
  },
  liberarTarefaSeparacao(tarefaNr) {
    return api.post(`/separacao/tarefas/${encodeURIComponent(tarefaNr)}/abandonar`, {}, {
      headers: {
        'Content-Type': 'application/json'
      }
    });
  },
  confirmarLinhaSeparacao(tarefaNr, payload) {
    return api.post(`/separacao/tarefas/${encodeURIComponent(tarefaNr)}/confirmar-linha`, payload, {
      headers: {
        'Content-Type': 'application/json'
      }
    });
  },
  passbyLinhaSeparacao(tarefaNr) {
    return api.post(`/separacao/tarefas/${encodeURIComponent(tarefaNr)}/passby-linha`, {}, {
      headers: {
        'Content-Type': 'application/json'
      }
    });
  },
  obterStatusTarefaSeparacao(tarefaNr) {
    return api.get(`/separacao/tarefas/${encodeURIComponent(tarefaNr)}/status`);
  },

  // Expedição

  obterTransportadorasExpedicao() {
    return api.get(`/expedicao/transportadoras`);
  },

  getResumoExpedicao(transportadoraId) {
    console.log('trasnportadoraId', transportadoraId);
    return api.get(`/expedicao/volumes/resumo/${transportadoraId}`);
  },

  obterPendentesExpedicao(transportadoraId) {
    return api.get(`/expedicao/volumes/pendentes/${transportadoraId}`);
  },

  obterLidosExpedicao(transportadoraId) {
    return api.get(`/expedicao/volumes/lidos/${transportadoraId}`);
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
  obterRomaneiosConferenciaSeparacao() {
    return api.get('/expedicao/conferencia-separacao/romaneios');
  },
  iniciarConferenciaSeparacao(romaneioId) {
    return api.post('/expedicao/conferencia-separacao/iniciar', { romaneioId }, {
      headers: { 'Content-Type': 'application/json' }
    });
  },
  obterItensConferenciaSeparacao(romaneioId) {
    return api.get(`/expedicao/conferencia-separacao/romaneios/${romaneioId}/itens`);
  },
  confirmarItemConferenciaSeparacao(romaneioId, payload) {
    return api.post(`/expedicao/conferencia-separacao/romaneios/${romaneioId}/confirmar`, payload, {
      headers: { 'Content-Type': 'application/json' }
    });
  },
  liberarConferenciaSeparacao(romaneioId) {
    return api.post(`/expedicao/conferencia-separacao/romaneios/${romaneioId}/abandonar`, {}, {
      headers: { 'Content-Type': 'application/json' }
    });
  },










  // Recebimento
  obterAreas() {
    return api.get(`/areas`);
  },

  contarVolume(areaId) {
    const statusId = 0;
    return api.get(`/recebimento/volumeresumo/${statusId}/${areaId}`);
  },
  obterVolumesPendentesRecebimento(areaId) {
    const statusPendenteId = 1;
    return api.get(`/recebimento/volumeresumo/${statusPendenteId}/${areaId}`);
  },
  processarVolume(volume, area) {
    return api.post('/recebimento/volumeupdate', {
      volume,
      area
    });
  },
  obterConferenciaVolume(volume) {
    return api.get(`/recebimento/conferencia-volume/${encodeURIComponent(volume)}`);
  },
  confirmarConferenciaItem(volume, itemId, payload) {
    return api.post(
      `/recebimento/conferencia-volume/${encodeURIComponent(volume)}/itens/${itemId}/confirmar`,
      payload,
      { headers: { 'Content-Type': 'application/json' } }
    );
  },
};

import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const scriptDirectory = path.dirname(fileURLToPath(import.meta.url));
const docsRoot = path.resolve(scriptDirectory, '..', '..', '..', 'docs');
const documents = [
  'README.md',
  'MANUAL_UTILIZACAO.md',
  'MANUAL_TELAS_INDEX.md',
  'MANUAL_TELAS_GERAL.md',
  'MANUAL_TELAS_RECEBIMENTO.md',
  'MANUAL_TELAS_ESTOQUE.md',
  'MANUAL_TELAS_SEPARACAO.md',
  'MANUAL_TELAS_EXPEDICAO.md',
  'TEMPLATE_MANUAL_TELA.md'
];

const corrections = {
  acao: 'ação',
  acoes: 'ações',
  aplicacao: 'aplicação',
  aplicacoes: 'aplicações',
  aplicavel: 'aplicável',
  administracao: 'administração',
  apos: 'após',
  area: 'área',
  areas: 'áreas',
  ate: 'até',
  atualizacao: 'atualização',
  atualizacoes: 'atualizações',
  automatico: 'automático',
  automatica: 'automática',
  automaticos: 'automáticos',
  automaticas: 'automáticas',
  basico: 'básico',
  basica: 'básica',
  basicos: 'básicos',
  basicas: 'básicas',
  botao: 'botão',
  botoes: 'botões',
  catalogo: 'catálogo',
  codigo: 'código',
  codigos: 'códigos',
  configuracao: 'configuração',
  configuracoes: 'configurações',
  conferencia: 'conferência',
  conferencias: 'conferências',
  conclusao: 'conclusão',
  concluida: 'concluída',
  confirmacao: 'confirmação',
  concorrencia: 'concorrência',
  conteudo: 'conteúdo',
  conteudos: 'conteúdos',
  convem: 'convém',
  critico: 'crítico',
  critica: 'crítica',
  celula: 'célula',
  devolucao: 'devolução',
  devolucoes: 'devoluções',
  descricao: 'descrição',
  descricoes: 'descrições',
  dialogo: 'diálogo',
  diferenca: 'diferença',
  diferencas: 'diferenças',
  digitacao: 'digitação',
  documentacao: 'documentação',
  disponivel: 'disponível',
  disponiveis: 'disponíveis',
  divergencia: 'divergência',
  divergencias: 'divergências',
  duvida: 'dúvida',
  emissao: 'emissão',
  endereco: 'endereço',
  enderecos: 'endereços',
  evidencia: 'evidência',
  evidencias: 'evidências',
  excecao: 'exceção',
  exclusao: 'exclusão',
  execucao: 'execução',
  especifico: 'específico',
  especifica: 'específica',
  estao: 'estão',
  evolucao: 'evolução',
  expedicao: 'expedição',
  facil: 'fácil',
  finalizacao: 'finalização',
  formatacao: 'formatação',
  formulario: 'formulário',
  formularios: 'formulários',
  fisica: 'física',
  fisico: 'físico',
  funcao: 'função',
  funcoes: 'funções',
  geracao: 'geração',
  generica: 'genérica',
  gravacao: 'gravação',
  ha: 'há',
  historico: 'histórico',
  historicos: 'históricos',
  homonimo: 'homônimo',
  homonimos: 'homônimos',
  identificacao: 'identificação',
  importacao: 'importação',
  importacoes: 'importações',
  impressao: 'impressão',
  impressoes: 'impressões',
  informacao: 'informação',
  informacoes: 'informações',
  integracao: 'integração',
  integracoes: 'integrações',
  ja: 'já',
  lancamento: 'lançamento',
  lancamentos: 'lançamentos',
  locacao: 'locação',
  locacoes: 'locações',
  logica: 'lógica',
  logistico: 'logístico',
  logistica: 'logística',
  mantem: 'mantém',
  maximo: 'máximo',
  maxima: 'máxima',
  metodo: 'método',
  metodos: 'métodos',
  minimo: 'mínimo',
  minima: 'mínima',
  modulo: 'módulo',
  modulos: 'módulos',
  movimentacao: 'movimentação',
  movimentacoes: 'movimentações',
  movel: 'móvel',
  moveis: 'móveis',
  nao: 'não',
  navegacao: 'navegação',
  necessario: 'necessário',
  necessaria: 'necessária',
  necessarios: 'necessários',
  necessarias: 'necessárias',
  numero: 'número',
  numeros: 'números',
  obrigatorio: 'obrigatório',
  obrigatoria: 'obrigatória',
  obrigatorios: 'obrigatórios',
  obrigatorias: 'obrigatórias',
  observacao: 'observação',
  observacoes: 'observações',
  operacao: 'operação',
  operacoes: 'operações',
  ordenacao: 'ordenação',
  orientacao: 'orientação',
  orientacoes: 'orientações',
  pagina: 'página',
  paginas: 'páginas',
  paginacao: 'paginação',
  parametro: 'parâmetro',
  parametros: 'parâmetros',
  parametrizacao: 'parametrização',
  padrao: 'padrão',
  padroes: 'padrões',
  periodo: 'período',
  pendencia: 'pendência',
  pendencias: 'pendências',
  periodica: 'periódica',
  politica: 'política',
  posicao: 'posição',
  possivel: 'possível',
  possiveis: 'possíveis',
  pratica: 'prática',
  praticas: 'práticas',
  pre: 'pré',
  proximo: 'próximo',
  proxima: 'próxima',
  proximos: 'próximos',
  publicacao: 'publicação',
  publico: 'público',
  publica: 'pública',
  rapido: 'rápido',
  rapida: 'rápida',
  recomendacao: 'recomendação',
  recomendacoes: 'recomendações',
  referencia: 'referência',
  referencias: 'referências',
  responsavel: 'responsável',
  responsaveis: 'responsáveis',
  separacao: 'separação',
  selecao: 'seleção',
  sequencia: 'sequência',
  serao: 'serão',
  sao: 'são',
  situacao: 'situação',
  situacoes: 'situações',
  tambem: 'também',
  tecnico: 'técnico',
  tecnica: 'técnica',
  tecnicos: 'técnicos',
  tecnicas: 'técnicas',
  titulo: 'título',
  titulos: 'títulos',
  transferencia: 'transferência',
  transferencias: 'transferências',
  transito: 'trânsito',
  ultimo: 'último',
  ultima: 'última',
  ultimos: 'últimos',
  ultimas: 'últimas',
  utilizacao: 'utilização',
  unico: 'único',
  unica: 'única',
  util: 'útil',
  usuario: 'usuário',
  usuarios: 'usuários',
  valido: 'válido',
  valida: 'válida',
  validacao: 'validação',
  validacoes: 'validações',
  veiculo: 'veículo',
  veiculos: 'veículos',
  versao: 'versão',
  visivel: 'visível',
  visiveis: 'visíveis'
};

function preserveCase(original, corrected) {
  if (original === original.toUpperCase()) {
    return corrected.toUpperCase();
  }

  if (original[0] === original[0].toUpperCase()) {
    return corrected[0].toUpperCase() + corrected.slice(1);
  }

  return corrected;
}

function correctSegment(segment) {
  return segment.replace(/\p{L}+/gu, word => {
    const corrected = corrections[word.toLowerCase()];
    return corrected ? preserveCase(word, corrected) : word;
  });
}

function correctLine(line) {
  const protectedValues = [];
  const protectedLine = line.replace(/`[^`]*`|\]\([^)]+\)/g, value => {
    const token = `@@MANUALPROTECTED${protectedValues.length}@@`;
    protectedValues.push(value);
    return token;
  });

  let corrected = correctSegment(protectedLine);
  corrected = corrected.replace(/@@MANUALPROTECTED(\d+)@@/g, (_, index) => protectedValues[Number(index)]);
  return corrected;
}

for (const document of documents) {
  const filePath = path.join(docsRoot, document);
  const original = fs.readFileSync(filePath, 'utf8');
  const corrected = original
    .split(/\r?\n/)
    .map(correctLine)
    .join('\n');

  if (corrected !== original) {
    fs.writeFileSync(filePath, corrected, 'utf8');
    console.log(`Acentuação revisada: ${document}`);
  }
}

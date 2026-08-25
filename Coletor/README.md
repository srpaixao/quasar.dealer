# Coletor Quasar

Versão: `1.0.7`

Documentação revisada em `25/08/2026`.

## Objetivo

Aplicacao frontend do coletor para operacao mobile do WMS Quasar.

O foco do projeto e oferecer fluxos curtos, navegacao touch e consumo da API operacional.

## Stack

- Vue 3
- Vite
- Vuetify 3
- Pinia
- Axios

## Modulos Atuais no Coletor

### Home

- menu principal

### Recebimento

- descarregar
- conferir
- armazenar

Na conferencia de recebimento, cada item registra quantidade conferida, responsavel e data/hora. A armazenagem registra quantidade armazenada, estoquista e data/hora sem sobrescrever a quantidade faturada.

No descarregamento:

- o card `Pendentes` abre uma lista atualizada contendo somente os números dos volumes pendentes
- o teclado virtual do campo `Volume NR` fica desativado por padrão
- o ícone de teclado habilita a entrada manual
- o leitor deve enviar `Enter` ou `Tab` ao final do barcode

### Estoque

- consultar locacao
- consultar item
- contar
- coletar
- transferir

Os fluxos `Coletar` e `Transferir` são orientados pela Locação de Espera. A coleta registra movimentações para a espera e a transferência lista as pendências da localização antes da confirmação do destino final.

### Separacao

- menu de separacao

### Expedicao

- despachar
- conferir volume
- conferir separacao

## Rotas da Aplicacao

Definidas em `src/router/index.js`.

Rotas principais:

- `/login`
- `/`
- `/recebimento`
- `/descarga`
- `/conferencia`
- `/armazenagem`
- `/estoque`
- `/material`
- `/locacao`
- `/contar`
- `/coletar`
- `/transferir`
- `/separacao`
- `/expedicao`
- `/despachar`
- `/expedicao/conferir-volume`
- `/expedicao/conferir-separacao`

## Integracao com API

### Base URL

Em desenvolvimento, a URL da API e lida de:

- `.env`
- `.env.development`
- `.env.qa`
- `.env.production`

Exemplo atual em desenvolvimento:

```env
VITE_API_BASE_URL="http://localhost:5049/"
```

## Scripts

| Comando | Descricao |
|---|---|
| `npm run dev` | sobe o ambiente local com Vite |
| `npm run build` | gera build padrao |
| `npm run build:qa` | gera build no modo `qa` |
| `npm run preview` | publica a build localmente para validacao |

## Execucao

```powershell
cd coletor
npm install
npm run dev
```

## Build

```powershell
npm run build
```

## Fluxo de Conferir Separacao em Expedicao

Tela:

- `Expedicao > Conferir Separacao`

Comportamento atual:

- ao selecionar o romaneio, a conferencia inicia automaticamente
- nao ha botao de iniciar conferencia
- o romaneio fica bloqueado para o usuario que assumiu
- quantidade zero exige confirmacao
- quantidade zero envia o item para `Em Busca`
- opcao de voltar ou sair libera o romaneio quando a conferencia ainda esta em andamento

## Estrutura Relevante

```text
coletor/
|-- src/
|   |-- http/
|   |-- router/
|   |-- stores/
|   `-- views/
|-- public/
|-- scripts/
|-- package.json
`-- vite.config.js
```

## Observacoes

- este projeto depende funcionalmente da API
- o backend legado principal continua no diretorio `web/`
- mudancas de regra operacional devem ser atualizadas em conjunto no coletor, API e manual de utilizacao
- o coletor depende das rotas de conferencia quantitativa descritas em `../api/README.md`
- regras consolidadas em `../web/docs/ATUALIZACOES_20260720.md`
- atualização funcional mais recente em `../web/docs/ATUALIZACOES_20260825.md`

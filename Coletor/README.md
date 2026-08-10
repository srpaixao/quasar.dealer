# Quasar Coletor

Frontend do projeto **Quasar Dealer Nova Chevrolet**, desenvolvido em **Vue 3** com **Vite**, **Vuetify** e **Pinia**. A aplicação é voltada para uso operacional em coletores ou navegadores mobile/desktop, consumindo a API do projeto `api` para autenticação e fluxos de recebimento, estoque e expedição.

Este README foi escrito para facilitar o onboarding de quem precisa manter ou evoluir o frontend.

## Visão geral do produto

O sistema funciona como uma interface operacional para atividades de logística. O usuário faz login, seleciona a filial e navega por módulos que representam processos do chão de operação.

Fluxos presentes no código:

1. autenticação do operador;
2. navegação por módulos operacionais;
3. recebimento com descarga/conferência de volumes;
4. armazenagem de itens;
5. consultas e movimentações de estoque;
6. conferência de volumes na expedição.

Há também partes ainda incompletas, desativadas ou em transição, como:

- inventário;
- separação;
- despacho direto na expedição;
- integração dinâmica com API externa do cliente/DMS em alguns trechos.

## Stack técnica

- `Vue 3`
- `Vite 5`
- `Vuetify 3`
- `Vue Router 4`
- `Pinia`
- `pinia-plugin-persistedstate`
- `Axios`
- `Material Design Icons`

Dependências declaradas em [`package.json`](./package.json):

- `vue`
- `vue-router`
- `vuetify`
- `pinia`
- `pinia-plugin-persistedstate`
- `axios`
- `@mdi/font`

## Estrutura do projeto

```text
coletor/
├── public/
├── scripts/
├── src/
│   ├── assets/
│   ├── components/
│   ├── http/
│   ├── router/
│   ├── stores/
│   ├── utils/
│   └── views/
├── .env*
├── index.html
├── package.json
└── vite.config.js
```

### Pastas principais

- [`src/main.js`](./src/main.js): bootstrap da aplicação, registro de router, Pinia e Vuetify.
- [`src/App.vue`](./src/App.vue): shell principal com `v-app`, navbar superior e `router-view`.
- [`src/router/index.js`](./src/router/index.js): definição das rotas, guarda de autenticação e logout.
- [`src/http/axios.js`](./src/http/axios.js): instância Axios com interceptors.
- [`src/http/request.js`](./src/http/request.js): camada de acesso HTTP usada pelas telas.
- [`src/stores/authStore.js`](./src/stores/authStore.js): store do usuário autenticado.
- [`src/stores/clienteApiStore.js`](./src/stores/clienteApiStore.js): store de configuração e token de API externa.
- [`src/views`](./src/views): telas organizadas por domínio operacional.
- [`scripts/run-vite-build.js`](./scripts/run-vite-build.js): wrapper para build com suporte a `--mode`.

## Como a aplicação sobe

O ponto de entrada é [`src/main.js`](./src/main.js):

1. cria a app Vue;
2. registra `Pinia`;
3. ativa persistência com `pinia-plugin-persistedstate`;
4. registra `Vue Router`;
5. registra `Vuetify`;
6. monta a aplicação em `#app`.

O layout base fica em [`src/App.vue`](./src/App.vue):

- mostra a [`Navbar.vue`](./src/components/Navbar.vue) quando a rota ativa pede `meta.showNavbar`;
- renderiza a view corrente dentro de `v-main`;
- não há layout complexo nem componente global de bottom bar compartilhado.

## Configuração por ambiente

Arquivos existentes:

- [`.env`](./.env)
- [`.env.development`](./.env.development)
- [`.env.qa`](./.env.qa)
- [`.env.production`](./.env.production)

A variável usada explicitamente no código é:

- `VITE_API_BASE_URL`: URL base da API backend.

Valores encontrados hoje:

- `Development`: `http://localhost:5049/`
- `QA`: `http://srpaixao-001-site6.jtempurl.com/`
- `Production`: `http://nova.api.quasardealer.com.br`

### Observações importantes

- a aplicação faz `console.log` do modo e da `VITE_API_BASE_URL` em `main.js`;
- o `build:qa` usa `npm run build --mode qa`;
- não há tipagem nem validação centralizada das variáveis de ambiente;
- a URL da API é crítica, porque praticamente toda navegação autenticada depende dela.

## Execução local

### Pré-requisitos

- `Node.js`
- `npm`
- API backend disponível e acessível pela `VITE_API_BASE_URL`

### Comandos principais

```bash
npm install
npm run dev
```

Build local:

```bash
npm run build
```

Build para QA:

```bash
npm run build:qa
```

Preview do bundle:

```bash
npm run preview
```

### Observação sobre scripts

O projeto atualmente possui apenas estes scripts:

- `dev`
- `build`
- `build:qa`
- `preview`

Não há scripts de:

- `test`
- `lint`
- `type-check`

## Build e empacotamento

O build usa [`vite.config.js`](./vite.config.js) com:

- alias `@` apontando para `src`;
- separação manual de chunks para reduzir o bundle principal:
  - `vendor_vuetify`
  - `vendor_vue`
  - `vendor`

O script [`scripts/run-vite-build.js`](./scripts/run-vite-build.js) existe para tratar modos de build com flexibilidade, inclusive quando o `npm` repassa o modo de forma diferente.

## Navegação e rotas

As rotas estão em [`src/router/index.js`](./src/router/index.js). O projeto usa `createWebHistory()` e uma guarda global simples baseada em token no `sessionStorage`.

### Regras atuais de autenticação

- o token principal da sessão é salvo em `sessionStorage` com a chave `quasarJWT`;
- rotas com `meta.requiresAuth: true` exigem esse token;
- se o token não existir, o usuário é redirecionado para `/login`;
- o logout remove `quasarJWT`, limpa a store `clienteApiStore` e volta para `/login`.

### Rotas mapeadas

| Rota | Tela | Status |
| --- | --- | --- |
| `/login` | login | implementada |
| `/` | menu inicial | implementada |
| `/recebimento` | menu de recebimento | implementada |
| `/descarga` | descarga/conferência de volumes | implementada |
| `/conferencia` | conferência de recebimento | rota existe, tela pouco clara/incompleta |
| `/armazenagem` | armazenagem de materiais | implementada |
| `/estoque` | menu de estoque | implementada |
| `/material` | consulta de item | implementada |
| `/locacao` | consulta de locação | implementada |
| `/contar` | contagem | rota existe, mas menu marca como inativa |
| `/coletar` | coleta de item | implementada |
| `/transferir` | transferência com integração externa | implementada |
| `/separacao` | menu de separação | existe, mas sem fluxo aprofundado |
| `/expedicao` | menu de expedição | implementada |
| `/despachar` | despacho | tela existe, mas menu atual marca como inativo |
| `/expedicao/conferir-volume` | conferência de volumes da expedição | implementada |

## Fluxos principais

### Login

Tela em [`src/views/Auth/Login.vue`](./src/views/Auth/Login.vue).

Comportamento atual:

- carrega filiais via `GET /empresas`;
- autentica via `POST /auth/login`;
- grava dados básicos do usuário na `authStore`;
- grava o JWT em `sessionStorage`;
- redireciona para a home.

Dados mantidos após login:

- `account`
- `fullName`
- `email`
- `filialId`

### Recebimento

### Menu

Arquivo: [`src/views/Recebimento/Menu.vue`](./src/views/Recebimento/Menu.vue)

Entradas expostas no menu:

- `Descarregar`
- `Armazenar`

### Descarregar

Arquivo: [`src/views/Recebimento/Descarregar.vue`](./src/views/Recebimento/Descarregar.vue)

Fluxo implementado:

- carrega áreas via `GET /areas`;
- filtra áreas com `Tipo = R`;
- ao selecionar uma área, consulta resumo de volumes via `GET /recebimento/volumeresumo/{statusId}/{areaId}`;
- ao ler um volume, envia para `POST /recebimento/volumeupdate`;
- exibe contadores de pendentes, confirmados, total e incorretos.

### Armazenar

Arquivo: [`src/views/Recebimento/Armazenar.vue`](./src/views/Recebimento/Armazenar.vue)

Fluxo implementado:

- valida material via `GET /armazenagem/validarmaterial/{codigo}`;
- valida locação confirmada pelo operador;
- valida quantidade disponível via `GET /armazenagem/validarquantidade/{codigo}`;
- atualiza armazenagem via `POST /armazenagem/atualizarItemNotaFiscal`;
- grava histórico via `POST /armazenagem/gravarHistorico`.

O componente também registra ocorrência de erro em histórico quando:

- a locação informada está incorreta;
- a quantidade informada excede a permitida.

### Estoque

### Menu

Arquivo: [`src/views/Estoque/Menu.vue`](./src/views/Estoque/Menu.vue)

Entradas do menu:

- `Consultar Locação`
- `Consultar Item`
- `Coletar`
- `Transferir`
- `Contagem` como botão visualmente inativo

### Consultar Item

Arquivo: [`src/views/Estoque/ConsultarItem.vue`](./src/views/Estoque/ConsultarItem.vue)

Usa:

- `GET /estoque/consultaritem/{itemnr}`

### Consultar Locação

Arquivo: [`src/views/Estoque/ConsultarLocacao.vue`](./src/views/Estoque/ConsultarLocacao.vue)

Usa:

- `GET /estoque/consultarlocacao/{codigo}`

### Coletar

Arquivo: [`src/views/Estoque/Coletar.vue`](./src/views/Estoque/Coletar.vue)

Fluxo implementado:

- consulta item e saldo atual;
- monta payload de movimentação de coleta;
- grava movimentação via `POST /estoque/movimentacao`.

### Transferir

Arquivo: [`src/views/Estoque/Transferir.vue`](./src/views/Estoque/Transferir.vue)

Fluxo implementado:

- consulta movimentação pendente via `GET /estoque/consultarmovimentacao/{itemnr}`;
- valida a locação destino lida pelo operador;
- envia evento para integração externa/DMS;
- finaliza a movimentação no backend via `PUT /estoque/movimentacao/{id}`.

Esse fluxo é o mais sensível do frontend hoje, porque mistura:

- backend Quasar;
- configuração externa via `clienteApiStore`;
- integração DMS;
- payloads parcialmente hardcoded.

### Expedição

### Menu

Arquivo: [`src/views/Expedicao/Menu.vue`](./src/views/Expedicao/Menu.vue)

Entradas do menu:

- `Conferir Volumes`
- `Despachar` como botão visualmente inativo

### Conferir Volumes

Arquivo: [`src/views/Expedicao/ConferirVolume.vue`](./src/views/Expedicao/ConferirVolume.vue)

Fluxo implementado:

- carrega transportadoras habilitadas;
- consulta resumo de volumes por transportadora;
- abre listagens de pendentes e lidos;
- consulta documento por nota fiscal;
- consulta histórico de volumes já conferidos;
- grava histórico de despacho de cada volume.

Endpoints usados:

- `GET /expedicao/transportadoras`
- `GET /expedicao/volumes/resumo/{transportadoraId}`
- `GET /expedicao/volumes/pendentes/{transportadoraId}`
- `GET /expedicao/volumes/lidos/{transportadoraId}`
- `GET /expedicao/doc`
- `GET /expedicao/historico/volumes`
- `POST /expedicao/historico`

## Stores e persistência

### `authStore`

Arquivo: [`src/stores/authStore.js`](./src/stores/authStore.js)

Responsável por manter:

- conta do usuário;
- nome;
- e-mail;
- filial.

Usa persistência com `pinia-plugin-persistedstate`.

### `clienteApiStore`

Arquivo: [`src/stores/clienteApiStore.js`](./src/stores/clienteApiStore.js)

Responsável por manter:

- `baseApi`
- `userApi`
- `apiToken`

Essa store serve de base para integrações externas, especialmente no interceptor Axios e no fluxo de transferência.

## Camada HTTP

### Axios base

Arquivo: [`src/http/axios.js`](./src/http/axios.js)

Comportamento atual:

- usa `VITE_API_BASE_URL` como `baseURL`;
- envia `Content-Type: application/json`;
- adiciona `Authorization: Bearer <quasarJWT>` por padrão;
- se a URL da requisição pertencer à `baseApi` externa, tenta usar o `apiToken` da `clienteApiStore` no lugar do JWT principal;
- em `401`, executa logout;
- em `404`, apenas propaga para tratamento na view.

### Catálogo de chamadas

Arquivo: [`src/http/request.js`](./src/http/request.js)

Esse arquivo centraliza quase todo o consumo da API:

- auth
- empresas
- armazenagem
- estoque
- expedição
- recebimento
- configuração de API externa

Apesar disso, ele ainda contém detalhes que merecem revisão:

- URL externa hardcoded para DMS;
- token externo hardcoded para teste;
- métodos comentados e legado de tentativa com cookies;
- inconsistência em nomes de payload entre frontend e backend.

## Layout e UX

O projeto tem uma abordagem simples e pragmática:

- menu principal por módulos;
- telas com poucos campos e foco em leitura por scanner;
- uso intenso de `focus()` após ações;
- navegação inferior local em várias telas;
- feedback por dialogs modais e mensagens curtas.

Isso é coerente com uso operacional, mas a UI ainda está bastante acoplada à lógica da tela.

## Integração com o backend `api`

Este frontend depende diretamente da API .NET do projeto vizinho.

Mapeamentos principais já observados:

- login `POST /auth/login`
- empresas `GET /empresas`
- áreas `GET /areas`
- recebimento `GET/POST /recebimento/...`
- armazenagem `GET/POST /armazenagem/...`
- estoque `GET/POST/PUT /estoque/...`
- expedição `GET/POST /expedicao/...`
- config externa `GET /config/cliente-api`

Na prática, o frontend assume:

- backend disponível;
- contrato de resposta relativamente estável;
- autenticação JWT funcionando;
- `filialId` presente após login.

## Estado atual de implementação

O projeto está funcional para parte relevante do fluxo operacional, mas ainda não está totalmente consolidado.

Pontos observados:

- há telas claramente operacionais e usadas;
- há telas/rotas presentes, mas parcialmente prontas ou não expostas no menu final;
- parte do código contém comentários extensos e trechos desativados;
- há dependência direta de estruturas da API sem camada de normalização robusta.

## Riscos e pontos de atenção

- **sem testes automatizados**: qualquer mudança exige validação manual forte.
- **sem lint/type-check**: erros simples podem passar com facilidade.
- **integração externa hardcoded**: `request.js` e `Transferir.vue` possuem URL/token/payload de DMS embutidos.
- **uso misto de persistência**: JWT em `sessionStorage`, stores em persistência Pinia.
- **contratos pouco tipados**: respostas da API são consumidas direto nas views.
- **lógica concentrada em componentes**: regras de fluxo, validação e integração ficam espalhadas nas telas.
- **botões/rotas inconsistentes**: algumas telas existem, mas aparecem como inativas no menu.
- **logs no console**: há `console.log` e `console.warn` em fluxo normal da aplicação.

## Sugestão de leitura para onboarding

Se você precisa entender rápido o frontend, leia nesta ordem:

1. [`package.json`](./package.json)
2. [`src/main.js`](./src/main.js)
3. [`src/router/index.js`](./src/router/index.js)
4. [`src/http/axios.js`](./src/http/axios.js)
5. [`src/http/request.js`](./src/http/request.js)
6. [`src/stores/authStore.js`](./src/stores/authStore.js)
7. [`src/stores/clienteApiStore.js`](./src/stores/clienteApiStore.js)
8. telas em [`src/views`](./src/views)

Essa sequência já dá uma visão boa de bootstrap, autenticação, contratos com API e fluxos operacionais.

## Próximos melhoramentos recomendados

- centralizar contratos HTTP por domínio;
- remover dados hardcoded de integração externa;
- padronizar tratamento de erro e mensagens;
- separar lógica operacional das views em composables ou services;
- revisar rotas/telas realmente ativas;
- adicionar lint e testes mínimos de smoke/integração;
- documentar o fluxo real de DMS e a origem dos tokens.

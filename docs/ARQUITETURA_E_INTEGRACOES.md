# Arquitetura e Integracoes

## Visao Geral

O ambiente Automec do WMS Quasar opera com arquitetura hibrida:

- aplicacao web MVC legada e principal
- API moderna para integracoes e operacao mobile
- coletor frontend consumindo a API
- banco SQL Server compartilhado

## Componentes

### 1. Web MVC

Responsabilidades principais:

- telas administrativas e operacionais de backoffice
- menus
- relatorios
- cadastros
- regras legadas ainda mantidas no proprio MVC

Tecnologias:

- ASP.NET MVC 5
- .NET Framework 4.8
- Entity Framework 6
- SQL Server

### 2. API

Responsabilidades principais:

- autenticacao do coletor
- operacoes de recebimento, estoque, separacao e expedicao
- regras transacionais mais novas
- pontos de integracao HTTP

Tecnologias:

- ASP.NET Core `net9.0`
- Entity Framework Core
- JWT

### 3. Coletor

Responsabilidades principais:

- experiencia operacional simples
- navegação touch
- consumo da API

Tecnologias:

- Vue 3
- Vite
- Vuetify
- Pinia
- Axios

## Banco de Dados

Base principal observada em desenvolvimento:

- `Quasar_Automec`

Observacoes:

- Web MVC usa EDMX/EF6
- API usa EF Core
- qualquer alteracao de schema deve considerar os dois lados

## Integracoes Internas

### Web -> Banco

- acesso direto via EF6 e consultas SQL

### API -> Banco

- acesso via EF Core
- alguns fluxos usam SQL e transacoes para controle de concorrencia

### Coletor -> API

- autenticacao por JWT
- URL base configurada por variavel `VITE_API_BASE_URL`

### Descarga de Recebimento

1. O coletor consulta `/recebimento/volumeresumo/0/{areaId}` para montar os indicadores.
2. Ao abrir `Pendentes`, consulta `/recebimento/volumeresumo/1/{areaId}`.
3. A API agrupa os registros por `VolumeNr` e aplica filial, área e prioridade de status.
4. O coletor exibe somente `VolumeNr`, sem duplicar a regra de status no frontend.
5. A leitura envia o volume para `/recebimento/volumeupdate` e atualiza os indicadores.

O uso de `inputmode="none"` evita a abertura automática do teclado virtual em coletores compatíveis sem bloquear a entrada do leitor configurado como teclado físico.

### Estoque por Locação de Espera

1. A coleta valida a Locação de Espera e consulta item, saldo e origem.
2. A API cria ou atualiza a movimentação intermediária.
3. A transferência consulta todas as movimentações pendentes da Locação de Espera.
4. O operador confirma o destino físico e a API finaliza a movimentação.

### Anomalias GM

O módulo `AnomaliaApp` opera no Web MVC e utiliza SQL transacional para proteger o saldo reclamável.

1. A consulta parte do número do item e retorna as ocorrências de NF/volume da filial.
2. O prazo é calculado com `DataEmissao` e `PrazoDias` do tipo de anomalia.
3. No cadastro, o saldo é novamente validado dentro da transação para impedir consumo concorrente acima do faturado.
4. Cada item mantém seu próprio tipo A, B, C ou G, mesmo dentro do mesmo processo.
5. O serviço de formulário preenche modelos `.xls` oficiais armazenados em `App_Data/Templates`.
6. Preço unitário e imposto vêm das posições DNI importadas pelo fluxo `Trânsito GM`.

Principais tabelas:

- `AnomaliaGmProcesso`;
- `AnomaliaGmItem`;
- `AnomaliaGmTipo`;
- `AnomaliaGmStatus`;
- `AnomaliaGmHistorico`;
- `AnomaliaGmArquivo` e `AnomaliaGmArquivoItem`.

Os registros são isolados por `FilialId`. O reenvio referencia a reclamação original e não consome saldo novamente.

## Fluxos de Integracao Relevantes

### Conferencia de Romaneios de Expedicao

Fluxo atual:

1. coletor ou web seleciona romaneio
2. canal executa assumir/iniciar conferencia
3. sistema grava bloqueio por usuario
4. itens sao consultados com base no usuario logado
5. confirmacoes atualizam status, quantidade e data de conferencia
6. ao final, romaneio e concluido ou liberado

Pontos tecnicos importantes:

- controle de concorrencia por `ConferenteId`
- transacao ao assumir e ao soltar
- uso da data/hora do servidor
- protecao contra sobrescrita por outro usuario

### Separacao por Tarefa

Fluxo atual:

1. coletor assume tarefa
2. consulta linha atual
3. confirma ou faz pass-by de linha
4. atualiza status conforme progresso

### Importacao e Impressao de Transportadora

Fluxo atual:

1. Web extrai NFs e volumes do PDF da transportadora.
2. NFs ja existentes em `DocExpedicao` sao descartadas antes da geracao das etiquetas.
3. Linhas repetidas sao deduplicadas por NF, contato e volume.
4. O ultimo lote da filial substitui o lote anterior em `NotaFiscalTransportadora`.
5. Com `ImprimirDireto = True`, o Web envia ao servidor nome/IP/porta da impressora e ZPL.
6. Com `ImprimirDireto = False`, o navegador imprime somente o texto ZPL em uma area isolada e mantem a tela de upload.

Configuracoes obrigatorias:

- `AppConfig.ImprimirDireto`
- `AppConfig.ImpressoraPadrao`
- `AppConfig.PrinterServerIP`
- `AppConfig.PrinterServerPort`
- `Impressora.Nome`, `Impressora.IP` e `Impressora.Porta`

Nao ha fallback fixo de infraestrutura no codigo.

## Menus e Navegacao

### Web

Menus controlados por:

- sessao
- perfil
- cache de menu por perfil/filial
- registros em `AppMenu`

### Coletor

Menus controlados por rotas fixas e autenticacao via session storage.

## Dependencias de Ambiente

### Web

- Windows
- Visual Studio
- MSBuild
- .NET Framework 4.8
- SQL Server

### API

- .NET SDK 9
- acesso ao SQL Server

### Coletor

- Node.js
- npm

## Riscos Tecnicos Conhecidos

- duplicidade de regras entre MVC e API exige sincronia documental e funcional
- EDMX exige cuidado extra quando o banco mudar
- mudancas de status operacionais impactam web, API, coletor e processos
- parte dos fluxos misturam acesso ORM com SQL direto para atender regras de negocio ou limitacoes do modelo

## Recomendacoes

- documentar toda nova regra de processo no momento da entrega
- manter o manual de utilizacao alinhado com telas novas
- revisar EDMX sempre que coluna, tabela ou relacionamento mudar
- validar impactos em `AppMenu`, API e coletor quando houver novo fluxo operacional
- validar a impressao automatica com as configuracoes da filial antes do Publish em producao
- consultar [Atualizacoes Operacionais de 20/07/2026](ATUALIZACOES_20260720.md)
- consultar [Atualizações Operacionais de 25/08/2026](ATUALIZACOES_20260825.md)
- consultar [Atualizações Operacionais de 31/08/2026](ATUALIZACOES_20260831.md)

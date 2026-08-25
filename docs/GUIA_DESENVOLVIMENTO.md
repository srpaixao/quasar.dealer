# Guia de Desenvolvimento e Execucao

## Objetivo

Orientar setup, execucao local, validacao e manutencao da solucao Automec.

## Pre-requisitos

### Gerais

- Windows
- acesso ao SQL Server
- permissoes de leitura e escrita no workspace

### Web MVC

- Visual Studio com suporte a ASP.NET classico
- .NET Framework 4.8 targeting pack
- MSBuild

### API

- .NET SDK 9

### Coletor

- Node.js 20 ou superior recomendado
- npm

## Estrategia de Execucao Local

### 1. API

```powershell
cd api
dotnet restore
dotnet run
```

Validacao:

- acessar `http://localhost:5049/` ou porta configurada
- conferir retorno com versao, servidor e base

### 2. Coletor

```powershell
cd coletor
npm install
npm run dev
```

Validacao:

- confirmar a `VITE_API_BASE_URL`
- abrir a URL exibida pelo Vite

### 3. Web MVC

Abrir:

- `web/Simplify.Quasar.sln`

Executar via Visual Studio.

Compilacao por terminal, quando o ambiente estiver completo:

```powershell
cd web
msbuild .\Simplify.Quasar.sln /t:Build /p:Configuration=Debug
```

## Validacoes Recomendadas por Tipo de Alteracao

### Alteracao no Coletor

- `npm run build`
- teste manual da tela alterada
- validacao de integracao com API
- validação do teclado e do terminador do leitor no modelo físico do coletor

### Alteracao na API

- `dotnet build`
- se o binario estiver em uso:

```powershell
dotnet build -o .\artifacts\validation-build
```

- teste manual do endpoint alterado

### Alteracao no Web MVC

- build no Visual Studio ou `msbuild`
- teste funcional na area alterada
- revisao de impacto em menu, sessao e perfil

## Cuidados com Banco e Modelos

### Web MVC

- usa EDMX e classes geradas
- mudanca de banco pode exigir ajuste em:
  - `Model_Quasar.edmx`
  - classes em `Models/`
  - consultas SQL manuais

### API

- usa modelos e contexto proprios em `Database/`

## Scripts SQL Disponiveis

Em `web/docs/sql/`:

- `20260607_DevolucaoComplemento.sql`
- `20260702_AlocacaoPedidosZona.sql`
- `20260718_Recebimento_NotaFiscal_Unicidade.sql`
- `20260718_Conferencia_Volume_Quantidades.sql`

Recomendacoes:

- versionar scripts de evolucao com data
- documentar impacto funcional no manual e nos procedimentos
- nao criar script para parametros que ja existam em `AppConfig`; validar valores por filial

## Convenções Relevantes

- usar a data/hora do servidor para eventos operacionais
- usar o usuario logado para ownership de processo
- preservar controle de concorrencia em fluxos com bloqueio
- nao sobrescrever dados finalizados sem regra explicita

## Fluxos que Merecem Regressao Manual

- conferencia de volumes
- conferencia de romaneios de expedicao
- separacao por tarefa
- alocacao por zona
- cadastro e navegacao de menus operacionais

## Checklist Antes de Entregar Alteracoes

1. o comportamento funcional foi validado?
2. a API ou o coletor ainda compilam?
3. o web foi validado no Visual Studio ou MSBuild quando houver alteracao nele?
4. o manual de utilizacao precisa ser atualizado?
5. o documento de processos precisa registrar a nova regra?
6. houve mudanca de menu, status ou concorrencia?
7. IPs, portas e nomes de impressora permanecem exclusivamente em `AppConfig`/`Impressora`?
8. o ultimo lote de `NotaFiscalTransportadora` substitui o anterior sem acumular registros?
9. NFs existentes em `DocExpedicao` deixam de gerar documentos e etiquetas?
10. o `inputmode` foi validado no WebView/Android do coletor?
11. o leitor envia `Enter` ou `Tab` ao final do barcode?
12. a versão do `package.json` está visível no login e na barra superior?
13. foi criado backup do site antes da sincronização de produção?

Para a entrega de 20/07/2026, consultar [Atualizacoes Operacionais](ATUALIZACOES_20260720.md).

Para a entrega de 25/08/2026, consultar [Atualizações Operacionais](ATUALIZACOES_20260825.md).

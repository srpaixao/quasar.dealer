# Simplify.Quasar Web

## Objetivo

Este diretorio contem a aplicacao principal do WMS Quasar em ASP.NET MVC 5.

Ela concentra:

- operacao administrativa
- telas operacionais de backoffice
- menus e cadastros
- relatorios
- regras legadas e regras ainda nao migradas para API/coletor

## Projeto Principal

- solucao: `Simplify.Quasar.sln`
- projeto web: `Simplify.Quasar/Simplify.Quasar.csproj`
- framework alvo: `.NET Framework 4.8`
- ORM principal: `Entity Framework 6`
- banco: SQL Server

## Modulos MVC

Os modulos atuais mapeados em `Simplify.Quasar/Areas` sao:

- `AdminApp`
- `AnomaliaApp`
- `ComprasApp`
- `ConfiguracaoApp`
- `ControleAcessoApp`
- `DevolucaoApp`
- `EstoqueApp`
- `ExpedicaoApp`
- `GarantiaApp`
- `RecebimentoApp`
- `SeparacaoApp`

## Destaques Funcionais

Versão Web MVC desta atualização: `1.0.10.0`.

### Padrão visual 1.0.10

O padrão visual baseado no Quasar.Delivery foi adotado como interface oficial do Quasar.Dealer:

- login orbital com card transparente e chamada operacional
- identidade visual azul do Quasar no botão principal
- cabeçalho, menu lateral, rodapé e dashboard responsivos
- formulários, tabelas, alertas, paginação e cards padronizados
- popups Bootstrap, Bootbox e SweetAlert com o mesmo padrão visual

A mudança é restrita à camada de apresentação e não altera controllers, serviços ou regras de negócio.

### Anomalias GM

O módulo `AnomaliaApp` permite:

- localizar todas as NFs e volumes de um item na filial ativa;
- cadastrar tipos A, B, C e G por item;
- controlar prazo e saldo reclamável de forma transacional;
- acompanhar aceite e rejeição dos itens;
- exportar os formulários oficiais de Anomalias e Danificados;
- visualizar no dashboard a quantidade de itens pendentes.

Os modelos `.xls` oficiais ficam em `Simplify.Quasar/App_Data/Templates`. A implantação do módulo exige os scripts `20260831_AnomaliasGM_*` antes da publicação da aplicação.

### Expedicao

O modulo de expedicao concentra, entre outros, os fluxos de:

- lancamentos e importacao de documentos
- etiquetas
- conferencias operacionais
- conferencia de romaneios via web

Fluxo novo ou revisado:

- `Expedicao > Conferir Romaneios`

Esse fluxo foi criado como opcao propria no web e replica a logica operacional do coletor para conferencia de romaneios de expedicao.

O fluxo `Expedicao > Importar arquivo de Transportadora` tambem contempla:

- rejeicao de NFs ja existentes em `DocExpedicao`
- deduplicacao de volumes extraidos do PDF
- manutencao somente do ultimo lote por filial
- impressao automatica parametrizada por `AppConfig` e cadastro `Impressora`
- impressao manual na propria tela, sem view adicional

## Menus com Atualizacao Automatizada

No startup da aplicacao, ajustes de menu podem ser aplicados pelo codigo:

- `EnsureRecebimentoConferenciaVolumeMenuTarget()`
- `EnsureExpedicaoConferenciaRomaneioMenu()`

Esses ajustes sao disparados em `Global.asax.cs` e ajudam a manter o `AppMenu` coerente com funcionalidades novas ou redirecionadas.

## Documentacao Relacionada

- [Indice de Documentacao](docs/README.md)
- [Manual de Utilizacao](docs/MANUAL_UTILIZACAO.md)
- [Procedimentos de Trabalho e Processos](docs/PROCEDIMENTOS_TRABALHO_E_PROCESSOS.md)
- [Arquitetura e Integracoes](docs/ARQUITETURA_E_INTEGRACOES.md)
- [Guia de Desenvolvimento e Execucao](docs/GUIA_DESENVOLVIMENTO.md)
- [Atualizacoes Operacionais de 20/07/2026](docs/ATUALIZACOES_20260720.md)
- [Atualizações Operacionais de 25/08/2026](docs/ATUALIZACOES_20260825.md)
- [Manual por Tela - Anomalias](docs/MANUAL_TELAS_ANOMALIAS.md)
- [Manual por Tela - Devolução](docs/MANUAL_TELAS_DEVOLUCAO.md)
- [Manual por Tela - Cadastros e Administração](docs/MANUAL_TELAS_CADASTROS.md)
- [Atualizações Operacionais de 31/08/2026](docs/ATUALIZACOES_20260831.md)
- [Padrão visual baseado no Quasar.Delivery](docs/visual-delivery-preview.md)

## Integração Operacional Atual

- o coletor de recebimento permite consultar os números dos volumes pendentes por área
- o teclado virtual da descarga fica desativado até o acionamento manual pelo operador
- a conferência quantitativa por volume registra quantidade, divergência, operador e horário
- coleta e transferência de estoque usam a Locação de Espera como contexto operacional
- a API e o coletor devem ser versionados em conjunto quando houver mudança de contrato

## Compilacao

### Via Visual Studio

Abrir `Simplify.Quasar.sln` e executar o projeto web.

### Via terminal

Requer:

- `MSBuild`
- workloads do ASP.NET classico
- .NET Framework 4.8 targeting pack

Exemplo:

```powershell
cd web
msbuild .\Simplify.Quasar.sln /t:Build /p:Configuration=Debug
```

## Estrutura Relevante

```text
web/
|-- Simplify.Quasar/
|   |-- Areas/
|   |-- Controllers/
|   |-- Custom/
|   |-- Models/
|   |-- Views/
|   `-- Global.asax.cs
|-- docs/
|   |-- MANUAL_UTILIZACAO.md
|   |-- MANUAL_TELAS_ANOMALIAS.md
|   |-- PROCEDIMENTOS_TRABALHO_E_PROCESSOS.md
|   |-- ARQUITETURA_E_INTEGRACOES.md
|   |-- GUIA_DESENVOLVIMENTO.md
|   `-- sql/
`-- README.md
```

## Observacoes

- o modelo EDMX ainda existe e precisa ser mantido consistente com o banco
- parte das regras operacionais hoje esta no MVC, parte na API
- quando houver mudanca funcional, o ideal e atualizar manual, processos e documentacao tecnica no mesmo ciclo
- para a parametrizacao de impressao, nao usar fallbacks fixos de IP, porta ou nome de impressora
- quando os parametros de impressao ja existirem no banco, a implantacao dessas regras exige somente Publish do Web

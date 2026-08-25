# Quasar API

Documentação revisada em `25/08/2026`.

## Objetivo

Esta API atende principalmente o coletor e concentracoes de regras operacionais mais novas do WMS Quasar.

## Stack

- ASP.NET Core `net9.0`
- Entity Framework Core
- SQL Server
- autenticacao JWT
- Swagger/Swashbuckle

## Arquivos Principais

- `Program.cs`: bootstrap da aplicacao
- `Extensions/`: configuracao de servicos, arquitetura e middleware
- `Routes/`: endpoints por dominio
- `Database/`: contexto e modelos do banco
- `DTO/`: contratos de entrada e saida

## Rotas Principais

### Autenticacao

- `POST /auth/login`

### Recebimento

- `GET /recebimento/volumeresumo/{statusId}/{areaId}`
- `POST /recebimento/volumeupdate`
- `GET /recebimento/conferencia-volume/{volume}`
- `POST /recebimento/conferencia-volume/{volume}/itens/{itemId}/confirmar`

O resumo de volumes aceita `statusId = 0` para todos ou o identificador do status desejado. No descarregamento, o coletor usa `statusId = 1` para listar somente `VolumeNr` pendentes da área e filial autenticadas.

Na conferencia por volume, a API retorna e atualiza `QtdConferida`, `Conferido`, usuario/data da conferencia e dados de armazenagem. Divergencias sao calculadas pela diferenca entre quantidade conferida e quantidade faturada.

### Armazenagem

- `GET /armazenagem/validarmaterial/{codigo}`
- `GET /armazenagem/validarquantidade/{codigo}`
- `POST /armazenagem/atualizarItemNotaFiscal`
- `POST /armazenagem/gravarHistorico`

### Estoque

- `GET /estoque/consultaritem/{itemnr}`
- `GET /estoque/consultarlocacao/{codigo}`
- `GET /estoque/consultarmovimentacao/{itemnr}`
- `POST /estoque/movimentacao`
- `PUT /estoque/movimentacao/{id}`

Os fluxos atuais de coleta e transferência validam a Locação de Espera, consultam suas movimentações pendentes e preservam a rastreabilidade entre origem, espera e destino final.

### Separacao

- `GET /separacao/zonas`
- `POST /separacao/assumir-tarefa`
- `GET /separacao/tarefas/{tarefaNr}/linha-atual`
- `POST /separacao/tarefas/{tarefaNr}/abandonar`
- `POST /separacao/tarefas/{tarefaNr}/passby-linha`
- `POST /separacao/tarefas/{tarefaNr}/confirmar-linha`
- `GET /separacao/tarefas/{tarefaNr}/status`

### Expedicao

- `GET /expedicao/conferencia-separacao/romaneios`
- `POST /expedicao/conferencia-separacao/iniciar`
- `GET /expedicao/conferencia-separacao/romaneios/{romaneioId}/itens`
- `POST /expedicao/conferencia-separacao/romaneios/{romaneioId}/confirmar`
- `POST /expedicao/conferencia-separacao/romaneios/{romaneioId}/abandonar`
- `GET /expedicao/transportadoras`
- `GET /expedicao/volumes/resumo/{transportadoraId}`
- `GET /expedicao/volumes/pendentes/{transportadoraId}`
- `GET /expedicao/volumes/lidos/{transportadoraId}`
- `GET /expedicao/doc`
- `GET /expedicao/historico/volumes`
- `POST /expedicao/historico`

## Fluxo de Conferencia de Romaneios de Expedicao

Regras principais implementadas:

- o romaneio disponivel nao pode estar assumido por outro usuario
- ao iniciar a conferencia:
  - `Romaneio.StatusId = 9`
  - `Romaneio.ConferenteId = usuario logado`
  - `Romaneio.DataConferente = data/hora atual`
  - `RomaneioItem.StatusId = 9`
  - `RomaneioItem.ConferenteId = usuario logado`
  - `RomaneioItem.DataConferente = data/hora atual`
- quantidade maior que a pendente e rejeitada
- quantidade zero envia o item para `StatusId = 6` (`Em Busca`)
- itens concluidos ficam com `StatusId = 4`
- ao abandonar:
  - `Romaneio.StatusId = 8`
  - apenas itens nao confirmados retornam para `StatusId = 8`
  - `ConferenteId` e `DataConferente` dos nao confirmados sao limpos

## Configuracao

### Connection String

Configurada em `appsettings.json` e sobreposicoes por ambiente:

- `appsettings.Development.json`
- `appsettings.Test.json`
- `appsettings.Production.json`

### CORS

`AllowedOrigins` deve contemplar a URL do coletor em uso.

### JWT

As configuracoes de chave, emissores e audiencias estao em `Jwt`.

## Execucao

```powershell
cd api
dotnet restore
dotnet run
```

A raiz `/` responde com:

- versao
- servidor de banco
- base ativa
- ambiente

## Build

```powershell
dotnet build
```

Se houver DLL em uso por processo ja executando, use uma saida separada:

```powershell
dotnet build -o .\artifacts\validation-build
```

## Observacoes

- o pacote `System.Security.Cryptography.Xml` possui aviso de vulnerabilidade conhecido e deve ser acompanhado
- a API e a principal base de integracao do coletor
- alteracoes funcionais de rotas devem refletir em `coletor/README.md` e no manual operacional
- a conferencia de recebimento depende das colunas documentadas em `web/docs/sql/20260718_Conferencia_Volume_Quantidades.sql`
- regras consolidadas em `web/docs/ATUALIZACOES_20260720.md`
- atualização funcional mais recente: `web/docs/ATUALIZACOES_20260825.md`
